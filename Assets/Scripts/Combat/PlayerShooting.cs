using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletForce = 15f;
    [SerializeField] private float shotVolume = 1f; // Громкость выстрела (0-1)
    
    [Header("Audio")]
    [SerializeField] private bool enableShotSound = true;
    [SerializeField] private float shotSoundRange = 20f;
    
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;
        
        // Создаем пулю
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse);
        }
        
        // Оповещаем всех врагов о выстреле
        if (enableShotSound)
        {
            AlertEnemiesToGunshot();
        }
    }

void AlertEnemiesToGunshot()
{
    // ВРЕМЕННО ЗАКОММЕНТИРУЕМ, пока не реализована система звуков
    // ZombieAI[] allEnemies = FindObjectsOfType<ZombieAI>();
    // foreach (ZombieAI enemy in allEnemies)
    // {
    //     if (enemy != null)
    //     {
    //         enemy.ReactToGunshot(transform.position, shotVolume);
    //     }
    // }
    
    Debug.Log("Выстрел! (система реакции врагов на звук пока не реализована)");
}
}