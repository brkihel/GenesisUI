using System;
using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.InventoryModel;
using GenesisUI.Theme;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GenesisUI.Widgets
{
    /// <summary>A compact, scrollable view of native cells. Item operations stay on native elements.</summary>
    [GameContract("assembly_valheim", "InventoryGrid", "m_onReleased", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Action\u00603[[InventoryGrid, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[ItemDrop\u002BItemData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[Vector2i, assembly_utils, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Inventory", "GetWidth")]
    [GameContract("assembly_valheim", "Inventory", "GetHeight")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetIcon")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetMaxDurability", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_durability", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_quality", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_stack", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxStackSize", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxQuality", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_useDurability", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_utils", "ZInput", "IsTouchActive")]
    [GameContract("assembly_utils", "ZInput", "IsGamepadActive", Parameters = new string[0])]
    [GameContract("assembly_valheim", "InventoryGrid", "m_elements", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[InventoryElement, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetGamepadSelectedElement")]
    [GameContract("assembly_valheim", "InventoryElement", "get_Position")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "x", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "y", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "op_Equality", Parameters = new string[] { "Vector2i", "Vector2i" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", ".ctor", Parameters = new string[] { "System.Int32", "System.Int32" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance)]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_equipped", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [ContractDependency(typeof(WindowParts), typeof(SlotView), typeof(ScrollArea), typeof(Guard), typeof(Ui))]
    internal sealed class NativeGridProjection
    {
        internal const string Owner = "module:win.crafting";
        private static AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> _elements;
        private static CellInput _hovered;
        private readonly ThemeRuntime _theme;
        private readonly ScrollArea _scroll;
        private readonly List<CellInput> _cells = new List<CellInput>();
        private InventoryGrid _grid;
        private int _width = -1, _height = -1;
        private readonly int _columns;
        private readonly float _size;
        private bool _shown;

        internal static void Bind() => _elements = AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");

        internal NativeGridProjection(RectTransform parent, ThemeRuntime theme, string name, float x, float y, float width, float height, int columns)
        {
            _theme = theme; _columns = columns;
            _size = (width - 10f - (columns - 1) * 6f) / columns;
            _scroll = new ScrollArea(parent, name, x, y, width, height, theme.Tokens, _size + 6f);
        }

        internal void Refresh(InventoryGrid grid, ItemDrop.ItemData dragged)
        {
            _grid = grid;
            var inventory = grid != null ? grid.GetInventory() : null;
            if (inventory == null) { Hide(); return; }
            int width = inventory.GetWidth(), height = inventory.GetHeight();
            var layout = new GridProjection(width, height, _columns);
            _shown = true;
            if (width != _width || height != _height)
            {
                ClearHover();
                foreach (var cell in _cells) UnityEngine.Object.Destroy(cell.gameObject);
                _cells.Clear(); _width = width; _height = height;
                int columns = layout.Columns;
                for (int i = 0; i < layout.Count; i++)
                {
                    var slot = new SlotView(_scroll.Content, "Cell " + i, _theme, new Vector2(0f, 1f),
                        new Vector2(i % columns * (_size + 6f), -i / columns * (_size + 6f)), new Vector2(_size, _size), null, "hotslot", "Windows");
                    slot.Root.pivot = new Vector2(0f, 1f);
                    Ui.Image(slot.Root, null, Color.clear, raycast: true);
                    var quality = Ui.Fit(Ui.Text(slot.Root, "Quality", _theme, FontRole.Display, _size * .2f,
                        ThemeRuntime.ToUnity(_theme.Tokens.AccentGoldBright), TextAlignmentOptions.TopRight), 9f);
                    Ui.Fill((RectTransform)quality.transform, _size * .5f, 4f, 4f, _size * .65f);
                    var input = slot.Root.gameObject.AddComponent<CellInput>();
                    var address = layout.Native(i);
                    input.Init(this, slot, quality, new Vector2i(address.X, address.Y));
                    _cells.Add(input);
                }
                _scroll.ContentHeight = layout.Rows * (_size + 6f);
                _scroll.ToTop();
            }
            var selected = ZInput.IsGamepadActive() || ZInput.IsTouchActive() ? grid.GetGamepadSelectedElement() : null;
            var focused = selected != null ? selected.GetComponent<InventoryElement>() : null;
            if (focused != null && ZInput.IsGamepadActive())
            {
                var position = focused.Position;
                int index = position.y * width + position.x;
                if (index >= 0 && index < layout.Count)
                {
                    float top = (index / layout.Columns) * (_size + 6f);
                    _scroll.ShowRange(top, top + _size);
                }
            }
            foreach (var cell in _cells)
            {
                var item = inventory.GetItemAt(cell.Position.x, cell.Position.y);
                var icon = item != null ? item.GetIcon() : null;
                float alpha = item != null && item == dragged ? .35f : 1f;
                if (icon != cell.ShownIcon || alpha != cell.ShownAlpha)
                {
                    cell.ShownIcon = icon; cell.ShownAlpha = alpha;
                    cell.Slot.SetIcon(icon, alpha);
                }
                cell.Slot.SetAmount(item != null && item.m_shared.m_maxStackSize > 1 ? item.m_stack : 0, compact: true);
                cell.Slot.SetBar(item != null && item.m_shared.m_useDurability ? Mathf.Clamp01(item.m_durability / Mathf.Max(1f, item.GetMaxDurability())) : -1f);
                cell.Slot.SetState(_hovered == cell || (focused != null && focused.Position == cell.Position), item != null && item.m_equipped);
                int quality = item != null && item.m_shared.m_maxQuality > 1 ? item.m_quality : 0;
                if (quality != cell.ShownQuality)
                {
                    cell.ShownQuality = quality;
                    if (quality > 0) cell.Quality.SetText("{0}", quality); else cell.Quality.text = "";
                }
            }
        }

        internal void Hide() { ClearHover(); _shown = false; }
        private void ClearHover()
        {
            if (_hovered == null || _hovered.Projection != this) return;
            var cell = _hovered;
            _hovered = null;
            cell.Forward(cell.HoverEvent, ExecuteEvents.pointerExitHandler);
            cell.HoverEvent = null;
        }

        private InventoryElement Element(Vector2i position)
        {
            if (!_shown || _grid == null || _elements == null) return null;
            var elements = _elements(_grid);
            if (elements == null) return null;
            foreach (var element in elements) if (element != null && element.Position == position) return element;
            return null;
        }

        internal static InventoryElement ProjectHover(InventoryGrid grid)
        {
            var cell = _hovered;
            return cell != null && !Guard.IsTripped(Owner) && cell.Projection._shown && cell.Projection._grid == grid
                ? cell.Projection.Element(cell.Position) : null;
        }

        internal sealed class CellInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
            IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler
        {
            internal NativeGridProjection Projection;
            internal SlotView Slot;
            internal TextMeshProUGUI Quality;
            internal Vector2i Position;
            internal PointerEventData HoverEvent;
            internal int ShownQuality = -1;
            internal Sprite ShownIcon;
            internal float ShownAlpha = -1f;
            private Action<PointerEventData> _down, _up, _click, _enter, _exit, _begin, _drag, _end, _drop, _wheel;
            internal void Init(NativeGridProjection projection, SlotView slot, TextMeshProUGUI quality, Vector2i position)
            {
                Projection = projection; Slot = slot; Quality = quality; Position = position;
                _down = e => Forward(e, ExecuteEvents.pointerDownHandler);
                _up = e => Forward(e, ExecuteEvents.pointerUpHandler);
                _click = e => Forward(e, ExecuteEvents.pointerClickHandler);
                _enter = e => { if (_hovered != null) _hovered.Projection.ClearHover(); _hovered = this; HoverEvent = e; Forward(e, ExecuteEvents.pointerEnterHandler); };
                _exit = e => { Forward(e, ExecuteEvents.pointerExitHandler); if (_hovered == this) _hovered = null; HoverEvent = null; };
                _begin = e => Forward(e, ExecuteEvents.beginDragHandler);
                _drag = e => Forward(e, ExecuteEvents.dragHandler);
                _end = e =>
                {
                    if (!ZInput.IsTouchActive()) return;
                    var original = e.pointerCurrentRaycast;
                    var projected = original;
                    var element = _hovered != null ? _hovered.Projection.Element(_hovered.Position) : null;
                    if (element != null) projected.gameObject = element.gameObject;
                    e.pointerCurrentRaycast = projected;
                    try { Forward(e, ExecuteEvents.endDragHandler); }
                    finally { e.pointerCurrentRaycast = original; }
                };
                _drop = e =>
                {
                    if (ZInput.IsTouchActive() || !Projection._shown || Projection._grid == null) return;
                    var inventory = Projection._grid.GetInventory();
                    if (inventory != null) Projection._grid.m_onReleased?.Invoke(Projection._grid, inventory.GetItemAt(Position.x, Position.y), Position);
                };
                _wheel = e => { Projection.ClearHover(); Projection._scroll.Scroll(e.scrollDelta.y); };
            }
            internal void Forward<T>(PointerEventData e, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
            {
                if (e == null || !Projection._shown) return;
                var element = Projection.Element(Position);
                if (element != null) ExecuteEvents.Execute(element.gameObject, e, handler);
            }
            public void OnPointerDown(PointerEventData e) => Guard.Run(Owner, _down, e);
            public void OnPointerUp(PointerEventData e) => Guard.Run(Owner, _up, e);
            public void OnPointerClick(PointerEventData e) => Guard.Run(Owner, _click, e);
            public void OnPointerEnter(PointerEventData e) => Guard.Run(Owner, _enter, e);
            public void OnPointerExit(PointerEventData e) => Guard.Run(Owner, _exit, e);
            public void OnBeginDrag(PointerEventData e) => Guard.Run(Owner, _begin, e);
            public void OnDrag(PointerEventData e) => Guard.Run(Owner, _drag, e);
            public void OnEndDrag(PointerEventData e) => Guard.Run(Owner, _end, e);
            public void OnDrop(PointerEventData e) => Guard.Run(Owner, _drop, e);
            public void OnScroll(PointerEventData e) => Guard.Run(Owner, _wheel, e);
        }
    }
}
