using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Malchin.Building
{
    /// <summary>
    /// A single placed ger in the world. Occupies a footprint of grid cells,
    /// snaps to the grid while dragging, refuses to overlap other gers, opens its
    /// upgrade panel when tapped, and shows a placeholder colored block + label.
    /// Requires a 2D collider (for pointer hits) and a Physics2DRaycaster on the camera.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class GerView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GerDefinition definition;
        [Tooltip("Unique id for this placed ger (distinguishes two of the same type).")]
        public string instanceId;

        [Tooltip("Bottom-left grid cell this ger occupies.")]
        public Vector2Int originCell;

        public int Level { get; private set; }

        public int FootW => definition != null ? Mathf.Max(1, definition.footprintWidth) : 1;
        public int FootH => definition != null ? Mathf.Max(1, definition.footprintHeight) : 1;

        private SpriteRenderer _sr;
        private BoxCollider2D _col;
        private TextMeshPro _label;
        private Camera _cam;
        private Vector2Int _candidateCell;
        private bool _dragging;

        private static readonly Color InvalidTint = new Color(0.9f, 0.25f, 0.25f, 0.9f);

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<BoxCollider2D>();
            _cam = Camera.main;
            EnsurePlaceholderSprite();
            EnsureLabel();
            ConfigureForGrid();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 0, definition != null ? definition.MaxLevelIndex : 0);
            RefreshLabel();
        }

        public void SetCell(Vector2Int cell)
        {
            originCell = cell;
            if (GridManager.Instance != null) SnapToCurrentCell();
        }

        /// <summary>Sizes the block + collider to the footprint and snaps to its cell.</summary>
        public void ConfigureForGrid()
        {
            float cs = GridManager.Instance != null ? GridManager.Instance.cellSize : 0.5f;
            float w = FootW * cs, h = FootH * cs;

            EnsurePlaceholderSprite();
            _sr.drawMode = SpriteDrawMode.Sliced;
            _sr.size = new Vector2(w, h);
            _sr.color = definition != null ? definition.placeholderColor : Color.gray;
            _col.size = new Vector2(w, h);

            if (_label != null) _label.transform.localPosition = new Vector3(0f, -h / 2f - 0.3f, 0f);
            SnapToCurrentCell();
            RefreshLabel();
        }

        public void SnapToCurrentCell()
        {
            if (GridManager.Instance == null) return;
            transform.position = GridManager.Instance.CellBlockCenterWorld(originCell, FootW, FootH);
        }

        // ── Pointer handling ──────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragging) return;
            BuildingManager.Instance?.OnGerTapped(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _candidateCell = originCell;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var grid = GridManager.Instance;
            Vector3 world = ScreenToWorld(eventData.position);
            if (grid == null) { transform.position = world; return; }

            _candidateCell = grid.WorldToOriginCellClamped(world, FootW, FootH);
            transform.position = grid.CellBlockCenterWorld(_candidateCell, FootW, FootH);

            bool valid = grid.AreCellsFree(_candidateCell, FootW, FootH, this);
            _sr.color = valid ? definition.placeholderColor : InvalidTint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            var grid = GridManager.Instance;
            if (grid != null)
            {
                if (grid.AreCellsFree(_candidateCell, FootW, FootH, this))
                {
                    grid.Move(this, _candidateCell, FootW, FootH);
                    originCell = _candidateCell;
                }
                SnapToCurrentCell();             // commit or revert to last good cell
            }
            if (definition != null) _sr.color = definition.placeholderColor;
            BuildingManager.Instance?.OnGerMoved(this);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        Vector3 ScreenToWorld(Vector2 screenPos)
        {
            if (_cam == null) _cam = Camera.main;
            float depth = _cam != null ? -_cam.transform.position.z : 10f;
            Vector3 w = _cam != null
                ? _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth))
                : Vector3.zero;
            w.z = 0f;
            return w;
        }

        void EnsurePlaceholderSprite()
        {
            if (_sr.sprite != null) return;
            // Full-rect sprite with a 1px border so the Sliced draw mode resizes cleanly.
            var tex = Texture2D.whiteTexture;
            _sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f), tex.width, 0u,
                                       SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
        }

        void EnsureLabel()
        {
            if (_label != null) return;
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -1.1f, 0f);
            _label = go.AddComponent<TextMeshPro>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 6f;
            _label.color = Color.white;
            _label.rectTransform.sizeDelta = new Vector2(6f, 2f);
        }

        void RefreshLabel()
        {
            if (_label == null || definition == null) return;
            int max = definition.MaxLevelIndex;
            _label.text = max > 0
                ? $"{definition.displayName}\nLv {Level}"
                : definition.displayName;
        }
    }
}
