using UnityEngine;
using CatGame.Core.Interfaces;
using CatGame.Core;
using CatGame.Core.Data;
using System;

namespace CatGame.Capabilities.Health
{
    public class HealthController : MonoBehaviour, IDamageable, IHealable
    {
        public float Health => system.Health;
        public float MaxHealth => system.MaxHealth;

        // Events.
        public static event Action<HealthController> OnAnyDead;
        public event Action OnDead;
        public event Action<float, float> OnHealthChanged;
        public event Action OnDamaged;
        public event Action OnHealed;

        [Header("Settings")]
        [SerializeField] private float maxHealth;

        [Header("Sounds")]
        [SerializeField] private AudioData damageSound;
        [SerializeField] private AudioData deathSound;

        private HealthSystem system;

        private IAudioService audioService;

        private void Awake()
        {
            system = new HealthSystem(maxHealth);
        }

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        /// <summary>
        /// Causa dano.
        /// </summary>
        /// <param name="amount"></param>
        public void TakeDamage(float amount)
        {
            float previousHealth = system.Health;
            if (!system.TakeDamage(amount))
                return;

            OnHealthChanged?.Invoke(previousHealth, system.Health);
            OnDamaged?.Invoke();

            if (system.IsDead)
            {
                audioService.PlaySFXClip(deathSound, transform.position);
                OnDead?.Invoke();
                OnAnyDead?.Invoke(this);
            }
            else
            {
                audioService.PlaySFXClip(damageSound, transform.position);         
            }
        }

        /// <summary>
        /// Cura até o limite da vida máxima.
        /// </summary>
        public void Heal(float amount)
        {
            float previousHealth = system.Health;
            if (!system.Heal(amount))
                return;

            OnHealthChanged?.Invoke(previousHealth, system.Health);
            OnHealed?.Invoke();
        }

        /// <summary>
        /// Define a vida máxima e reinicia a vida atual no máximo.
        /// </summary>
        public void SetMaxHealth(float newMaxHealth)
        {
            if (newMaxHealth <= 0f)
                return;

            float previousHealth = system != null ? system.Health : newMaxHealth;

            maxHealth = newMaxHealth;
            system = new HealthSystem(maxHealth);

            OnHealthChanged?.Invoke(previousHealth, system.Health);
        }      
    }
}