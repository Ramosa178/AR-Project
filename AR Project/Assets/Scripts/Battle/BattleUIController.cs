/*
 * BattleUIController.cs
 * Role: Updates all battle UI — HP bars, selection buttons, turn text, end panels.
 *
 * Attach to the Canvas GameObject in Battle.unity.
 * Drag each UI element from the Hierarchy into the matching slot in the Inspector.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PokemonAR.Battle
{
    public class BattleUIController : MonoBehaviour
    {
        // ── Player Pokemon HP bars (one per Pokemon) ──────────────────────────────
        [Header("Player Pokemon HP Sliders — one per Pokemon (0,1,2)")]
        [SerializeField] private Slider[] pokemonSliders = new Slider[3];

        [Header("Player Pokemon Name + HP Labels — one per Pokemon (0,1,2)")]
        [SerializeField] private TextMeshProUGUI[] pokemonLabels = new TextMeshProUGUI[3];

        // ── Boss HP bar ───────────────────────────────────────────────────────────
        [Header("Boss HP")]
        [SerializeField] private Slider          bossSlider;
        [SerializeField] private TextMeshProUGUI bossLabel;

        // ── Pokemon select buttons ────────────────────────────────────────────────
        [Header("Pokemon Select Buttons — one per Pokemon (0,1,2)")]
        [SerializeField] private Button[] selectButtons = new Button[3];

        // ── Attack / Special buttons ──────────────────────────────────────────────
        [Header("Attack and Special Buttons")]
        [SerializeField] private Button          attackButton;
        [SerializeField] private Button          specialButton;
        [SerializeField] private TextMeshProUGUI specialButtonLabel; // the TMP text child of Btn_Special

        // ── Turn text ─────────────────────────────────────────────────────────────
        [Header("Turn / Feedback Text")]
        [SerializeField] private TextMeshProUGUI turnText;

        // ── End panels ────────────────────────────────────────────────────────────
        [Header("End of Battle Panels")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        // ── Internal ──────────────────────────────────────────────────────────────
        private Color _normalColor   = Color.white;
        private Color _selectedColor = new Color(1f, 0.84f, 0f); // gold

        private void Start()
        {
            if (selectButtons.Length > 0 && selectButtons[0] != null)
                _normalColor = selectButtons[0].image.color;

            SetActive(victoryPanel, false);
            SetActive(defeatPanel,  false);
        }

        // ── Full refresh ──────────────────────────────────────────────────────────

        public void RefreshAll(PlayerCombat party, EnemyController boss)
        {
            for (int i = 0; i < party.Count; i++)
                RefreshPokemonHP(i, party);
            RefreshBossHP(boss);
            ResetButtonHighlights();
        }

        // ── HP bar updates ────────────────────────────────────────────────────────

        public void RefreshPokemonHP(int index, PlayerCombat party)
        {
            var hp = party.GetHealth(index);
            if (hp == null) return;

            float ratio = hp.MaxHealth > 0 ? (float)hp.CurrentHealth / hp.MaxHealth : 0f;

            if (index < pokemonSliders.Length && pokemonSliders[index] != null)
                pokemonSliders[index].value = ratio;

            if (index < pokemonLabels.Length && pokemonLabels[index] != null)
                pokemonLabels[index].text = party.IsAlive(index)
                    ? $"{party.GetName(index)}\n{hp.CurrentHealth}/{hp.MaxHealth} HP"
                    : $"{party.GetName(index)}\nFainted";

            // Grey out the select button if the Pokemon fainted
            if (index < selectButtons.Length && selectButtons[index] != null)
                selectButtons[index].interactable = party.IsAlive(index);
        }

        public void RefreshBossHP(EnemyController boss)
        {
            float ratio = boss.MaxHealth > 0 ? (float)boss.CurrentHealth / boss.MaxHealth : 0f;
            if (bossSlider != null) bossSlider.value = ratio;
            if (bossLabel  != null) bossLabel.text   = $"Boss\n{boss.CurrentHealth}/{boss.MaxHealth} HP";
        }

        // ── Selection highlight ───────────────────────────────────────────────────

        public void HighlightSelected(int index)
        {
            ResetButtonHighlights();
            if (index >= 0 && index < selectButtons.Length && selectButtons[index] != null)
                selectButtons[index].image.color = _selectedColor;
        }

        // ── Special button cooldown indicator ────────────────────────────────────

        // Call this whenever a Pokemon is selected or cooldowns tick.
        // ready = true  → button is green and says "SPECIAL"
        // ready = false → button is grey and says "SPECIAL (2)" with turns remaining
        public void UpdateSpecialButton(bool ready, int turnsLeft = 0)
        {
            if (specialButton != null)
                specialButton.interactable = ready;

            if (specialButtonLabel != null)
                specialButtonLabel.text = ready ? "SPECIAL" : $"SPECIAL\n({turnsLeft} turns)";
        }

        // ── Text ──────────────────────────────────────────────────────────────────

        public void SetTurnText(string message)
        {
            if (turnText != null) turnText.text = message;
        }

        // ── End panels ────────────────────────────────────────────────────────────

        public void ShowVictory()
        {
            SetActive(defeatPanel,  false);
            SetActive(victoryPanel, true);
            if (attackButton != null) attackButton.interactable = false;
        }

        public void ShowDefeat()
        {
            SetActive(victoryPanel, false);
            SetActive(defeatPanel,  true);
            if (attackButton != null) attackButton.interactable = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ResetButtonHighlights()
        {
            foreach (var btn in selectButtons)
                if (btn != null) btn.image.color = _normalColor;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
