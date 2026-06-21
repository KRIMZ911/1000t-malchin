using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Malchin.Economy
{
    /// <summary>
    /// Spawns one HerdRowUI per livestock type and refreshes them whenever the
    /// herd changes. Also wires Save / Load buttons if assigned.
    ///
    /// Manual setup:
    ///   1. Add this component to a Canvas GameObject.
    ///   2. Assign rowPrefab (see instructions below).
    ///   3. Assign rowContainer (a vertical layout group).
    ///   4. Optionally assign saveButton / loadButton.
    /// </summary>
    public class HerdUI : MonoBehaviour
    {
        [Tooltip("Prefab with HerdRowUI, an icon Image, a label TMP_Text, and a count TMP_Text.")]
        public GameObject rowPrefab;

        [Tooltip("Parent transform (use a Vertical Layout Group).")]
        public Transform rowContainer;

        [Tooltip("Optional — wires Save on click.")]
        public Button saveButton;

        [Tooltip("Optional — wires Load on click.")]
        public Button loadButton;

        private List<HerdRowUI> _rows = new List<HerdRowUI>();

        void Start()
        {
            if (HerdManager.Instance == null) { Debug.LogWarning("HerdUI: HerdManager not in scene."); return; }

            BuildRows();
            HerdManager.Instance.OnHerdChanged += RefreshRows;

            if (saveButton != null) saveButton.onClick.AddListener(SaveSystem.Save);
            if (loadButton != null) loadButton.onClick.AddListener(SaveSystem.Load);
        }

        void OnDestroy()
        {
            if (HerdManager.Instance != null)
                HerdManager.Instance.OnHerdChanged -= RefreshRows;
        }

        void BuildRows()
        {
            foreach (Transform child in rowContainer) Destroy(child.gameObject);
            _rows.Clear();

            foreach (var def in HerdManager.Instance.livestockTypes)
            {
                var go = Instantiate(rowPrefab, rowContainer);
                var row = go.GetComponent<HerdRowUI>();
                if (row == null) { Debug.LogError("rowPrefab is missing HerdRowUI component."); continue; }
                row.Initialize(def);
                _rows.Add(row);
            }
        }

        void RefreshRows()
        {
            foreach (var row in _rows) row.Refresh();
        }
    }
}
