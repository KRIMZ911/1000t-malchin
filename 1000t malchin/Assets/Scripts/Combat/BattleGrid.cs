using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>
    /// The battlefield grid. Row 0 (bottom) is the player's base edge; enemies
    /// enter above the top row and march down. Provides cell↔world conversion,
    /// a deploy collider over the whole grid, and a placeholder grid visual.
    /// Configured from a LevelDefinition at battle start.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BattleGrid : MonoBehaviour
    {
        public int cols = 6;
        public int rows = 8;
        public float cellSize = 1f;

        private BoxCollider2D _col;
        private SpriteRenderer _visual;

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

        public void Configure(int c, int r, float cs)
        {
            cols = Mathf.Max(1, c);
            rows = Mathf.Max(1, r);
            cellSize = Mathf.Max(0.25f, cs);
            ApplyCollider();
            BuildVisual();
        }

        void ApplyCollider()
        {
            if (_col == null) _col = GetComponent<BoxCollider2D>();
            _col.isTrigger = true;
            _col.size = new Vector2(cols * cellSize, rows * cellSize);
            _col.offset = new Vector2(0f, rows * cellSize / 2f); // cover from base upward
        }

        void BuildVisual()
        {
            if (_visual == null)
            {
                var go = new GameObject("GridVisual");
                go.transform.SetParent(transform, false);
                _visual = go.AddComponent<SpriteRenderer>();
                _visual.sortingOrder = -60;
            }
            _visual.transform.localPosition = new Vector3(0f, rows * cellSize / 2f, 0f);

            const int px = 8;
            int tw = cols * px, th = rows * px;
            var tex = new Texture2D(tw, th) { filterMode = FilterMode.Point };
            var fill = new Color(0.16f, 0.20f, 0.12f, 1f);
            var line = new Color(1f, 1f, 1f, 0.22f);
            var pixels = new Color[tw * th];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            for (int x = 0; x < tw; x++)
                for (int y = 0; y < th; y++)
                    if (x % px == 0 || y % px == 0 || x == tw - 1 || y == th - 1)
                        pixels[y * tw + x] = line;
            tex.SetPixels(pixels);
            tex.Apply();

            float ppu = px / cellSize;
            _visual.sprite = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
