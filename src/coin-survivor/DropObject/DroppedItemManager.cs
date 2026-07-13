using System.Collections.Generic;
using UnityEngine;

public class DroppedItemManager : MonoBehaviour
{
    [Header("보상 설정")]
    public GameObject coinPrefab;
    public GameObject diamondPrefab;

    [SerializeField] private Transform coinRoot;

    [Header("다이아 드롭 설정")]
    [SerializeField] private float diamondDropChance = 0.001f;
    [SerializeField] private int diamondAmount = 1;

    public static DroppedItemManager Instance { get; private set; }

    public readonly Dictionary<ItemType, List<DropSaveData>> dropItemDict = new Dictionary<ItemType, List<DropSaveData>>();
    public readonly Dictionary<Vector2Int, List<DropSaveData>> dropsByChunk = new Dictionary<Vector2Int, List<DropSaveData>>();
    public readonly Dictionary<Vector2Int, List<CoinPickup>> spawnedCoinsBychunk = new Dictionary<Vector2Int, List<CoinPickup>>();

    private MapDatabase mapDatabase;
    private MapData mapData;
    private float chunkWorldSize;

    [SerializeField] private int currentMapID = 1;
    [SerializeField] private int poolSize = 100;
    [SerializeField] private int diamondPoolSize = 20;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeMapData();

        if (mapData == null)
        {
            enabled = false;
            return;
        }

        chunkWorldSize = mapData.chunkSize;

        GameEventBus.OnEnemyKilled += SpawnReward;
    }

    private void Start()
    {
        if (coinPrefab != null)
        {
            ObjectPoolManager.Instance.InitPool(coinPrefab, poolSize, coinRoot);
        }

        if (diamondPrefab != null)
        {
            ObjectPoolManager.Instance.InitPool(diamondPrefab, diamondPoolSize, coinRoot);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            GameEventBus.OnEnemyKilled -= SpawnReward;
        }
    }

    private void InitializeMapData()
    {
        mapDatabase = new MapDatabase();
        mapDatabase.Initialize();

        mapData = mapDatabase.GetMapData(currentMapID);
    }

    private Vector2Int CalculateChunkCoord(Vector3 worldPos)
    {
        int chunkX = Mathf.FloorToInt(worldPos.x / chunkWorldSize);
        int chunkY = Mathf.FloorToInt(worldPos.y / chunkWorldSize);

        return new Vector2Int(chunkX, chunkY);
    }

    public DropSaveData SaveDropData(ItemType type, Vector3 pos, Vector2Int chunkCoord, int amount, bool isCollected)
    {
        if (!dropItemDict.ContainsKey(type))
        {
            dropItemDict[type] = new List<DropSaveData>();
        }

        if (!dropsByChunk.ContainsKey(chunkCoord))
        {
            dropsByChunk[chunkCoord] = new List<DropSaveData>();
        }

        DropSaveData data = new DropSaveData
        {
            dropType = type,
            position = pos,
            chunkCoord = chunkCoord,
            amount = amount,
            isCollected = isCollected
        };

        dropItemDict[type].Add(data);
        dropsByChunk[chunkCoord].Add(data);

        return data;
    }

    private void SpawnReward(Vector3 dropPos, int rewardGold)
    {
        SpawnGold(dropPos, rewardGold);
        TrySpawnDiamond(dropPos);
    }

    private void SpawnGold(Vector3 dropPos, int rewardGold)
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("보상으로 줄 코인 프리팹이 연결되지 않았습니다.");
            return;
        }

        int amount = rewardGold;
        Vector2Int chunkCoord = CalculateChunkCoord(dropPos);

        DropSaveData data = SaveDropData(ItemType.Coin, dropPos, chunkCoord, amount, false);
        SpawnDropObject(data);
    }

    private void TrySpawnDiamond(Vector3 dropPos)
    {
        if (diamondPrefab == null)
            return;

        if (Random.value > diamondDropChance)
            return;

        Vector3 randomOffset = Random.insideUnitCircle * 0.4f;
        Vector3 diamondPos = dropPos + randomOffset;

        Vector2Int chunkCoord = CalculateChunkCoord(diamondPos);

        DropSaveData data = SaveDropData(ItemType.Diamond, diamondPos, chunkCoord, diamondAmount, false);
        SpawnDropObject(data);
    }

    private void SpawnDropObject(DropSaveData data)
    {
        if (data == null)
            return;

        GameObject prefab = GetDropPrefab(data.dropType);

        if (prefab == null)
            return;

        GameObject dropObj = ObjectPoolManager.Instance.Get(prefab, data.position, Quaternion.identity);

        CoinPickup pickup = dropObj.GetComponent<CoinPickup>();

        if (pickup == null)
        {
            pickup = dropObj.AddComponent<CoinPickup>();
        }

        pickup.Initialize(data, this);

        Vector2Int chunkCoord = data.chunkCoord;

        if (!spawnedCoinsBychunk.ContainsKey(chunkCoord))
        {
            spawnedCoinsBychunk[chunkCoord] = new List<CoinPickup>();
        }

        spawnedCoinsBychunk[chunkCoord].Add(pickup);
    }

    private GameObject GetDropPrefab(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Coin:
                return coinPrefab;

            case ItemType.Diamond:
                return diamondPrefab;

            default:
                return null;
        }
    }

    public void RespawnDropsInChunk(Vector2Int chunkCoord)
    {
        if (!dropsByChunk.TryGetValue(chunkCoord, out List<DropSaveData> dropList))
            return;

        if (spawnedCoinsBychunk.TryGetValue(chunkCoord, out List<CoinPickup> spawnedList))
        {
            spawnedList.RemoveAll(p => p == null);

            if (spawnedList.Count > 0)
                return;
        }

        foreach (DropSaveData data in dropList)
        {
            if (!data.isCollected)
            {
                SpawnDropObject(data);
            }
        }
    }

    public void ClearSpawnedDropsInChunk(Vector2Int chunkCoord)
    {
        if (!spawnedCoinsBychunk.TryGetValue(chunkCoord, out List<CoinPickup> spawnedList))
        {
            return;
        }

        foreach (CoinPickup pickup in spawnedList)
        {
            if (pickup != null)
            {
                pickup.gameObject.transform.SetParent(coinRoot);
                ObjectPoolManager.Instance.ReturnToPool(pickup.gameObject);
            }
        }

        spawnedList.Clear();
    }

    public void MarkCollected(DropSaveData data, CoinPickup pickup)
    {
        if (data != null)
        {
            data.isCollected = true;

            if (spawnedCoinsBychunk.TryGetValue(data.chunkCoord, out List<CoinPickup> spawnedList))
            {
                spawnedList.Remove(pickup);
            }
        }

        if (pickup != null)
        {
            pickup.gameObject.transform.SetParent(coinRoot);
            ObjectPoolManager.Instance.ReturnToPool(pickup.gameObject);
        }
    }

    public void ClearAllDrops()
    {
        foreach (var pair in spawnedCoinsBychunk)
        {
            List<CoinPickup> spawnedList = pair.Value;

            foreach (CoinPickup pickup in spawnedList)
            {
                if (pickup != null)
                {
                    ObjectPoolManager.Instance.ReturnToPool(pickup.gameObject);
                }
            }

            spawnedList.Clear();
        }

        dropItemDict.Clear();
        dropsByChunk.Clear();
        spawnedCoinsBychunk.Clear();
    }
}
