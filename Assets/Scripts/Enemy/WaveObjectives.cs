using UnityEngine;

public class WaveTarget : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    private void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
            gameObject.SetActive(false);
    }
}

public class Hostage : MonoBehaviour, IDamageable
{
    public float maxHealth = 50f;
    public float CurrentHealth { get; private set; }

    private void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
            gameObject.SetActive(false);
    }
}