using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopShortcutGridView : IDisposable
    {
        public VisualElement Root { get; }

        public Vector2 ItemSize { get; }
        public Vector2 Gap { get; }



        public int Columns { get; private set; }
        public int Rows { get; private set; }

        public event Action DimensionsChanged;

        public DesktopShortcutGridView(
            VisualElement root,
            Vector2 itemSize,
            Vector2 gap)
        {
            Root = root
                ?? throw new ArgumentNullException(nameof(root));

            if (itemSize.x <= 0f || itemSize.y <= 0f ||
                gap.x < 0f || gap.y < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemSize),
                    "Item size must be positive and gaps nonnegative.");
            }

            ItemSize = itemSize;
            Gap = gap;

            Root.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            Measure();
        }

        public bool ContainsCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns &&
                   cell.y >= 0 && cell.y < Rows;
        }

        public Vector2 GetCellPosition(Vector2Int cell)
        {
            return new Vector2(
                cell.x * (ItemSize.x + Gap.x),
                cell.y * (ItemSize.y + Gap.y));
        }

        public bool TryGetCell(
            Vector2 panelPosition,
            out Vector2Int cell)
        {
            cell = default;

            if (Root.panel == null)
                return false;

            Vector2 localPosition =
                Root.WorldToLocal(panelPosition);

            if (!Root.contentRect.Contains(localPosition))
                return false;

            cell = new Vector2Int(
                Mathf.FloorToInt(
                    localPosition.x / (ItemSize.x + Gap.x)),
                Mathf.FloorToInt(
                    localPosition.y / (ItemSize.y + Gap.y)));

            return ContainsCell(cell);
        }

        public void Attach(VisualElement instanceRoot)
        {
            instanceRoot.style.position = Position.Absolute;
            instanceRoot.style.width = ItemSize.x;
            instanceRoot.style.height = ItemSize.y;
            instanceRoot.style.marginLeft = 0;
            instanceRoot.style.marginRight = 0;
            instanceRoot.style.marginTop = 0;
            instanceRoot.style.marginBottom = 0;

            // Remain hidden until a cell is assigned.
            instanceRoot.style.display = DisplayStyle.None;

            Root.Add(instanceRoot);
        }

        public void Place(
            VisualElement instanceRoot,
            Vector2Int cell)
        {
            Vector2 position = GetCellPosition(cell);

            instanceRoot.style.left = position.x;
            instanceRoot.style.top = position.y;
            instanceRoot.style.display = DisplayStyle.Flex;
        }

        public void Hide(VisualElement instanceRoot)
        {
            instanceRoot.style.display = DisplayStyle.None;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Measure();
        }

        private void Measure()
        {
            Rect bounds = Root.contentRect;

            int columns = CountCells(
                bounds.width, ItemSize.x, Gap.x);

            int rows = CountCells(
                bounds.height, ItemSize.y, Gap.y);

            if (columns == Columns && rows == Rows)
                return;

            Columns = columns;
            Rows = rows;

            DimensionsChanged?.Invoke();
        }

        private static int CountCells(
            float available,
            float itemSize,
            float gap)
        {
            if (float.IsNaN(available) ||
                float.IsInfinity(available) ||
                available < itemSize)
            {
                return 0;
            }

            // A final cell does not need a trailing gap.
            return Mathf.FloorToInt(
                (available + gap) / (itemSize + gap));
        }

        public void Dispose()
        {
            Root.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);
        }
    }
}