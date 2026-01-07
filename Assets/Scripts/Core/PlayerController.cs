using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 mousePosition;
    private Camera mainCamera;

    // ДЛЯ КАМЕРЫ - публичные свойства
    public bool IsMoving { get; private set; }
    public Vector2 MoveInput { get { return moveInput; } }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("Rigidbody2D not found on Player!");
        
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Считываем ввод с клавиатуры
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // Обновляем свойство IsMoving
        IsMoving = moveInput.magnitude > 0.1f;

        // Получаем позицию мыши
        mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    void FixedUpdate()
    {
        // ПОВОРОТ ПЕРСОНАЖА К КУРСОРУ МЫШИ
        Vector2 lookDirection = mousePosition - rb.position;
        float targetAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg - 90f;
        
        // Плавный поворот персонажа
        rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        
        // ПРАВИЛЬНОЕ КАМЕРА-ОТНОСИТЕЛЬНОЕ ДВИЖЕНИЕ
        Vector2 moveDirection = GetCameraRelativeMovement();
        
        // Применяем движение
        rb.velocity = moveDirection * moveSpeed;
    }
    
    Vector2 GetCameraRelativeMovement()
    {
        if (moveInput.magnitude < 0.1f) return Vector2.zero;
        
        // Преобразуем ввод экрана в мировые координаты с учетом поворота камеры
        Transform camTransform = mainCamera.transform;
        float cameraAngle = camTransform.eulerAngles.z * Mathf.Deg2Rad;
        
        // Матрица вращения для камеры
        Vector2 worldDirection = new Vector2(
            moveInput.x * Mathf.Cos(cameraAngle) - moveInput.y * Mathf.Sin(cameraAngle),
            moveInput.x * Mathf.Sin(cameraAngle) + moveInput.y * Mathf.Cos(cameraAngle)
        );
        
        return worldDirection.normalized;
    }
    
    // МЕТОД ДЛЯ КАМЕРЫ - направление движения относительно камеры
    public Vector2 GetCameraRelativeMoveDirection()
    {
        // Возвращаем просто moveInput - это уже направление относительно камеры
        return moveInput.normalized;
    }
    
    // Метод для получения абсолютного направления движения
    public Vector2 GetWorldMoveDirection()
    {
        return rb.velocity.normalized;
    }
    
    // Визуализация для отладки
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Красный: направление "вверх экрана" для текущей камеры
        if (mainCamera != null)
        {
            float cameraAngle = mainCamera.transform.eulerAngles.z * Mathf.Deg2Rad;
            Vector2 cameraUp = new Vector2(-Mathf.Sin(cameraAngle), Mathf.Cos(cameraAngle));
            Debug.DrawRay(transform.position, cameraUp * 2f, Color.red);
            
            // Зеленый: направление движения
            if (rb.velocity.magnitude > 0.1f)
            {
                Debug.DrawRay(transform.position, rb.velocity.normalized * 1.5f, Color.green);
            }
        }
    }
    
    // Для отладки в GUI
    void OnGUI()
    {
        if (mainCamera != null)
        {
            float cameraAngle = mainCamera.transform.eulerAngles.z;
            GUI.Label(new Rect(10, 10, 300, 20), $"Камера угол: {cameraAngle:F1}°");
            GUI.Label(new Rect(10, 30, 300, 20), $"Ввод: ({moveInput.x:F2}, {moveInput.y:F2})");
            
            Vector2 moveDir = GetCameraRelativeMovement();
            GUI.Label(new Rect(10, 50, 300, 20), $"Движение: ({moveDir.x:F2}, {moveDir.y:F2})");
        }
    }
}