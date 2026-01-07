using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private bool piercing = false; // Пробивает несколько целей
    [SerializeField] private int maxPierceCount = 1;
    
    private Vector2 direction;
    private float speed;
    private Rigidbody2D rb;
    private Weapon sourceWeapon;
    private int piercedCount = 0;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }
    
    public void Initialize(Vector2 dir, float spd, Weapon weapon = null)
    {
        direction = dir.normalized;
        speed = spd;
        sourceWeapon = weapon;
        
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }
        
        // Поворачиваем пулю в направлении полета
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Игнорируем триггеры, только твердые коллайдеры
        if (collision.isTrigger) return;
        
        // Проверяем слои
        if (((1 << collision.gameObject.layer) & targetLayers) == 0)
            return;
        
        // Наносим урон
        Health health = collision.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            
            // Можно передать информацию об оружии
            // if (sourceWeapon != null) health.LastDamageSource = sourceWeapon;
        }
        
        // Эффект попадания
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Проверяем, может ли пуля пробить дальше
        if (piercing && piercedCount < maxPierceCount)
        {
            piercedCount++;
            return; // Не уничтожаем пулю
        }
        
        Destroy(gameObject);
    }
    
    // Для пуль с физикой
    void OnCollisionEnter2D(Collision2D collision)
    {
        // То же самое, но для физических столкновений
        OnTriggerEnter2D(collision.collider);
    }
}