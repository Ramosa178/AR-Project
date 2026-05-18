/*
 * EnemyController.cs
 * Role: Manages the boss enemy.
 *
 * Drag the boss model GameObject into "Boss Model" in the Inspector.
 * The script auto-grabs HealthSystem and Animator from that GameObject.
 * BattleManager calls PlaceModel() to position the boss when AR detects a plane,
 * then calls TakeDamage() and PlayAttackAnim() each battle turn.
 */
using UnityEngine;

namespace PokemonAR.Battle
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Boss Model — drag the boss GameObject from the Hierarchy here")]
        [SerializeField] private GameObject bossModel;

        [Header("Boss Stats")]
        [SerializeField] private int    attackDamage      = 20;
        [SerializeField] private string attackAnimTrigger = "Attack";

        private HealthSystem _health;
        private Animator     _animator;

        public int AttackDamage   => attackDamage;
        public int CurrentHealth  => _health != null ? _health.CurrentHealth : 0;
        public int MaxHealth      => _health != null ? _health.MaxHealth     : 1;

        private void Awake()
        {
            if (bossModel == null) return;
            _health   = bossModel.GetComponent<HealthSystem>();
            _animator = bossModel.GetComponent<Animator>();
            bossModel.SetActive(false); // hidden until BattleManager places it
        }

        // Called by BattleManager once AR detects a plane
        // Called by BattleManager with the pre-computed world position and rotation
        public void PlaceModel(Vector3 worldPosition, Quaternion rotation)
        {
            if (bossModel == null) return;
            bossModel.transform.SetPositionAndRotation(worldPosition, rotation);
            bossModel.SetActive(true);
        }

        public void TakeDamage(int amount) => _health?.TakeDamage(amount);

        public bool IsDead() => _health != null && _health.IsDead();

        public void PlayAttackAnim()
        {
            if (_animator != null && !string.IsNullOrEmpty(attackAnimTrigger))
                _animator.SetTrigger(attackAnimTrigger);
        }
    }
}
