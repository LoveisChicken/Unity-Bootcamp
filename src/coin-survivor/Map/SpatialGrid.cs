using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{
    public static SpatialGrid Instance { get; private set; }

    // References
    private MapDatabase mapDatabase;
    private MapData mapData;

    [Header("Setting")]
    [SerializeField] private int currentMapID = 1;  // need to change for mapID
    private float chunkWorldSize;   // cellSize = chunkWorldSize -> TODO: need to balance size if we choose the tile asset

    private Dictionary<Vector2Int, List<Enemy>> enemyGrid = new Dictionary<Vector2Int, List<Enemy>>();

    private List<Enemy> nearbyEnemiesBuffer = new List<Enemy>(100);
    private Queue<List<Enemy>> listPool = new Queue<List<Enemy>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeMapData();
        if (!ValidateSettings())
            return;

        chunkWorldSize = mapData.chunkSize;     // mapData.tileSize * mapData.chunkSize;

    }
    private void InitializeMapData()
    {
        mapDatabase = new MapDatabase();
        mapDatabase.Initialize();

        mapData = mapDatabase.GetMapData(currentMapID);

        if (mapData == null)
        {
            Debug.LogWarning($"MapChunkManager: mapID {currentMapID} 데이터를 찾지 못했습니다.");
        }
    }

    private bool ValidateSettings()
    {
        if (mapData == null)
        {
            Debug.LogWarning("MapChunkManager: mapData가 없습니다.");
            return false;
        }


        if (mapData.chunkSize <= 0)
        {
            Debug.LogWarning("MapChunkManager: chunkSize는 0보다 커야 합니다.");
            return false;
        }

        return true;
    }

    private bool ValidateRuntime()
    {
        return chunkWorldSize > 0f;
    }

    public void Add(Enemy enemy)
    {
        AddToGrid(enemy, WorldToGrid(enemy.WorldPosition));
    }

    private List<Enemy> GetListFromPool()
    {
        return listPool.Count > 0 ? listPool.Dequeue() : new List<Enemy>();
    }

    private void AddToGrid(Enemy enemy, Vector2Int gridPos)
    {
        if (enemy != null)
        {
            if (!enemyGrid.TryGetValue(gridPos, out var enemyList))
            {
                enemyList = GetListFromPool(); // new 대신 풀에서
                enemyGrid[gridPos] = enemyList;
            }
            enemyList.Add(enemy);
        }
    }

    public void Remove(Enemy enemy)
    {
        RemoveFromGrid(enemy, WorldToGrid(enemy.WorldPosition));
    }

    private void RemoveFromGrid(Enemy enemy, Vector2Int gridPos)
    {
        if (enemyGrid.TryGetValue(gridPos, out var enemyList))
        {
            enemyList.Remove(enemy);
            if (enemyList.Count == 0)
            {
                enemyGrid.Remove(gridPos);
                listPool.Enqueue(enemyList); // 풀에 반납
            }
        }
    }

    public List<Enemy> GetNearby(Vector3 pos, float radius)
    {
        nearbyEnemiesBuffer.Clear();

        int nearbyGridX = Mathf.FloorToInt(pos.x / chunkWorldSize);
        int nearbyGridY = Mathf.FloorToInt(pos.y / chunkWorldSize);
        int nearbyGridRadius = Mathf.CeilToInt(radius / chunkWorldSize);
        Vector2Int centerGrid = new Vector2Int(nearbyGridX, nearbyGridY);

        for (int y = -nearbyGridRadius; y <= nearbyGridRadius; y++)
        {
            for (int x = -nearbyGridRadius; x <= nearbyGridRadius; x++)
            {
                Vector2Int gridPos = new Vector2Int(centerGrid.x + x, centerGrid.y + y);
                if (enemyGrid.TryGetValue(gridPos, out var enemyList))
                    nearbyEnemiesBuffer.AddRange(enemyList);
            }
        }

        return nearbyEnemiesBuffer;
    }

    public void UpdatePosition(Enemy enemy, Vector2Int oldPos, Vector2Int newPos)
    {
        RemoveFromGrid(enemy, oldPos);
        AddToGrid(enemy, newPos);
    }

    // 헬퍼 메서드 월드 좌표를 격자 좌표로 변환 (Enemy에서 사용)
    public Vector2Int WorldToGrid(Vector3 pos)
    {
        if (!ValidateRuntime())
            return Vector2Int.zero;

        int gridX = Mathf.FloorToInt(pos.x / chunkWorldSize);
        int gridY = Mathf.FloorToInt(pos.y / chunkWorldSize);

        return new Vector2Int(gridX, gridY);
    }

}
