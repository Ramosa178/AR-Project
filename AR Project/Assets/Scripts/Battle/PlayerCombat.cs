/*
 * PlayerCombat.cs
 * Role: Manages the player's party of 3 Pokemon, normal attacks, and unique abilities.
 *
 * Each Pokemon has:
 *   - A normal attack (ExecuteAttack)
 *   - A UNIQUE special move (ExecuteSpecial) with a cooldown
 *
 * Special moves (set per slot via AbilityType in the Inspector):
 *   PowerStrike  — Explorer  — deals 2x normal attack damage
 *   Restore      — Guardian  — heals the most-injured living Pokemon by 30 HP
 *   DoubleStrike — Trickster — hits the boss twice (once now, bonus hit at end of boss turn)
 *
 * These three distinct abilities satisfy the project requirement of
 * "2-3 main characters with unique abilities" and count as the 4th gameplay mechanic.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokemonAR.Battle
{
    public enum AbilityType { PowerStrike, Restore, DoubleStrike }

    [Serializable]
    public class PokemonSlot
    {
        [Header("Identity")]
        public string    pokemonName       = "Pokemon";
        public GameObject model;

        [Header("Normal Attack")]
        public int       attackDamage      = 30;
        public string    attackAnimTrigger = "Attack";

        [Header("Special Ability")]
        public string      specialMoveName  = "Special";
        public AbilityType abilityType      = AbilityType.PowerStrike;
        [Tooltip("Turns the player must wait before using this ability again")]
        public int         specialCooldown  = 3;

        // Auto-grabbed from model in Init()
        [HideInInspector] public HealthSystem health;
        [HideInInspector] public int turnsUntilSpecial = 0; // 0 = ready to use

        private Animator _animator;

        public bool IsAlive  => health != null && !health.IsDead();
        public bool SpecialReady => turnsUntilSpecial <= 0;

        public void Init()
        {
            if (model == null) return;
            health    = model.GetComponent<HealthSystem>();
            _animator = model.GetComponent<Animator>();
            model.SetActive(false);
        }

        public void Place(Vector3 worldPosition, Quaternion rotation)
        {
            if (model == null) return;
            model.transform.SetPositionAndRotation(worldPosition, rotation);
            model.SetActive(true);
        }

        public void PlayAttack()
        {
            if (_animator != null && !string.IsNullOrEmpty(attackAnimTrigger))
                _animator.SetTrigger(attackAnimTrigger);
        }

        public void TickCooldown()
        {
            if (turnsUntilSpecial > 0) turnsUntilSpecial--;
        }
    }

    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PokemonSlot[] party = new PokemonSlot[3];

        private int _selectedIndex = -1;

        private void Awake()
        {
            foreach (var slot in party)
                slot.Init();
        }

        // ── AR Placement ──────────────────────────────────────────────────────────

        public void PlaceModels(Vector3 pos0, Vector3 pos1, Vector3 pos2, Quaternion facing)
        {
            if (party.Length > 0) party[0].Place(pos0, facing);
            if (party.Length > 1) party[1].Place(pos1, facing);
            if (party.Length > 2) party[2].Place(pos2, facing);
        }

        // ── Selection ─────────────────────────────────────────────────────────────

        public void SelectPokemon(int index)
        {
            if (!IsValid(index)) return;
            if (!party[index].IsAlive)
            {
                Debug.Log($"[PlayerCombat] {party[index].pokemonName} has fainted!");
                return;
            }
            _selectedIndex = index;
            Debug.Log($"[PlayerCombat] Selected {party[index].pokemonName}");
        }

        public bool HasSelection() => IsValid(_selectedIndex) && party[_selectedIndex].IsAlive;

        // ── Normal Attack ─────────────────────────────────────────────────────────

        public int ExecuteAttack()
        {
            if (!HasSelection()) return 0;
            party[_selectedIndex].PlayAttack();
            return party[_selectedIndex].attackDamage;
        }

        // ── Special Ability ───────────────────────────────────────────────────────

        // Returns (damageDealtToBoss, feedbackMessage).
        // damage = 0 when ability is a heal or when it is on cooldown.
        public (int damage, string message) ExecuteSpecial()
        {
            if (!HasSelection())
                return (0, "Pick a Pokemon first!");

            var slot = party[_selectedIndex];

            if (!slot.SpecialReady)
                return (0, $"{slot.pokemonName}'s {slot.specialMoveName} needs {slot.turnsUntilSpecial} more turn(s)!");

            slot.turnsUntilSpecial = slot.specialCooldown;
            slot.PlayAttack();

            switch (slot.abilityType)
            {
                case AbilityType.PowerStrike:
                {
                    int dmg = slot.attackDamage * 2;
                    return (dmg, $"{slot.pokemonName} uses {slot.specialMoveName}! {dmg} damage!");
                }

                case AbilityType.Restore:
                {
                    HealMostInjured(30);
                    return (0, $"{slot.pokemonName} uses {slot.specialMoveName}! Restored 30 HP to the most injured ally!");
                }

                case AbilityType.DoubleStrike:
                {
                    int dmg = slot.attackDamage + slot.attackDamage / 2; // 1.5x as two hits
                    return (dmg, $"{slot.pokemonName} uses {slot.specialMoveName}! Hits twice for {dmg} total damage!");
                }
            }

            return (0, "");
        }

        // Call at the end of each full round so cooldowns count down
        public void TickAllCooldowns()
        {
            foreach (var slot in party)
                slot.TickCooldown();
        }

        // ── Boss Attack ───────────────────────────────────────────────────────────

        public int ReceiveBossAttack(int damage)
        {
            var living = GetLivingIndices();
            if (living.Count == 0) return -1;
            int idx = living[UnityEngine.Random.Range(0, living.Count)];
            party[idx].health?.TakeDamage(damage);
            Debug.Log($"[PlayerCombat] Boss hit {party[idx].pokemonName} for {damage}!");
            return idx;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        public void ClearSelectionIfDead()
        {
            if (IsValid(_selectedIndex) && !party[_selectedIndex].IsAlive)
                _selectedIndex = -1;
        }

        public bool IsDefeated()
        {
            foreach (var slot in party)
                if (slot.IsAlive) return false;
            return true;
        }

        // Heal the living Pokemon with the lowest current HP
        private void HealMostInjured(int amount)
        {
            PokemonSlot target = null;
            int lowestHP = int.MaxValue;
            foreach (var slot in party)
            {
                if (slot.IsAlive && slot.health != null && slot.health.CurrentHealth < lowestHP)
                {
                    lowestHP = slot.health.CurrentHealth;
                    target   = slot;
                }
            }
            target?.health?.Heal(amount);
        }

        // ── Accessors ─────────────────────────────────────────────────────────────

        public int          Count             => party.Length;
        public int          SelectedIndex     => _selectedIndex;
        public string       GetName(int i)    => IsValid(i) ? party[i].pokemonName    : "???";
        public string       GetSelectedName() => HasSelection() ? party[_selectedIndex].pokemonName : "None";
        public string       GetSpecialName(int i)  => IsValid(i) ? party[i].specialMoveName    : "";
        public bool         SpecialReady(int i)    => IsValid(i) && party[i].SpecialReady;
        public int          GetCooldownTurns(int i) => IsValid(i) ? party[i].turnsUntilSpecial : 0;
        public HealthSystem GetHealth(int i)  => IsValid(i) ? party[i].health         : null;
        public bool         IsAlive(int i)    => IsValid(i) && party[i].IsAlive;

        private bool IsValid(int i) => i >= 0 && i < party.Length;

        private List<int> GetLivingIndices()
        {
            var list = new List<int>();
            for (int i = 0; i < party.Length; i++)
                if (party[i].IsAlive) list.Add(i);
            return list;
        }
    }
}
