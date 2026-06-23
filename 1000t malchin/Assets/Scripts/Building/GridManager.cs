using System.Collections.Generic;
using UnityEngine;

namespace Malchin.Building
{
    /// <summary>
    /// A fixed build grid (default 20x20). Converts between world space and cell
    /// coordinates, tracks which cells each ger occupies, validates placement
    /// (in-bounds + no overlap), and draws a placeholder grid background.
    ///
    /// A ger's position is stored as its bottom-left "origin cell"; its footprint
    /// (in cells) comes from its GerDefinition.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid size")]
        [Min(1)] public int cols = 20;
        [Min(1)] public int rows = 20;
        [Min(0.05f)] public float cellSize = 0.5f;

        private readonly Dictionary<Vector2Int, GerView> _occupied = new Dictionary<Vector2Int, GerView>();

        /// <summary>World position of the grid's bottom-left corner (grid is centered on the origin).</summary>
        public Vector3 OriginWorld => new Vector3(-cols * cellSize / 2f, -rows * cellSize / 2f, 0f);

        public float GridWorldWidth => cols * cellSize;
        public float GridWorldHeight => rows * cellSize;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureGridVisual();
        }

        // ── Coordinate conversion ─────────────────────────────────────────────

        public Vector3 CellBlockCenterWorld(Vector2Int origin, int w, int h)
        {
            Vector3 o = OriginWorld;
            return new Vector3(o.x + (origin.x + w / 2f) * cellSize,
                               o.y + (origin.y + h / 2f) * cellSize, 0f);
        }

        /// <summary>Nearest valid origin cell so a w×h footprint centers under <paramref name="world"/>.</summary>
        public Vector2Int WorldToOriginCellClamped(Vector3 world, int w, int h)
        {
            Vector3 o = OriginWorld;
            int cx = Mathf.RoundToInt((world.x - o.x) / cellSize - w / 2f);
            int cy = Mathf.RoundToInt((world.y - o.y) / cellSize - h / 2f);
            cx = Mathf.Clamp(cx, 0, Mathf.Max(0, cols - w));
            cy = Mathf.Clamp(cy, 0, Mathf.Max(0, rows - h));
            return new Vector2Int(cx, cy);
        }

        // ── Occupancy ─────────────────────────────────────────────────────────

        public bool AreCellsFree(Vector2Int origin, int w, int h, GerView ignore)
        {
            for (int x = origin.x; x < origin.x + w; x++)
                for (int y = origin.y; y < origin.y + h; y++)
                {
                    if (x < 0 || y < 0 || x >= cols || y >= rows) return false;
                    if (_occupied.TryGetValue(new Vector2Int(x, y), out var g) && g != ignore) return false;
                }
            return true;
        }

        public void Occupy(GerView ger, Vector2Int origin, int w, int h)
        {
            for (int x = origin.x; x < origin.x + w; x++)
                for (int y = origin.y; y < origin.y + h; y++)
                    _occupied[new Vector2Int(x, y)] = ger;
        }

        public void Free(GerView ger)
        {
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _occupied)
                if (kv.Value == ger) toRemove.Add(kv.Key);
            foreach (var k in toRemove) _occupied.Remove(k);
        }

        public void Move(GerView ger, Vector2Int newOrigin, int w, int h)
        {
            Free(ger);
            Occupy(ger, newOrigin, w, h);
        }

        /// <summary>
        /// Clears and rebuilds occupancy from a set of gers, snapping each to its
        /// origin cell. If a ger's saved cell collides, it's nudged to the first
        /// free spot so nothing ends up stacked.
        /// </summary>
        public void Rebuild(IEnumerable<GerView> gers)
        {
            _occupied.Clear();
            foreach (var ger in gers)
            {
                int w = ger.FootW, h = ger.FootH;
                Vector2Int origin = ger.originCell;
                origin.x = Mathf.Clamp(origin.x, 0, Mathf.Max(0, cols - w));
                origin.y = Mathf.Clamp(origin.y, 0, Mathf.Max(0, rows - h));

                if (!AreCellsFree(origin, w, h, ger))
                    origin = FirstFreeCell(w, h, ger) ?? origin;

                ger.originCell = origin;
                Occupy(ger, origin, w, h);
                ger.ConfigureForGrid();
            }
        }

        private Vector2Int? FirstFreeCell(int w, int h, GerView ignore)
        {
            for (int y = 0; y <= rows - h; y++)
                for (int x = 0; x <= cols - w; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (AreCellsFree(c, w, h, ignore)) return c;
                }
            return null;
        }

        // ── Placeholder grid visual ───────────────────────────────────────────

        void EnsureGridVisual()
        {
            var go = new GameObject("GridVisual");
            go.transform.SetParent(transform, false);
            go.transform.position = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -100;

            const int px = 8; // texture pixels per cell
            int tw = cols * px, th = rows * px;
            var tex = new Texture2D(tw, th) { filterMode = FilterMode.Point };

            var fill = new Color(0.20f, 0.28f, 0.18f, 0.55f);
            var line = new Color(1f, 1f, 1f, 0.22f);
            var pixels = new Color[tw * th];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;

            for (int x = 0; x < tw; x++)
                for (int y = 0; y < th; y++)
                    if (x % px == 0 || y % px == 0 || x == tw - 1 || y == th - 1)
                        pixels[y * tw + x] = line;

            tex.SetPixels(pixels);
            tex.Apply();

            float ppu = px / cellSize; // maps texture exactly onto grid world size
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
