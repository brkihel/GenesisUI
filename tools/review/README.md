# Read-only review evidence generator

Development tool for the [2026-10-02 review](../../docs/review/2026-10-02/README.md).
It is not referenced by the plugin, shipped in packages, or part of the runtime dependency set.
It uses the existing MetadataLoadContext package and Core/IL scanner to inspect files without
executing game or foreign plugin code. It writes evidence only to the supplied output directory.

## Run

Build the reviewed plugin in Preview first. Keep heavy tasks serial and heap-capped.

```powershell
$env:DOTNET_GCHeapHardLimit = '0x40000000'
dotnet test GenesisUI.sln -c Preview --no-restore --nologo -v q -m:1 -nr:false
dotnet run --project tools/review/Review.csproj -c Release -- C:/dev/genesisUI C:/Users/diego/AppData/Roaming/com.kesomannen.gale/valheim/profiles/FullPlaythrough C:/dev/genesisUI/dist/review-20261002
```

Arguments: repository root, Gale profile, output directory, optional Valheim `Managed` directory.
The default Managed path is the usual Steam installation on this review machine. Supply the
fourth argument on other machines. Inputs require `ref/`, a built Preview DLL and profile
`BepInEx/plugins` and `BepInEx/core` directories. Use ignored `dist/` for generated evidence.

For the channel gates, run Debug, Preview and Release separately; save stdout/TRX results to
scratch. A successful CLI evidence run is not a successful game test or adapter certification.

## Outputs and interpretation

| File | Meaning |
|---|---|
| `contracts.json` | Declared contract attributes in the built plugin |
| `game-references-without-global-declaration.json` | IL reference candidates absent from a global type/member-name set |
| `installed-client-contracts.json` | Separate existence/declared-overload check using installed Managed DLLs first, then reference dependencies |
| `modpack-metadata-surfaces.json` | Foreign plugin identities and name-based candidate surfaces, with per-file errors |
| `selected-public-surfaces.json` | Public metadata signatures for selected equipment/container API types; scratch only |
| `pure-reproductions.json` | Modeled subscriber order using real Core fault dispatch; duplicate layout IDs; invalid JSON numbers |

The IL comparison is deliberately a screening aid. It does not normalize property accessor
aliases or inherited declaring types, does not prove caller-scoped coverage, and cannot find
members resolved only by strings/reflection. It excludes compiler PrivateImplementationDetails
name collisions. Candidate counts must never be reported as confirmed contract violations.

Metadata resolution takes the first assembly for each simple name from production refs, then
profile DLLs, installed Managed files and other dependencies. Duplicate-name versions can make
dependency resolution ambiguous; review actual file hashes/owning plugin identity before adapter
binding. Public API presence does not prove read-only semantics or safe action behavior. Some
foreign DLLs embed another mod's API type; never bind by type name alone.

The installed-client check uses that directory first and reference fallbacks for dependencies
not present there. It uses the same existence/optional-parameter semantics as current L2 tests;
it does not check all return/field types or behavioral compatibility. It leaves `ref/` untouched.

The recovery reproduction does not instantiate Unity veils or leases: it reproduces Core event
ordering with modeled cleanup/rebuild effects matching the inspected source. The duplicate-ID
case is a hardening gap; the current inventory caller uses unique indices. Invalid-number cases
demonstrate the current reader's accepted grammar. These are evidence scenarios, not new failing
tests added to the solution, and the CLI does not exit nonzero because a reviewed weakness exists.

## Handling evidence

Keep raw foreign metadata signature dumps, decompiled game types, DLLs and player logs outside
tracked documentation. Commit only our analysis, reproduction tool/output and version/hash facts
needed to identify the review snapshot. Do not copy foreign implementation or assets.

The package/source CSV inventories and safe Loading-entry extraction were generated separately
from manifest files, assembly metadata, file hashes and tracked Git paths. They are dated static
evidence, not maintained release manifests. Recreate them after changing the target snapshot.
