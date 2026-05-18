/*
 * HealthSystem.cs
 * Role: Generic HP component. Attach to any Pokemon or Boss model GameObject.
 * HealthSystem auto-grabs nothing — just tracks HP and fires events.
 */
using UnityEngine;
using UnityEngine.Events;

namespace PokemonAR.Battle
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public UnityEvent<int, int> OnHealthChanged; // (current, max)
        public UnityEvent OnDeath;

        private int  _currentHealth;
        private bool _isDead;

        public int  CurrentHealth => _currentHealth;
        public int  MaxHealth     => maxHealth;
        public bool IsDead()      => _isDead;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (_isDead || amount <= 0) return;
            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            if (_currentHealth <= 0 && !_isDead)
            {
                _isDead = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (_isDead || amount <= 0) return;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
    }
}
