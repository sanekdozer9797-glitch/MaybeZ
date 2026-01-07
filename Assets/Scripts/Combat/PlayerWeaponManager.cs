using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private float pickupRadius = 1f;
    [SerializeField] private KeyCode pickupKey = KeyCode.F;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private KeyCode switchKey = KeyCode.Q;
    
    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Text ammoText;
    [SerializeField] private UnityEngine.UI.Text weaponNameText;
    
    private Weapon currentWeapon;
    private Weapon lastHighlightedWeapon;
    private GameObject nearbyWeapon;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        FindNearbyWeapons();
        HandleInput();
        UpdateUI();
    }
    
void FindNearbyWeapons()
{
    // Сбрасываем подсветку у предыдущего оружия
    if (lastHighlightedWeapon != null && lastHighlightedWeapon.gameObject != nearbyWeapon)
    {
        SpriteRenderer lastSR = lastHighlightedWeapon.GetComponent<SpriteRenderer>();
        if (lastSR != null)
        {
            lastSR.color = Color.white; // Возвращаем исходный цвет
        }
    }
    
    nearbyWeapon = null;
    
    Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
    
    float closestDistance = Mathf.Infinity;
    
    foreach (Collider2D col in colliders)
    {
        Weapon weapon = col.GetComponent<Weapon>();
        if (weapon != null && weapon.CanBePickedUp())
        {
            float distance = Vector2.Distance(transform.position, weapon.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearbyWeapon = weapon.gameObject;
            }
        }
    }
    
    // Подсвечиваем новое ближайшее оружие
    if (nearbyWeapon != null)
    {
        Weapon weapon = nearbyWeapon.GetComponent<Weapon>();
        lastHighlightedWeapon = weapon;
        
        SpriteRenderer sr = nearbyWeapon.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1, 1, 0.5f, 1);
        }
    }
}
    
    void HighlightNearbyWeapon()
    {
        // Можно добавить свечение или подсветку
        if (nearbyWeapon != null)
        {
            SpriteRenderer sr = nearbyWeapon.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1, 1, 0.5f, 1); // Желтоватый оттенок
            }
        }
    }
    
    void HandleInput()
    {
        // Подбор оружия
        if (Input.GetKeyDown(pickupKey) && nearbyWeapon != null)
        {
            PickupWeapon(nearbyWeapon);
        }
        
        // Выброс оружия
        if (Input.GetKeyDown(dropKey) && currentWeapon != null)
        {
            DropCurrentWeapon();
        }
    }
    
    void PickupWeapon(GameObject weaponObj)
    {
        // Если уже есть оружие - выбрасываем его
        if (currentWeapon != null)
        {
            DropCurrentWeapon();
        }
        
        Weapon weapon = weaponObj.GetComponent<Weapon>();
        if (weapon != null)
        {
            currentWeapon = weapon;
            
            // Удаляем физику с подобранного оружия
            Rigidbody2D rb = weaponObj.GetComponent<Rigidbody2D>();
            if (rb != null) Destroy(rb);
            
            Collider2D col = weaponObj.GetComponent<Collider2D>();
            if (col != null) Destroy(col);
            
            // Присоединяем к игроку
            weapon.Equip(weaponHoldPoint, Vector3.zero);
            
            // Обновляем спрайт оружия
            UpdateWeaponSprite();
        }
    }
    
    void DropCurrentWeapon()
    {
        if (currentWeapon != null)
        {
            currentWeapon.Drop();
            currentWeapon = null;
            
            // Можно также скрыть спрайт оружия на игроке
            UpdateWeaponSprite();
        }
    }
    
    void UpdateWeaponSprite()
    {
        // Если есть оружие, показываем его спрайт на игроке
        // Это опционально, если оружие - отдельный объект
    }
    
    void UpdateUI()
    {
        if (ammoText != null)
        {
            if (currentWeapon != null)
            {
                ammoText.text = currentWeapon.GetAmmoText();
                weaponNameText.text = currentWeapon.name;
            }
            else
            {
                ammoText.text = "No Weapon";
                weaponNameText.text = "Fists";
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Радиус подбора оружия
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        
        // Линия к ближайшему оружию
        if (nearbyWeapon != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nearbyWeapon.transform.position);
        }
    }
}