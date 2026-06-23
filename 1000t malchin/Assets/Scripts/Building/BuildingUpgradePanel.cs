using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Malchin.Economy;

namespace Malchin.Building
{
    /// <summary>
    /// Screen-space panel shown when a ger is tapped. Displays name, level, and
    /// the next upgrade cost, and performs the upgrade via BuildingManager.
    /// </summary>
    public class BuildingUpgradePanel : MonoBehaviour
    {
        public GameObject root;            // the panel container to show/hide
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text costText;
        public Button upgradeButton;
        public TMP_Text upgradeButtonLabel;
        public Button closeButton;

        private GerView _current;

        void Start()
        {
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (HerdManager.Instance != null) HerdManager.Instance.OnHerdChanged += RefreshIfOpen;
            Hide();
        }

        void OnDestroy()
        {
            if (HerdManager.Instance != null) HerdManager.Instance.OnHerdChanged -= RefreshIfOpen;
        }

        public void Show(GerView ger)
        {
            _current = ger;
            if (root != null) root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            _current = null;
            if (root != null) root.SetActive(false);
        }

        void RefreshIfOpen()
        {
            if (_current != null) Refresh();
        }

        void Refresh()
        {
            if (_current == null) return;
            var def = _current.definition;

            if (nameText != null) nameText.text = def.displayName;
            if (levelText != null) levelText.text = $"Level {_current.Level} / {def.MaxLevelIndex}";

            bool canUpgrade = def.CanUpgradeFrom(_current.Level);
            var costs = def.CostToUpgradeFrom(_current.Level);

            if (costText != null)
            {
                if (!canUpgrade) costText.text = "Max level";
                else costText.text = "Cost: " + FormatCosts(costs);
            }

            bool affordable = canUpgrade && BuildingManager.Instance != null
                                          && BuildingManager.Instance.CanAfford(_current);

            if (upgradeButton != null) upgradeButton.interactable = affordable;
            if (upgradeButtonLabel != null)
                upgradeButtonLabel.text = !canUpgrade ? "Maxed" : (affordable ? "Upgrade" : "Need more");
        }

        void OnUpgradeClicked()
        {
            if (_current == null || BuildingManager.Instance == null) return;
            if (BuildingManager.Instance.TryUpgrade(_current)) Refresh();
        }

        static string FormatCosts(System.Collections.Generic.List<LivestockCost> costs)
        {
            if (costs == null || costs.Count == 0) return "free";
            var sb = new StringBuilder();
            for (int i = 0; i < costs.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"{costs[i].amount} {costs[i].livestockId}");
            }
            return sb.ToString();
        }
    }
}
