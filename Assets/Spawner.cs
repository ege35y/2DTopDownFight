using UnityEngine;

public class ContinuousSpawner2D : MonoBehaviour
{
    [Header("Düşman Prefab'ı")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Dinamik Süre")]
    [SerializeField] private float initialSpawnTime = 3.5f;
    [SerializeField] private float minimumSpawnTime = 0.8f;
    [SerializeField] private float timeToReachMaxDifficulty = 120f;

    [Header("Çit Sınırları")]
    [SerializeField] private float minX = -23f;
    [SerializeField] private float maxX = 25f;
    [SerializeField] private float minY = -18f;
    [SerializeField] private float maxY = 27f;
    [SerializeField] private float borderPadding = 1f;
    [SerializeField] private float minDistanceFromPlayer = 4f;

    private Transform playerTransform;
    private float elapsedTime = 0f;
    private float spawnTimer = 0f;
    private float nextSpawnInterval = 3f;

    void Start()
    {
        FindPlayer();
        nextSpawnInterval = initialSpawnTime;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        // Player öldüyse veya referans koptuysa yeniden bulmayı dene
        if (playerTransform == null)
        {
            FindPlayer();
        }

        // Zamanı geldiğinde spawn et
        if (spawnTimer >= nextSpawnInterval)
        {
            spawnTimer = 0f;

            // Bir sonraki spawn aralığını hesapla
            float difficultyPercent = Mathf.Clamp01(elapsedTime / timeToReachMaxDifficulty);
            float baseInterval = Mathf.Lerp(initialSpawnTime, minimumSpawnTime, difficultyPercent);
            nextSpawnInterval = Random.Range(baseInterval * 0.85f, baseInterval * 1.15f);

            SpawnEnemy();
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Spawner üzerinde Enemy Prefab atanmamış!");
            return;
        }

        Vector2 spawnPos = Vector2.zero;
        bool validPos = false;
        int maxAttempts = 15;
        int currentAttempt = 0;

        float safeMinX = minX + borderPadding;
        float safeMaxX = maxX - borderPadding;
        float safeMinY = minY + borderPadding;
        float safeMaxY = maxY - borderPadding;

        while (!validPos && currentAttempt < maxAttempts)
        {
            float randomX = Random.Range(safeMinX, safeMaxX);
            float randomY = Random.Range(safeMinY, safeMaxY);
            spawnPos = new Vector2(randomX, randomY);

            // Player sahnede aktif mi kontrolü (Hata vermesini engeller)
            if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
            {
                if (Vector2.Distance(spawnPos, playerTransform.position) >= minDistanceFromPlayer)
                {
                    validPos = true;
                }
            }
            else
            {
                // Player yoksa veya öldüyse de spawn etmeye devam et
                validPos = true;
            }

            currentAttempt++;
        }

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}