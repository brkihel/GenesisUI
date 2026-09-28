using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace GenesisUI.Contract.Tests
{
    /// <summary>One IL instruction that references a member or type.</summary>
    internal sealed class Reference
    {
        public string CallerType;   // "GenesisUI.Foundation.ReportWriter" (nested: "Outer/Inner")
        public string CallerMethod;
        public string Target;       // "System.IO.File::WriteAllText" or "type:Some.Type"

        public override string ToString() => CallerType + "::" + CallerMethod + " -> " + Target;
    }

    /// <summary>
    /// Minimal IL reader over System.Reflection.Metadata: lists every member and type
    /// token used by every method body, plus the types in signatures, base types and
    /// fields. Enough to enforce "never call X" and "Foundation never uses Y".
    /// </summary>
    internal static class IlScanner
    {
        private static readonly Dictionary<ushort, OpCode> OpCodesByValue =
            typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => (OpCode)f.GetValue(null))
                .ToDictionary(o => (ushort)o.Value);

        public static List<Reference> Scan(string dllPath)
        {
            var result = new List<Reference>();
            using var stream = File.OpenRead(dllPath);
            using var pe = new PEReader(stream);
            var md = pe.GetMetadataReader();
            var names = new NameProvider(md);

            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                string typeName = names.TypeDefName(th);

                void AddType(string target, string method) => result.Add(new Reference { CallerType = typeName, CallerMethod = method, Target = "type:" + target });

                if (!td.BaseType.IsNil) AddType(names.TypeName(td.BaseType), "<base>");
                foreach (var ih in td.GetInterfaceImplementations())
                    AddType(names.TypeName(md.GetInterfaceImplementation(ih).Interface), "<interface>");
                foreach (var fh in td.GetFields())
                {
                    var f = md.GetFieldDefinition(fh);
                    AddType(f.DecodeSignature(names, null), "<field " + md.GetString(f.Name) + ">");
                }

                foreach (var mh in td.GetMethods())
                {
                    var m = md.GetMethodDefinition(mh);
                    string methodName = md.GetString(m.Name);
                    var sig = m.DecodeSignature(names, null);
                    AddType(sig.ReturnType, methodName);
                    foreach (var p in sig.ParameterTypes) AddType(p, methodName);

                    if (m.RelativeVirtualAddress == 0) continue;
                    var body = pe.GetMethodBody(m.RelativeVirtualAddress);
                    if (!body.LocalSignature.IsNil)
                    {
                        var locals = md.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(names, null);
                        foreach (var l in locals) AddType(l, methodName);
                    }

                    var il = body.GetILReader();
                    while (il.RemainingBytes > 0)
                    {
                        ushort value = il.ReadByte();
                        if (value == 0xFE) value = (ushort)(0xFE00 | il.ReadByte());
                        var op = OpCodesByValue[value];
                        switch (op.OperandType)
                        {
                            case OperandType.InlineMethod:
                            case OperandType.InlineField:
                            case OperandType.InlineTok:
                            case OperandType.InlineType:
                                var handle = MetadataTokens.EntityHandle(il.ReadInt32());
                                string target = names.Describe(handle);
                                if (target != null) result.Add(new Reference { CallerType = typeName, CallerMethod = methodName, Target = target });
                                break;
                            case OperandType.InlineSwitch:
                                int n = il.ReadInt32();
                                il.Offset += 4 * n;
                                break;
                            case OperandType.InlineNone: break;
                            case OperandType.ShortInlineBrTarget:
                            case OperandType.ShortInlineI:
                            case OperandType.ShortInlineVar: il.Offset += 1; break;
                            case OperandType.InlineVar: il.Offset += 2; break;
                            case OperandType.InlineI8:
                            case OperandType.InlineR: il.Offset += 8; break;
                            default: il.Offset += 4; break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>Turns handles and signatures into readable full names.</summary>
        private sealed class NameProvider : ISignatureTypeProvider<string, object>
        {
            private readonly MetadataReader _md;

            public NameProvider(MetadataReader md) => _md = md;

            public string TypeDefName(TypeDefinitionHandle h)
            {
                var t = _md.GetTypeDefinition(h);
                var declaring = t.GetDeclaringType();
                string name = _md.GetString(t.Name);
                if (!declaring.IsNil) return TypeDefName(declaring) + "/" + name;
                string ns = _md.GetString(t.Namespace);
                return ns.Length == 0 ? name : ns + "." + name;
            }

            public string TypeRefName(TypeReferenceHandle h)
            {
                var t = _md.GetTypeReference(h);
                string name = _md.GetString(t.Name);
                if (t.ResolutionScope.Kind == HandleKind.TypeReference) return TypeRefName((TypeReferenceHandle)t.ResolutionScope) + "/" + name;
                string ns = _md.GetString(t.Namespace);
                return ns.Length == 0 ? name : ns + "." + name;
            }

            public string TypeName(EntityHandle h) => h.Kind switch
            {
                HandleKind.TypeDefinition => TypeDefName((TypeDefinitionHandle)h),
                HandleKind.TypeReference => TypeRefName((TypeReferenceHandle)h),
                HandleKind.TypeSpecification => _md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(this, null),
                _ => "?",
            };

            public string Describe(EntityHandle h)
            {
                switch (h.Kind)
                {
                    case HandleKind.MethodDefinition:
                        var md = _md.GetMethodDefinition((MethodDefinitionHandle)h);
                        return TypeDefName(md.GetDeclaringType()) + "::" + _md.GetString(md.Name);
                    case HandleKind.FieldDefinition:
                        var fd = _md.GetFieldDefinition((FieldDefinitionHandle)h);
                        return TypeDefName(fd.GetDeclaringType()) + "::" + _md.GetString(fd.Name);
                    case HandleKind.MemberReference:
                        var mr = _md.GetMemberReference((MemberReferenceHandle)h);
                        string parent = mr.Parent.Kind == HandleKind.MethodDefinition
                            ? TypeDefName(_md.GetMethodDefinition((MethodDefinitionHandle)mr.Parent).GetDeclaringType())
                            : TypeName(mr.Parent);
                        return parent + "::" + _md.GetString(mr.Name);
                    case HandleKind.MethodSpecification:
                        return Describe(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);
                    case HandleKind.TypeDefinition:
                    case HandleKind.TypeReference:
                    case HandleKind.TypeSpecification:
                        return "type:" + TypeName(h);
                    default:
                        return null;
                }
            }

            // Generic instantiations keep their arguments ("List`1<Foo>") so a type used only
            // as a generic argument is still visible to the isolation check.
            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
                genericType + "<" + string.Join(",", typeArguments) + ">";
            public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[]";
            public string GetByReferenceType(string elementType) => elementType;
            public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
            public string GetGenericMethodParameter(object genericContext, int index) => "!!" + index;
            public string GetGenericTypeParameter(object genericContext, int index) => "!" + index;
            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
            public string GetPinnedType(string elementType) => elementType;
            public string GetPointerType(string elementType) => elementType;
            public string GetPrimitiveType(PrimitiveTypeCode typeCode) => "System." + typeCode;
            public string GetSZArrayType(string elementType) => elementType + "[]";
            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => TypeDefName(handle);
            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => TypeRefName(handle);
            public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
                reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }
    }
}
