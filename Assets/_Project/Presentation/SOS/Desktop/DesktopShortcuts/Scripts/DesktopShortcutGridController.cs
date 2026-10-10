using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopShortcutGridController : IDisposable
    {
        private readonly DesktopShortcutGridView view;

        private readonly Dictionary<string, GridItem> items =
            new Dictionary<string, GridItem>();

        private readonly List<GridItem> orderedItems =
            new List<GridItem>();

        private readonly Dictionary<Vector2Int, string> occupiedCells =
            new Dictionary<Vector2Int, string>();

        private sealed class GridItem
        {
            public string ID;
            public VisualElement Root;
            public Vector2Int? Cell;
        }

        public int PendingCount { get; private set; }

        public event Action<string, Vector2Int> ItemMoved;

        public DesktopShortcutGridController(
            DesktopShortcutGridView view)
        {
            this.view = view
                ?? throw new ArgumentNullException(nameof(view));

            view.DimensionsChanged += ReconcileLayout;
        }

        public void Add(
            string itemID,
            VisualElement instanceRoot,
            Vector2Int? preferredCell = null)
        {
            if (string.IsNullOrWhiteSpace(itemID))
                throw new ArgumentException(
                    "A grid item needs a stable ID.", nameof(itemID));

            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            if (items.ContainsKey(itemID))
                throw new InvalidOperationException(
                    $"Grid already contains '{itemID}'.");

            var item = new GridItem
            {
                ID = itemID,
                Root = instanceRoot,
                Cell = preferredCell
            };

            view.Attach(instanceRoot);
            items.Add(itemID, item);
            orderedItems.Add(item);

            ReconcileLayout();
        }

        public bool TryGetCell(
            string itemID,
            out Vector2Int cell)
        {
            cell = default;

            if (!items.TryGetValue(itemID, out GridItem item) ||
                !item.Cell.HasValue)
            {
                return false;
            }

            cell = item.Cell.Value;
            return true;
        }

        public bool CanMove(
            string itemID,
            Vector2Int destination)
        {
            if (!items.ContainsKey(itemID) ||
                !view.ContainsCell(destination))
            {
                return false;
            }

            return !occupiedCells.TryGetValue(
                       destination, out string occupant) ||
                   occupant == itemID;
        }

        public bool TryMove(
            string itemID,
            Vector2Int destination)
        {
            if (!CanMove(itemID, destination))
                return false;

            GridItem item = items[itemID];

            if (item.Cell.HasValue &&
                item.Cell.Value == destination)
            {
                return true;
            }

            if (item.Cell.HasValue)
            {
                occupiedCells.Remove(item.Cell.Value);
            }
            else
            {
                PendingCount--;
            }

            item.Cell = destination;
            occupiedCells[destination] = itemID;

            view.Place(item.Root, destination);
            ItemMoved?.Invoke(itemID, destination);

            return true;
        }

        public bool TryMoveToPointer(
            string itemID,
            Vector2 panelPosition)
        {
            return view.TryGetCell(panelPosition, out Vector2Int cell) &&
                   TryMove(itemID, cell);
        }

        public bool Remove(string itemID)
        {
            if (!items.TryGetValue(itemID, out GridItem item))
                return false;

            items.Remove(itemID);
            orderedItems.Remove(item);
            item.Root.RemoveFromHierarchy();

            ReconcileLayout();
            return true;
        }

        private void ReconcileLayout()
        {
            occupiedCells.Clear();

            // Reserve existing valid positions first.
            foreach (GridItem item in orderedItems)
            {
                if (item.Cell.HasValue &&
                    view.ContainsCell(item.Cell.Value) &&
                    !occupiedCells.ContainsKey(item.Cell.Value))
                {
                    occupiedCells.Add(item.Cell.Value, item.ID);
                }
                else
                {
                    item.Cell = null;
                }
            }

            PendingCount = 0;

            foreach (GridItem item in orderedItems)
            {
                if (!item.Cell.HasValue &&
                    TryFindEmptyCell(out Vector2Int emptyCell))
                {
                    item.Cell = emptyCell;
                    occupiedCells.Add(emptyCell, item.ID);
                }

                if (item.Cell.HasValue)
                {
                    view.Place(item.Root, item.Cell.Value);
                }
                else
                {
                    view.Hide(item.Root);
                    PendingCount++;
                }
            }
        }

        private bool TryFindEmptyCell(out Vector2Int cell)
        {
            for (int column = 0; column < view.Columns; column++)
            {
                for (int row = 0; row < view.Rows; row++)
                {
                    var candidate = new Vector2Int(column, row);

                    if (!occupiedCells.ContainsKey(candidate))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            cell = default;
            return false;
        }

        public void Clear()
        {
            foreach (GridItem item in orderedItems)
                item.Root.RemoveFromHierarchy();

            items.Clear();
            orderedItems.Clear();
            occupiedCells.Clear();
            PendingCount = 0;
        }

        public void Dispose()
        {
            view.DimensionsChanged -= ReconcileLayout;
            Clear();
        }
    }
}