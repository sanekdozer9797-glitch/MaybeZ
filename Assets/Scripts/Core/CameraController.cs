using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    
    [Header("Position")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, -10);
    [SerializeField] private float positionSmoothTime = 0.1f;
    
    [Header("Rotation Settings")]
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;    // +45° (по часовой)
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;   // -45° (против часовой)
    [SerializeField] private float rotationStep = 45f;
    [SerializeField] private float rotationSmoothTime = 0.15f;
    
    [Header("Zoom Settings")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float defaultZoom = 5f;
    [SerializeField] private float zoomSmoothTime = 0.1f;
    
    [Header("Look Ahead")]
    [SerializeField] private bool useLookAhead = false; // ОТКЛЮЧЕНО по умолчанию
    [SerializeField] private float lookAheadDistance = 1.5f;
    [SerializeField] private float lookAheadSmoothTime = 0.2f;
    
    // Компоненты
    private Camera cam;
    private PlayerController playerController;
    private Vector3 velocity = Vector3.zero;
    private float targetRotation = 0f;
    private float currentRotation;
    private float rotationVelocity;
    private float targetZoom;
    private float zoomVelocity;
    private Vector2 lookAheadVelocity;
    private Vector2 currentLookAhead;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        if (target == null)
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObj)
            {
                target = targetObj.transform;
                playerController = targetObj.GetComponent<PlayerController>();
            }
        }
        
        if (cam.orthographic)
        {
            cam.orthographicSize = defaultZoom;
            targetZoom = defaultZoom;
        }
        
        currentRotation = 0f;
    }
    
    void Update()
    {
        HandleRotationInput();
        HandleZoom();
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        UpdateCamera();
    }
    
    void HandleRotationInput()
    {
        if (Input.GetKeyDown(rotateLeftKey))
        {
            targetRotation += rotationStep;
        }
        
        if (Input.GetKeyDown(rotateRightKey))
        {
            targetRotation -= rotationStep;
        }
    }
    
    void UpdateCamera()
    {
        Vector3 targetPosition = target.position + positionOffset;
        
        // LOOK AHEAD (если включен)
        if (useLookAhead && playerController != null)
        {
            // Используем moveInput напрямую, т.к. это направление относительно камеры
            Vector2 moveDirection = playerController.MoveInput;
            
            if (moveDirection.magnitude > 0.1f)
            {
                // Поворачиваем опережение
                Vector2 rotatedDirection = Quaternion.Euler(0, 0, currentRotation) * moveDirection;
                Vector2 desiredLookAhead = rotatedDirection * lookAheadDistance;
                
                currentLookAhead = Vector2.SmoothDamp(
                    currentLookAhead, 
                    desiredLookAhead, 
                    ref lookAheadVelocity, 
                    lookAheadSmoothTime
                );
                
                targetPosition += (Vector3)currentLookAhead;
            }
            else
            {
                currentLookAhead = Vector2.SmoothDamp(
                    currentLookAhead, 
                    Vector2.zero, 
                    ref lookAheadVelocity, 
                    lookAheadSmoothTime * 0.5f
                );
                targetPosition += (Vector3)currentLookAhead;
            }
        }
        
        // Позиция
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            positionSmoothTime
        );
        
        // Поворот
        currentRotation = Mathf.SmoothDampAngle(
            currentRotation,
            targetRotation,
            ref rotationVelocity,
            rotationSmoothTime
        );
        
        transform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }
    
    void HandleZoom()
    {
        if (!enableZoom || !cam.orthographic) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
        
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetZoom,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }
}