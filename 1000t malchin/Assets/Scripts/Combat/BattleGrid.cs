using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>
    /// The battlefield grid. Row 0 (bottom) is the player's base edge; enemies enter above
    /// the top row and march down. Provides cell↔world conversion, a deploy collider over
    /// the whole grid, and the visuals: a whole-map background, a per-cell terrain layer
    /// (tile textures or placeholder colors), and grid lines. Configured from a
    /// LevelDefinition at battle start. Also answers terrain queries for later phases.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BattleGrid : MonoBehaviour
    {
        public int cols = 6;
        public int rows = 8;
        public float cellSize = 1f;

        private BoxCollider2D _col;
        private SpriteRenderer _lines;
        private SpriteRenderer _bg;
        private Transform _tileRoot;
        private TerrainDefinition[] _tiles;   // row-major, length cols*rows, or null = all default ground

        void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            _col.isTrigger = true;
        }

        // Grid is centered horizontally on this transform; row 0 sits at transform.y.
        public Vector3 Origin => new Vector3(transform.position.x - cols * cellSize / 2f, transform.position.y, 0f);
        public float BaseY => Origin.y;
        public float TopY => Origin.y + rows * cellSize;
        public float CenterY => Origin.y + rows * cellSize / 2f;

        public float ColumnX(int c) => Origin.x + (c + 0.5f) * cellSize;
        public float RowY(int r) => Origin.y + (r + 0.5f) * cellSize;
        public Vector3 CellCenter(int c, int r) => new Vector3(ColumnX(c), RowY(r), 0f);

        public int WorldToColumn(Vector3 w) => Mathf.Clamp(Mathf.FloorToInt((w.x - Origin.x) / cellSize), 0, cols - 1);
        public int WorldToRow(Vector3 w) => Mathf.Clamp(Mathf.FloorToInt((w.y - Origin.y) / cellSize), 0, rows - 1);

        // ── Configure ─────────────────────────────────────────────────────────

        /// <summary>Configure from a full level (terrain tiles + background).</summary>
        public void Configure(LevelDefinition level)
        {
            cols = Mathf.Max(1, level.gridWidth);
            rows = Mathf.Max(1, level.gridHeight);
            cellSize = Mathf.Max(0.25f, level.cellSize);
            _tiles = (level.tiles != null && level.tiles.Length == cols * rows) ? level.tiles : null;
            ApplyCollider();
            BuildBackground(level.background);
            BuildTiles();
            BuildGridLines();
        }

        /// <summary>Plain configure with no terrain (back-compat).</summary>
        public void Configure(int c, int r, float cs)
        {
            cols = Mathf.Max(1, c);
            rows = Mathf.Max(1, r);
            cellSize = Mathf.Max(0.25f, cs);
            _tiles = null;
            ApplyCollider();
            BuildBackground(null);
            BuildTiles();
            BuildGridLines();
        }

        // ── Terrain queries (used by deploy/pathing in later phases) ────────────

        public TerrainDefinition TerrainAt(int col, int row)
        {
            if (_tiles == null) return null;
            if (col < 0 || col >= cols || row < 0 || row >= rows) return null;
            return _tiles[row * cols + col];
        }

        /// <summary>Default ground (null tile) is walkable.</summary>
        public bool CanWalk(int col, int row)
        {
            var t = TerrainAt(col, row);
            return t == null || t.enemiesCanWalk;
        }

        /// <summary>Default ground (null tile) accepts melee only.</summary>
        public bool CanDeploy(int col, int row, bool ranged)
        {
            var t = TerrainAt(col, row);
            if (t == null) return !ranged;
            switch (t.deploy)
            {
                case TerrainDeploy.MeleeOnly:  return !ranged;
                case TerrainDeploy.RangedOnly: return ranged;
                case TerrainDeploy.Both:       return true;
                default:                       return false;
            }
        }

        // ── Visuals ─────────────────────────────────────────────────────────────

        void BuildBackground(Sprite sprite)
        {
            if (_bg == null)
            {
                var go = new GameObject("Background");
                go.transform.SetParent(transform, false);
                _bg = go.AddComponent<SpriteRenderer>();
                _bg.sortingOrder = -70;
            }
            _bg.gameObject.SetActive(sprite != null);
            if (sprite == null) return;

            _bg.sprite = sprite;
            _bg.transform.localPosition = new Vector3(0f, rows * cellSize / 2f, 0f);
            Vector2 s = sprite.bounds.size;
            float sx = s.x > 0f ? (cols * cellSize) / s.x : 1f;
            float sy = s.y > 0f ? (rows * cellSize) / s.y : 1f;
            _bg.transform.localScale = new Vector3(sx, sy, 1f);
        }

        void BuildTiles()
        {
            if (_tileRoot == null)
            {
                _tileRoot = new GameObject("Tiles").transform;
                _tileRoot.SetParent(transform, false);
            }
            for (int i = _tileRoot.childCount - 1; i >= 0; i--)
                DestroyImmediateSafe(_tileRoot.GetChild(i).gameObject);

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var terrain = TerrainAt(c, r);
                    var go = new GameObject($"Tile_{c}_{r}");
                    go.transform.SetParent(_tileRoot, false);
                    go.transform.localPosition = new Vector3(ColumnX(c) - transform.position.x,
                                                             RowY(r) - transform.position.y, 0f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -60;

                    if (terrain != null && terrain.tileSprite != null)
                    {
                        sr.sprite = terrain.tileSprite;
                        Vector2 bs = terrain.tileSprite.bounds.size;
                        float sx = bs.x > 0f ? cellSize / bs.x : cellSize;
                        float sy = bs.y > 0f ? cellSize / bs.y : cellSize;
                        go.transform.localScale = new Vector3(sx, sy, 1f);
                    }
                    else
                    {
                        sr.sprite = WhiteSquare();
                        sr.color = terrain != null ? terrain.color : new Color(0.20f, 0.26f, 0.16f, 1f);
                        go.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                    }
                }
        }

        void BuildGridLines()
        {
            if (_lines == null)
            {
                var go = new GameObject("GridLines");
                go.transform.SetParent(transform, false);
                _lines = go.AddComponent<SpriteRenderer>();
                _lines.sortingOrder = -50;
            }
            _lines.transform.localPosition = new Vector3(0f, rows * cellSize / 2f, 0f);

            const int px = 8;
            int tw = cols * px, th = rows * px;
            var tex = new Texture2D(tw, th) { filterMode = FilterMode.Point };
            var clear = new Color(0f, 0f, 0f, 0f);
            var line = new Color(1f, 1f, 1f, 0.22f);
            var pixels = new Color[tw * th];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            for (int x = 0; x < tw; x++)
                for (int y = 0; y < th; y++)
                    if (x % px == 0 || y % px == 0 || x == tw - 1 || y == th - 1)
                        pixels[y * tw + x] = line;
            tex.SetPixels(pixels);
            tex.Apply();

            float ppu = px / cellSize;
            _lines.sprite = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), ppu);
        }

        void ApplyCollider()
        {
            if (_col == null) _col = GetComponent<BoxCollider2D>();
            _col.isTrigger = true;
            _col.size = new Vector2(cols * cellSize, rows * cellSize);
            _col.offset = new Vector2(0f, rows * cellSize / 2f); // cover from base upward
        }

        static void DestroyImmediateSafe(GameObject go)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        private static Sprite _white;
        static Sprite WhiteSquare()
        {
            if (_white != null) return _white;
            var tex = Texture2D.whiteTexture;
            _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), tex.width);
            return _white;
        }
    }
}
