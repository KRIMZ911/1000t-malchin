using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Malchin.Combat
{
    /// <summary>
    /// Battle UI: a "Battle" entry button shown on the base, the in-battle deploy
    /// controls + status readout, and the end-of-battle result with a Return button.
    /// All references are wired by the CombatSceneSetup editor tool.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        public BattleController controller;

        [Header("Base view")]
        public Button battleButton;          // shown when not in battle

        [Header("In-battle")]
        public GameObject battlePanel;        // deploy controls + status
        public TMP_Text baseHpText;
        public TMP_Text enemiesText;
        public Button archerButton;
        public TMP_Text archerLabel;
        public Button horsemanButton;
        public TMP_Text horsemanLabel;

        [Header("Manual skill")]
        public Button skillButton;            // shown when a unit with a manual skill is tapped
        public TMP_Text skillLabel;

        [Header("Result")]
        public GameObject resultPanel;
        public TMP_Text resultText;
        public Button returnButton;

        void Start()
        {
            if (battleButton != null)   battleButton.onClick.AddListener(() => controller.StartBattle());
            if (archerButton != null)   archerButton.onClick.AddListener(() => controller.SelectArcher());
            if (horsemanButton != null) horsemanButton.onClick.AddListener(() => controller.SelectHorseman());
            if (returnButton != null)   returnButton.onClick.AddListener(() => controller.Return());
            if (skillButton != null)    skillButton.onClick.AddListener(() => controller.UseSelectedUnitSkill());

            ShowBaseView();
        }

        // ── Manual skill button ──────────────────────────────────────────────

        public void ShowSkillButton(string skillName, bool ready)
        {
            if (skillButton == null) return;
            skillButton.gameObject.SetActive(true);
            if (skillLabel != null) skillLabel.text = string.IsNullOrEmpty(skillName) ? "Use Skill" : $"Use: {skillName}";
            SetSkillButtonReady(ready);
        }

        public void SetSkillButtonReady(bool ready)
        {
            if (skillButton != null) skillButton.interactable = ready;
        }

        public void HideSkillButton()
        {
            if (skillButton != null) skillButton.gameObject.SetActive(false);
        }

        // ── Called by BattleController ────────────────────────────────────────

        public void OnBattleStarted()
        {
            if (battleButton != null) battleButton.gameObject.SetActive(false);
            if (battlePanel != null)  battlePanel.SetActive(true);
            if (resultPanel != null)  resultPanel.SetActive(false);
            HideSkillButton();
        }

        public void Refresh(float baseHp, float baseMax, int enemiesRemaining, int archersLeft, int horsemenLeft)
        {
            if (baseHpText != null)  baseHpText.text  = $"Base HP: {Mathf.Max(0, Mathf.CeilToInt(baseHp))}/{Mathf.CeilToInt(baseMax)}";
            if (enemiesText != null) enemiesText.text = $"Enemies left: {enemiesRemaining}";
            if (archerLabel != null)   archerLabel.text   = $"Archer ({archersLeft})";
            if (horsemanLabel != null) horsemanLabel.text = $"Horseman ({horsemenLeft})";
        }

        public void OnBattleEnded(bool won, int sheep, int cattle, int horse)
        {
            if (battlePanel != null) battlePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText != null)
            {
                resultText.text = won
                    ? $"Victory!\n\n+{sheep} sheep\n+{cattle} cattle\n+{horse} special horse"
                    : "Defeat...\n\nYour camp was overrun.\nNo reward.";
            }
        }

        public void OnReturned()
        {
            ShowBaseView();
        }

        void ShowBaseView()
        {
            if (battleButton != null) battleButton.gameObject.SetActive(true);
            if (battlePanel != null)  battlePanel.SetActive(false);
            if (resultPanel != null)  resultPanel.SetActive(false);
            HideSkillButton();
        }
    }
}
