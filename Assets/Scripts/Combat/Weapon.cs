using UnityEngine;

[System.Serializable]
public class WeaponData
{
    [Header("Basic Info")]
    public string weaponName = "Pistol";
    public WeaponType weaponType = WeaponType.Pistol;
    
    [Header("Firing")]
    public float fireRate = 0.2f;      // Выстрелов в секунду
    public int bulletsPerShot = 1;     // Для дробовиков
    public float spreadAngle = 0f;     // Разброс в градусах
    public float bulletSpeed = 15f;
    public bool automatic = false;     // Автоматический огонь?
    public int burstCount = 1;         // Для очередей (3 = 3 выстрела за нажатие)
    public float burstDelay = 0.1f;    // Задержка между выстрелами в очереди
    
    [Header("Ammo")]
    public int maxAmmo = 30;
    public int clipSize = 10;
    public float reloadTime = 1.5f;
    public bool infiniteAmmo = false;
    
    [Header("Visual")]
    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;
    public Vector3 equippedOffset = new Vector3(0.3f, 0, 0); // Смещение при подборе
    public Vector3 equippedRotation = Vector3.zero;
    
    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
}

public enum WeaponType { Pistol, Shotgun, MachineGun, Rifle, Launcher }

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform firePoint; // Точка выстрела на оружии
    
    // Текущее состояние
    private int currentAmmo;
    private int currentClip;
    private bool isReloading = false;
    private float nextFireTime = 0f;
    private bool isEquipped = false;
    
    // Ссылки
    private Camera mainCamera;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    
    // Для очередей
    private int burstCounter = 0;
    private float burstTimer = 0f;
    
    void Start()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentAmmo = weaponData.maxAmmo;
        currentClip = Mathf.Min(weaponData.clipSize, currentAmmo);
        
        UpdateAmmoDisplay();
    }
    
    void Update()
    {
        if (!isEquipped) return;
        
        UpdateBurstFire();
        
        if (isReloading) return;
        
        // Автоматическая перезарядка при пустом магазине
        if (currentClip <= 0 && weaponData.automatic)
        {
            StartCoroutine(Reload());
            return;
        }
        
        // Проверка на выстрел
        bool tryingToShoot = weaponData.automatic ? 
            Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
            
        if (tryingToShoot && Time.time >= nextFireTime)
        {
            TryShoot();
        }
        
        // Ручная перезарядка
        if (Input.GetKeyDown(KeyCode.R) && currentClip < weaponData.clipSize && currentAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }
    
    void TryShoot()
    {
        if (currentClip <= 0)
        {
            PlaySound(weaponData.emptySound);
            return;
        }
        
        // Запускаем очередь или одиночный выстрел
        if (weaponData.burstCount > 1)
        {
            burstCounter = weaponData.burstCount;
        }
        else
        {
            Shoot();
        }
        
        // Обновляем таймер следующего выстрела
        nextFireTime = Time.time + weaponData.fireRate;
    }
    
    void UpdateBurstFire()
    {
        if (burstCounter > 0)
        {
            burstTimer -= Time.deltaTime;
            
            if (burstTimer <= 0f)
            {
                Shoot();
                burstCounter--;
                burstTimer = weaponData.burstDelay;
            }
        }
    }
    
    void Shoot()
    {
        // Вычисляем направление от точки выстрела к курсору
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 firePointPos = firePoint.position;
        
        // Основное направление
        Vector2 baseDirection = (mouseWorldPos - firePointPos).normalized;
        
        // Создаем несколько пуль (для дробовика)
        for (int i = 0; i < weaponData.bulletsPerShot; i++)
        {
            Vector2 shootDirection = baseDirection;
            
            // Добавляем разброс
            if (weaponData.spreadAngle > 0)
            {
                float randomAngle = Random.Range(-weaponData.spreadAngle / 2f, weaponData.spreadAngle / 2f);
                shootDirection = Quaternion.Euler(0, 0, randomAngle) * baseDirection;
            }
            
            CreateBullet(firePointPos, shootDirection);
        }
        
        // Эффекты
        CreateMuzzleFlash();
        PlaySound(weaponData.shootSound);
        
        // Отдача (опционально)
        ApplyRecoil();
        
        // Расход патронов
        if (!weaponData.infiniteAmmo)
        {
            currentClip--;
            UpdateAmmoDisplay();
        }
    }
    
    void CreateBullet(Vector2 position, Vector2 direction)
    {
        if (weaponData.bulletPrefab == null) return;
        
        GameObject bullet = Instantiate(weaponData.bulletPrefab, position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        
        if (bulletScript != null)
        {
            // Передаем данные оружия в пулю
            bulletScript.Initialize(direction, weaponData.bulletSpeed, this);
        }
    }
    
    void CreateMuzzleFlash()
    {
        if (weaponData.muzzleFlashPrefab == null) return;
        
        GameObject flash = Instantiate(weaponData.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
        flash.transform.SetParent(firePoint);
        
        // Автоуничтожение эффекта
        Destroy(flash, 0.1f);
    }
    
    System.Collections.IEnumerator Reload()
    {
        if (isReloading || currentClip == weaponData.clipSize || currentAmmo <= 0) yield break;
        
        isReloading = true;
        PlaySound(weaponData.reloadSound);
        
        // Можно добавить анимацию перезарядки здесь
        Debug.Log($"Reloading {weaponData.weaponName}...");
        
        yield return new WaitForSeconds(weaponData.reloadTime);
        
        // Пересчитываем патроны
        int neededAmmo = weaponData.clipSize - currentClip;
        int ammoToAdd = Mathf.Min(neededAmmo, currentAmmo);
        
        currentClip += ammoToAdd;
        if (!weaponData.infiniteAmmo)
        {
            currentAmmo -= ammoToAdd;
        }
        
        isReloading = false;
        UpdateAmmoDisplay();
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void ApplyRecoil()
    {
        // Простая отдача - смещение оружия
        if (firePoint != null)
        {
            // Можно анимировать
        }
    }
    
    void UpdateAmmoDisplay()
    {
        // Здесь можно обновлять UI
        // GameManager.Instance.UpdateAmmoUI(currentClip, currentAmmo);
    }
    
    // Публичные методы для управления
    public void Equip(Transform parent, Vector3 localPosition)
    {
        transform.SetParent(parent);
        transform.localPosition = weaponData.equippedOffset + localPosition;
        transform.localRotation = Quaternion.Euler(weaponData.equippedRotation);
        transform.localScale = Vector3.one;
        
        isEquipped = true;
        enabled = true;
        
        // Включаем коллайдер, если есть
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = 10; // Перед игроком
    }
    
    public void Drop()
    {
        transform.SetParent(null);
        isEquipped = false;
        enabled = false;
        
        // Включаем физику и коллайдер
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0.5f;
        rb.angularDrag = 2f;
        rb.drag = 1f;
        
        // Случайное вращение при падении
        rb.angularVelocity = Random.Range(-180f, 180f);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.enabled = true;
        col.isTrigger = false;
        
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = 1; // Под игроком
    }
    
    // Геттеры для UI
    public string GetAmmoText()
    {
        return $"{currentClip}/{currentAmmo}";
    }
    
    public bool CanBePickedUp()
    {
        return !isEquipped;
    }
}