using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave 1";
        public int zombieCount = 5;
        public float timeBetweenSpawns = 2f;
        public GameObject[] zombiePrefabs; // Разные типы зомби
        public float waveCooldown = 10f; // Отдых между волнами
    }
    
    [Header("Wave Settings")]
    [SerializeField] private Wave[] waves;
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool autoStartWaves = true;
    
    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float minDistanceFromPlayer = 5f;
    [SerializeField] private LayerMask obstacleMask;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform zombieContainer; // Папка для зомби
    
    private List<GameObject> activeZombies = new List<GameObject>();
    private bool isWaveActive = false;
    private int zombiesLeftInWave = 0;
    
    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Text waveText;
    [SerializeField] private UnityEngine.UI.Text zombiesLeftText;
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) player = playerObj.transform;
        }
        
        if (zombieContainer == null)
        {
            zombieContainer = new GameObject("Zombies").transform;
        }
        
        if (autoStartWaves)
        {
            StartNextWave();
        }
    }
    
    void Update()
    {
        UpdateUI();
        CheckWaveCompletion();
    }
    
    public void StartNextWave()
    {
        if (isWaveActive || currentWaveIndex >= waves.Length) return;
        
        Wave currentWave = waves[currentWaveIndex];
        StartCoroutine(SpawnWave(currentWave));
        
        Debug.Log($"Starting {currentWave.waveName} with {currentWave.zombieCount} zombies");
    }
    
    IEnumerator SpawnWave(Wave wave)
    {
        isWaveActive = true;
        zombiesLeftInWave = wave.zombieCount;
        
        for (int i = 0; i < wave.zombieCount; i++)
        {
            SpawnZombie(wave);
            yield return new WaitForSeconds(wave.timeBetweenSpawns);
        }
        
        // Ждем пока все зомби умрут
        while (zombiesLeftInWave > 0)
        {
            yield return new WaitForSeconds(1f);
        }
        
        isWaveActive = false;
        
        // Отдых между волнами
        yield return new WaitForSeconds(wave.waveCooldown);
        
        currentWaveIndex++;
        if (currentWaveIndex < waves.Length)
        {
            StartNextWave();
        }
        else
        {
            Debug.Log("All waves completed! Game Over?");
        }
    }
    
    void SpawnZombie(Wave wave)
    {
        Vector2 spawnPosition = GetValidSpawnPosition();
        if (spawnPosition == Vector2.zero) return;
        
        // Выбираем случайного зомби из префабов
        GameObject zombiePrefab = wave.zombiePrefabs.Length > 0 
            ? wave.zombiePrefabs[Random.Range(0, wave.zombiePrefabs.Length)]
            : null;
        
        if (zombiePrefab == null)
        {
            Debug.LogError("No zombie prefab assigned!");
            return;
        }
        
        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity, zombieContainer);
        
        // Настраиваем зомби
        ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            // Можно настроить параметры зомби в зависимости от волны
            zombieAI.name = $"Zombie_Wave{currentWaveIndex + 1}";
        }
        
        // Отслеживаем смерть зомби
        Health zombieHealth = zombie.GetComponent<Health>();
        if (zombieHealth != null)
        {
            zombieHealth.OnDeath.AddListener(() => OnZombieDied(zombie));
        }
        
        activeZombies.Add(zombie);
    }
    
    Vector2 GetValidSpawnPosition()
    {
        for (int i = 0; i < 30; i++) // 30 попыток
        {
            // Случайная точка в радиусе
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector2 spawnPos = (Vector2)transform.position + randomCircle;
            
            // Проверяем расстояние до игрока
            if (Vector2.Distance(spawnPos, player.position) < minDistanceFromPlayer)
                continue;
            
            // Проверяем нет ли препятствий
            Collider2D hit = Physics2D.OverlapCircle(spawnPos, 0.5f, obstacleMask);
            if (hit != null)
                continue;
            
            // Проверяем видимость от игрока (не спавнить прямо перед ним)
            Vector2 dirToPlayer = (spawnPos - (Vector2)player.position).normalized;
            float dot = Vector2.Dot(dirToPlayer, player.up);
            if (dot > 0.7f) // Если точка перед игроком
                continue;
            
            return spawnPos;
        }
        
        Debug.LogWarning("Could not find valid spawn position!");
        return Vector2.zero;
    }
    
    void OnZombieDied(GameObject zombie)
    {
        activeZombies.Remove(zombie);
        zombiesLeftInWave--;
        
        // Даем игроку очки/ресурсы за убийство
        if (player != null)
        {
            // Здесь можно добавить систему очков
            Debug.Log($"Zombie killed! {zombiesLeftInWave} zombies left in wave");
        }
    }
    
    void CheckWaveCompletion()
    {
        // Удаляем мертвых зомби из списка
        activeZombies.RemoveAll(z => z == null);
    }
    
    void UpdateUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {currentWaveIndex + 1}/{waves.Length}";
        }
        
        if (zombiesLeftText != null && isWaveActive)
        {
            zombiesLeftText.text = $"Zombies Left: {zombiesLeftInWave}";
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Зона спавна
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Минимальное расстояние от игрока
        if (player != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);
        }
    }
}