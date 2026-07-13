using System.Collections.Generic;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    private ObjectPoolManager poolManager;
    private EnemyDatabase enemyDatabase;

    [Header("Data")]
    [SerializeField] private StageTimelineSO currentTimeline;

    [Header("Factory Settings")]
    [SerializeField] private int normalEnemyPoolCount = 80;
    [SerializeField] private int eventEnemyPoolCount = 10;
    [SerializeField] private int bossPoolCount = 1;

    private readonly Dictionary<int, GameObject> enemyPrefabCache = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, int> aliveCounts = new Dictionary<int, int>();

    private readonly Dictionary<int, string> pathCache = new Dictionary<int, string>();

    [Header("Root Transforms")]
    [SerializeField] private Transform enemyRoot;
    public static Transform EnemyRoot { get; private set; }

    private void Start()
    {
        ResetAliveCounts();

        poolManager = ObjectPoolManager.Instance;

        if (poolManager == null)
        {
            enabled = false;
            return;
        }

        enemyDatabase = new EnemyDatabase();
        enemyDatabase.Initialize();

        PrewarmEnemiesFromTimeline();
    }

    private void PrewarmEnemiesFromTimeline()
    {
        if (currentTimeline == null)
        {
            PrewarmEnemyPool(1, normalEnemyPoolCount);
            return;
        }

        HashSet<int> normalEnemyIDs = new HashSet<int>();
        Dictionary<int, int> eventEnemyCounts = new Dictionary<int, int>();

        foreach (var wave in currentTimeline.continuousWaves)
        {
            normalEnemyIDs.Add(wave.enemyID);
        }

        foreach (var waveEvent in currentTimeline.oneTimeEvents)
        {
            int count = waveEvent.pattern == SpawnPattern.Boss
                ? bossPoolCount
                : Mathf.Max(eventEnemyPoolCount, waveEvent.spawnCount);

            if (!eventEnemyCounts.ContainsKey(waveEvent.enemyID))
                eventEnemyCounts[waveEvent.enemyID] = count;
            else
                eventEnemyCounts[waveEvent.enemyID] = Mathf.Max(eventEnemyCounts[waveEvent.enemyID], count);
        }

        foreach (int enemyID in normalEnemyIDs)
        {
            PrewarmEnemyPool(enemyID, normalEnemyPoolCount);
        }

        foreach (var pair in eventEnemyCounts)
        {
            if (normalEnemyIDs.Contains(pair.Key))
                continue;

            PrewarmEnemyPool(pair.Key, pair.Value);
        }
    }

    private void PrewarmEnemyPool(int enemyID, int count)
    {
        if (enemyDatabase == null)
        {
            return;
        }

        EnemyData enemyData = enemyDatabase.GetEnemyData(enemyID);

        if (enemyData == null)
        {
            return;
        }

        GameObject prefab = LoadEnemyPrefab(enemyID);

        if (prefab == null)
        {
            return;
        }

        poolManager.InitPool(prefab, count, enemyRoot);

    }

    public void SpawnEnemy(int enemyID, Vector3 spawnPos)
    {
        if (poolManager == null)
        {
            poolManager = ObjectPoolManager.Instance;

            if (poolManager == null)
            {
                return;
            }
        }

        if (enemyDatabase == null)
        {
            enemyDatabase = new EnemyDatabase();
            enemyDatabase.Initialize();
        }

        EnemyData enemyData = enemyDatabase.GetEnemyData(enemyID);

        if (enemyData == null)
        {
            return;
        }

        GameObject prefab = LoadEnemyPrefab(enemyID);

        if (prefab == null)
            return;

        GameObject enemyObj = poolManager.Get(prefab, spawnPos, Quaternion.identity);

        if (enemyObj == null)
        {
            return;
        }

        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            enemy = enemyObj.GetComponentInChildren<Enemy>();

        if (enemy == null)
        {
            return;
        }

        enemy.Initialize(enemyData, enemyID, this);

        if (EnemyRuntimeManager.Instance != null)
            EnemyRuntimeManager.Instance.Register(enemy);

        RegisterEnemy(enemyID);
    }

    public int GetAliveCount(int enemyID)
    {
        return aliveCounts.TryGetValue(enemyID, out int count) ? count : 0;
    }

    private void RegisterEnemy(int enemyID)
    {
        if (!aliveCounts.ContainsKey(enemyID))
            aliveCounts[enemyID] = 0;

        aliveCounts[enemyID]++;
    }

    public void UnregisterEnemy(int enemyID)
    {
        if (!aliveCounts.ContainsKey(enemyID))
            return;

        aliveCounts[enemyID]--;

        if (aliveCounts[enemyID] < 0)
            aliveCounts[enemyID] = 0;
    }

    public void ResetAliveCounts()
    {
        aliveCounts.Clear();

        Debug.Log("<color=orange>[EnemyFactory] Alive Counts Reset</color>");
    }

    private GameObject LoadEnemyPrefab(int enemyID)
    {
        if (enemyPrefabCache.TryGetValue(enemyID, out GameObject cachedPrefab))
            return cachedPrefab;

        //string path = $"Enemies/Enemy_{enemyID}";
        if (!pathCache.TryGetValue(enemyID, out string path))
        {
            path = $"Enemies/Enemy_{enemyID}";
            pathCache[enemyID] = path;
        }

        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            return null;
        }

        enemyPrefabCache.Add(enemyID, prefab);
        return prefab;
    }
}
