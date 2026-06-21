using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Malchin.Economy
{
    /// <summary>
    /// One row in the herd display panel.
    /// Set up via HerdUI — you don't need to place this manually.
    /// </summary>
    public class HerdRowUI : MonoBehaviour
    {
        public Image icon;
        public TMP_Text label;
        public TMP_Text countText;

        private string _id;

        public void Initialize(LivestockDefinition def)
        {
            _id = def.id;
            if (icon != null) icon.color = def.placeholderColor;
            if (label != null) label.text = def.displayName;
            Refresh();
        }

        public void Refresh()
        {
            if (HerdManager.Instance == null) return;
            int count = HerdManager.Instance.GetCount(_id);
            int cap   = HerdManager.Instance.GetCap(_id);
            if (countText != null) countText.text = $"{count} / {cap}";
        }
    }
}
