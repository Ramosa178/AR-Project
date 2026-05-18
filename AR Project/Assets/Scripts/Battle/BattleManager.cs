/*
 * BattleManager.cs
 * Role: Owns the turn-based battle loop and AR plane detection.
 *
 * FLOW:
 *   Point camera at floor → AR detects plane → 4 models appear in formation
 *   → Player taps a Pokemon button → taps ATTACK → Pokemon damages boss
 *   → Boss attacks a random living Pokemon → repeat until someone is defeated
 *
 * ORIENTATION FIX:
 *   Formation is built relative to the camera direction at the moment the plane
 *   is detected. Pokemon spawn on the camera-facing side, boss on the far side.
 *   Both sides face each other using a flat (Y-only) LookRotation so models
 *   stay upright regardless of the imported mesh rotation.
 *
 * Place this script (and PlayerCombat, EnemyController, BattleController)
 * all on ONE GameObject called "BattleManager" in Battle.unity.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using PokemonAR.Core;

namespace PokemonAR.Battle
{
    public enum BattleState { WaitingForAR, PlayerTurn, Attacking, BossTurn, Won, Lost }

    public class BattleManager : MonoBehaviour
    {
        // ── AR ────────────────────────────────────────────────────────────────────
        [Header("AR — drag XR Origin here")]
        [SerializeField] private ARPlaneManager arPlaneManager;

        // ── References ────────────────────────────────────────────────────────────
        [Header("Battle Scripts (all on this same GameObject)")]
        [SerializeField] private PlayerCombat      playerParty;
        [SerializeField] private EnemyController   boss;
        [SerializeField] private BattleUIController ui;

        // ── Formation distances ───────────────────────────────────────────────────
        [Header("Formation Distances (metres from plane centre)")]
        [Tooltip("How far the 3 Pokemon appear toward the camera")]
        [SerializeField] private float pokemonDistance = 0.30f;
        [Tooltip("Horizontal gap between each Pokemon")]
        [SerializeField] private float pokemonSpread   = 0.20f;
        [Tooltip("How far the boss appears away from the camera")]
        [SerializeField] private float bossDistance    = 0.30f;

        // ── Timing ────────────────────────────────────────────────────────────────
        [Header("Seconds to wait after each attack animation")]
        [SerializeField] private float attackDuration = 1.5f;

        // ── Events ────────────────────────────────────────────────────────────────
        public UnityEvent OnBattleStarted;
        public UnityEvent OnBattleWon;
        public UnityEvent OnBattleLost;

        public BattleState CurrentState { get; private set; } = BattleState.WaitingForAR;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (arPlaneManager != null)
                arPlaneManager.planesChanged += OnPlanesChanged;
        }

        private void OnDisable()
        {
            if (arPlaneManager != null)
                arPlaneManager.planesChanged -= OnPlanesChanged;
        }

        // ── AR plane detection ────────────────────────────────────────────────────

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (CurrentState != BattleState.WaitingForAR) return;
            foreach (var plane in args.added)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    PlaceFormationAt(plane.center);
                    return;
                }
            }
        }

        // Computes camera-relative positions so the formation always faces the player,
        // then sets model positions and rotations before starting the battle.
        private void PlaceFormationAt(Vector3 origin)
        {
            arPlaneManager.enabled = false;  // stop detecting new planes

            // Direction from the plane centre toward the camera (horizontal only)
            Camera cam = Camera.main;
            Vector3 toCamera = cam != null
                ? new Vector3(cam.transform.position.x - origin.x, 0f,
                              cam.transform.position.z - origin.z).normalized
                : Vector3.forward;
            if (toCamera == Vector3.zero) toCamera = Vector3.forward;

            Vector3 right = Vector3.Cross(Vector3.up, toCamera).normalized;

            // Pokemon on the camera-facing side, spread left/right
            Vector3 p0 = origin + toCamera * pokemonDistance - right * pokemonSpread;
            Vector3 p1 = origin + toCamera * pokemonDistance;
            Vector3 p2 = origin + toCamera * pokemonDistance + right * pokemonSpread;

            // Boss on the far side
            Vector3 bossPos = origin - toCamera * bossDistance;

            // Each side faces the other (Y-axis rotation only — models stay upright)
            Quaternion pokemonFacing = FlatLook(p1,      bossPos);
            Quaternion bossFacing    = FlatLook(bossPos, p1);

            playerParty.PlaceModels(p0, p1, p2, pokemonFacing);
            boss.PlaceModel(bossPos, bossFacing);

            StartBattle();
        }

        // Horizontal-only LookRotation so models never tilt forward/back
        private static Quaternion FlatLook(Vector3 from, Vector3 to)
        {
            Vector3 dir = new Vector3(to.x - from.x, 0f, to.z - from.z);
            return dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;
        }

        // ── Battle start ──────────────────────────────────────────────────────────

        private void StartBattle()
        {
            SetState(BattleState.PlayerTurn);
            ui?.RefreshAll(playerParty, boss);
            ui?.SetTurnText("Your turn! Pick a Pokemon and tap ATTACK.");
            OnBattleStarted?.Invoke();
        }

        // ── UI BUTTON HOOKS — wire these in the Inspector ─────────────────────────

        /// Wire Pokemon button 0 → OnPokemonSelected(0), button 1 → (1), button 2 → (2)
        public void OnPokemonSelected(int index)
        {
            if (CurrentState != BattleState.PlayerTurn) return;
            playerParty.SelectPokemon(index);
            ui?.HighlightSelected(index);
            ui?.SetTurnText($"{playerParty.GetName(index)} is ready to attack!");
            RefreshSpecialButton(index);
        }

        /// Wire the SPECIAL button to this
        public void OnSpecialPressed()
        {
            if (CurrentState != BattleState.PlayerTurn) return;
            if (!playerParty.HasSelection())
            {
                ui?.SetTurnText("Pick a Pokemon first!");
                return;
            }
            SetState(BattleState.Attacking);
            StartCoroutine(SpecialMoveRoutine());
        }

        /// Wire the ATTACK button to this
        public void OnAttackPressed()
        {
            if (CurrentState != BattleState.PlayerTurn) return;
            if (!playerParty.HasSelection())
            {
                ui?.SetTurnText("Pick a Pokemon first!");
                return;
            }
            SetState(BattleState.Attacking);
            StartCoroutine(PlayerAttackRoutine());
        }

        // ── Attack coroutines ─────────────────────────────────────────────────────

        private IEnumerator SpecialMoveRoutine()
        {
            var (dmg, message) = playerParty.ExecuteSpecial();

            // Cooldown not ready — return to player turn without spending the turn
            if (message.Contains("needs") && dmg == 0)
            {
                ui?.SetTurnText(message);
                SetState(BattleState.PlayerTurn);
                yield break;
            }

            if (dmg > 0) boss.TakeDamage(dmg);
            ui?.RefreshAll(playerParty, boss);
            ui?.SetTurnText(message);

            yield return new WaitForSeconds(attackDuration);

            if (boss.IsDead()) { Win(); yield break; }

            SetState(BattleState.BossTurn);
            ui?.SetTurnText("Boss is attacking...");
            yield return new WaitForSeconds(0.6f);
            StartCoroutine(BossAttackRoutine());
        }

        private IEnumerator PlayerAttackRoutine()
        {
            int dmg = playerParty.ExecuteAttack();
            boss.TakeDamage(dmg);
            ui?.RefreshBossHP(boss);
            ui?.SetTurnText($"{playerParty.GetSelectedName()} attacks for {dmg} damage!");

            yield return new WaitForSeconds(attackDuration);

            if (boss.IsDead()) { Win(); yield break; }

            SetState(BattleState.BossTurn);
            ui?.SetTurnText("Boss is attacking...");
            yield return new WaitForSeconds(0.6f);
            StartCoroutine(BossAttackRoutine());
        }

        private IEnumerator BossAttackRoutine()
        {
            boss.PlayAttackAnim();
            int targetIdx = playerParty.ReceiveBossAttack(boss.AttackDamage);
            ui?.RefreshPokemonHP(targetIdx, playerParty);
            ui?.SetTurnText($"Boss attacks {playerParty.GetName(targetIdx)} for {boss.AttackDamage} damage!");

            yield return new WaitForSeconds(attackDuration);

            if (playerParty.IsDefeated()) { Lose(); yield break; }

            playerParty.TickAllCooldowns();
            playerParty.ClearSelectionIfDead();
            ui?.RefreshAll(playerParty, boss);
            RefreshSpecialButton(playerParty.SelectedIndex);
            SetState(BattleState.PlayerTurn);
            ui?.SetTurnText("Your turn! Pick a Pokemon and tap ATTACK.");
        }

        // ── Win / Lose ────────────────────────────────────────────────────────────

        private void Win()
        {
            SetState(BattleState.Won);
            ui?.ShowVictory();
            OnBattleWon?.Invoke();
            GameManager.Instance?.CompletePhase(GameState.Battle);
            BattleController.Instance?.CompletePhase();
        }

        private void Lose()
        {
            SetState(BattleState.Lost);
            ui?.ShowDefeat();
            OnBattleLost?.Invoke();
            GameManager.Instance?.TriggerGameOver();
            BattleController.Instance?.TriggerGameOver();
        }

        private void SetState(BattleState s)
        {
            CurrentState = s;
            Debug.Log($"[BattleManager] → {s}");
        }

        // Reads the selected Pokemon's cooldown and updates the SPECIAL button label.
        private void RefreshSpecialButton(int selectedIndex)
        {
            if (selectedIndex < 0)
            {
                ui?.UpdateSpecialButton(false, 0);
                return;
            }
            bool ready      = playerParty.SpecialReady(selectedIndex);
            // turnsUntilSpecial is internal — we derive it from the feedback message
            // Simple approach: just pass ready state; turns shown only when not ready
            // Use the accessor we added to PlayerCombat
            ui?.UpdateSpecialButton(ready, playerParty.GetCooldownTurns(selectedIndex));
        }

        // ── Editor debug helper ───────────────────────────────────────────────────
        // In Play mode: right-click the BattleManager component header → "DEBUG Spawn in Front of Camera"
        [ContextMenu("DEBUG Spawn in Front of Camera")]
        private void DebugSpawnAtOrigin()
        {
            if (!Application.isPlaying) return;
            Camera cam = Camera.main;
            if (cam == null) { PlaceFormationAt(Vector3.zero); return; }
            // Place the formation 1.5 m in front of the camera at floor level
            Vector3 origin = cam.transform.position + cam.transform.forward * 1.5f;
            origin.y = 0f;
            PlaceFormationAt(origin);
        }
    }
}
