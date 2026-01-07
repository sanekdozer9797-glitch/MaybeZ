using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private float visionAngle = 120f;
    [SerializeField] private float closeSenseRange = 2f;
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private GameObject attackHitbox;
    
    [Header("Patrol Settings")]
    [SerializeField] private bool patrolEnabled = true;
    [SerializeField] private float patrolRadius = 4f;
    
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 patrolTarget;
    private float lastAttackTime;
    private bool canSeePlayer = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("No player found! Make sure player has tag 'Player'");
        
        startPosition = transform.position;
        SetNewPatrolPoint();
        
        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }
    
    void Update()
    {
        if (player == null) return;
        
        CheckForPlayer();
        UpdateState();
    }
    
    void FixedUpdate()
    {
        if (player == null) return;
        
        ExecuteState();
    }
    
    void CheckForPlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= closeSenseRange)
        {
            canSeePlayer = true;
            return;
        }
        
        if (distanceToPlayer > visionRange)
        {
            canSeePlayer = false;
            return;
        }
        
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector2.Angle(transform.up, dirToPlayer);
        
        if (angleToPlayer > visionAngle / 2f)
        {
            canSeePlayer = false;
            return;
        }
        
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dirToPlayer,
            distanceToPlayer,
            wallLayer
        );
        
        Debug.DrawLine(transform.position, player.position, 
            hit.collider == null ? Color.green : Color.red);
        
        canSeePlayer = (hit.collider == null);
    }
    
    void UpdateState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (canSeePlayer && distanceToPlayer <= attackRange)
        {
            currentState = State.Attack;
        }
        else if (canSeePlayer && distanceToPlayer <= visionRange)
        {
            currentState = State.Chase;
        }
        else if (!canSeePlayer && distanceToPlayer > visionRange * 1.5f)
        {
            currentState = State.Patrol;
        }
    }
    
    void ExecuteState()
    {
        switch (currentState)
        {
            case State.Patrol:
                PatrolBehavior();
                break;
            case State.Chase:
                ChaseBehavior();
                break;
            case State.Attack:
                AttackBehavior();
                break;
        }
    }
    
    void PatrolBehavior()
    {
        if (!patrolEnabled) return;
        
        Vector2 direction = (patrolTarget - (Vector2)transform.position).normalized;
        rb.velocity = direction * (moveSpeed * 0.6f);
        
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        
        if (Vector2.Distance(transform.position, patrolTarget) < 0.5f)
        {
            SetNewPatrolPoint();
        }
    }
    
    void ChaseBehavior()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        direction = AvoidWalls(direction);
        
        rb.velocity = direction * moveSpeed;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }
    
    void AttackBehavior()
    {
        rb.velocity = Vector2.zero;
        
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.fixedDeltaTime);
        }
        
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }
    
    Vector2 AvoidWalls(Vector2 desiredDirection)
    {
        if (desiredDirection == Vector2.zero) return desiredDirection;
        
        RaycastHit2D hitForward = Physics2D.Raycast(
            transform.position,
            desiredDirection,
            1.2f,
            wallLayer
        );
        
        if (hitForward.collider == null) return desiredDirection;
        
        Vector2 perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x);
        
        RaycastHit2D hitLeft = Physics2D.Raycast(
            transform.position,
            perpendicular,
            1.2f,
            wallLayer
        );
        
        RaycastHit2D hitRight = Physics2D.Raycast(
            transform.position,
            -perpendicular,
            1.2f,
            wallLayer
        );
        
        if (hitLeft.collider == null)
            return perpendicular;
        else if (hitRight.collider == null)
            return -perpendicular;
        else
            return -desiredDirection;
    }
    
    void SetNewPatrolPoint()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        patrolTarget = startPosition + randomDir * Random.Range(1f, patrolRadius);
    }
    
    void Attack()
    {
        lastAttackTime = Time.time;
        
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(true);
            Invoke(nameof(DisableHitbox), 0.3f);
            
            SpriteRenderer hitboxRenderer = attackHitbox.GetComponent<SpriteRenderer>();
            if (hitboxRenderer != null)
            {
                hitboxRenderer.color = Color.red;
                Invoke(nameof(ResetHitboxColor), 0.2f);
            }
        }
        
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange * 1.2f)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }
    
    void DisableHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }
    
    void ResetHitboxColor()
    {
        if (attackHitbox != null)
        {
            SpriteRenderer hitboxRenderer = attackHitbox.GetComponent<SpriteRenderer>();
            if (hitboxRenderer != null)
            {
                hitboxRenderer.color = new Color(1, 0, 0, 0.3f);
            }
        }
    }
    
    // Добавляем enum State внутри класса
    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeSenseRange);
        
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector3 leftDir = Quaternion.Euler(0, 0, visionAngle * 0.5f) * transform.up;
        Vector3 rightDir = Quaternion.Euler(0, 0, -visionAngle * 0.5f) * transform.up;
        Gizmos.DrawRay(transform.position, leftDir * visionRange);
        Gizmos.DrawRay(transform.position, rightDir * visionRange);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(patrolTarget, 0.2f);
        }
    }
}