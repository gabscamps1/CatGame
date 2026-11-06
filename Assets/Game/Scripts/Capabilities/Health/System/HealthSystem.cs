using System;

namespace CatGame.Capabilities.Health
{
    public class HealthSystem
    {
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get; private set; }

        public HealthSystem(float maxHealth)
        {
            Health = maxHealth;
            MaxHealth = maxHealth;
        }

        public bool TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) 
                return false;

            Health -= amount;
            Health = Math.Max(Health, 0);

            if (Health <= 0)
            {
                IsDead = true;
            }

            return true;
        }

        public bool Heal(float amount)
        {
            if (IsDead || amount <= 0f) 
                return false;

            Health += amount;
            Health = Math.Clamp(Health, 0, MaxHealth);

            return true;
        }
    }
}
