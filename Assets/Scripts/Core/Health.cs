using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [System.Serializable]
    public class DamageEvent : UnityEvent<float> { }
    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Events")]
    public DamageEvent OnDamageTaken;
    public UnityEvent OnDeath;
    
    private float currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // Вызываем событие с переданным уроном
        OnDamageTaken?.Invoke(damage);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }
}