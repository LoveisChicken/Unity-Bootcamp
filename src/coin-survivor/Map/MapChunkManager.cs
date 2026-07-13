using System.Collections.Generic;
using UnityEngine;

public class MapChunkManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int currentMapID = 1;
    [SerializeField] private int viewRadius = 2;
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Transform chunkRoot;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;

    [Header("Pool")]
    [SerializeField] private int poolSize = 40;

    [Header("Delay Setting")]
    [SerializeField] private float releaseDelay = 0.5f;

    private MapDatabase mapDatabase;
    private MapData mapData;

    private MapChunkPool chunkPool = new MapChunkPool();

    private Dictionary<Vector2Int, MapChunk> activeChunks = new Dictionary<Vector2Int, MapChunk>();

    private Dictionary<Vector2Int, float> pendingRelease = new Dictionary<Vector2Int, float>();

    private Vector2Int currentPlayerChunk;

    private List<Vector2Int> toRemove = new List<Vector2Int>();
    private List<Vector2Int> removeList = new List<Vector2Int>();
    private HashSet<Vector2Int> requiredCoords = new HashSet<Vector2Int>();

    private float chunkWorldSize;

    private void Start()
    {
        InitializeMapData();

        if (!ValidateSettings())
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        chunkWorldSize = mapData.chunkSize;
        viewRadius = CalculateRequiredViewRadius();

        int diameter = viewRadius * 2 + 1;
        poolSize = diameter * diameter + 10; // 여유분 으로 10 넣어놨음

        chunkPool.Initialize(chunkPrefab, chunkRoot, poolSize);

        currentPlayerChunk = CalculateCurrentChunk(playerTransform.position);

        UpdateChunks(currentPlayerChunk);
    }

    private void Update()
    {
        if (!ValidateRuntime())
            return;

        Vector3 referencePos = mainCamera != null ? mainCamera.transform.position : playerTransform.position;

        Vector2Int newChunk = CalculateCurrentChunk(playerTransform.position);

        if (newChunk != currentPlayerChunk)
        {
            currentPlayerChunk = newChunk;
            UpdateChunks(currentPlayerChunk);
        }

        ProcessPendingRelease();
    }

    private void InitializeMapData()
    {
        mapDatabase = new MapDatabase();
        mapDatabase.Initialize();

        mapData = mapDatabase.GetMapData(currentMapID);
    }

    private bool ValidateSettings()
    {
        return playerTransform != null
            && chunkPrefab != null
            && mapData != null
            && mapData.chunkSize > 0
            && mainCamera != null;
    }

    private bool ValidateRuntime()
    {
        return chunkWorldSize > 0f;
    }

    private int CalculateRequiredViewRadius()
    {
        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        int radiusX = Mathf.CeilToInt(halfWidth / chunkWorldSize) + 1;
        int radiusY = Mathf.CeilToInt(halfHeight / chunkWorldSize) + 1;

        return Mathf.Max(radiusX, radiusY);
    }

    private Vector2Int CalculateCurrentChunk(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
        int chunkY = Mathf.FloorToInt(worldPosition.y / chunkWorldSize);

        return new Vector2Int(chunkX, chunkY);
    }

    private void MakeCoordinateList(Vector2Int centerChunk, int radius)
    {
        requiredCoords.Clear();

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                requiredCoords.Add(new Vector2Int(
                    centerChunk.x + x,
                    centerChunk.y + y));
            }
        }

    }

    private void UpdateChunks(Vector2Int centerChunk)
    {
        MakeCoordinateList(centerChunk, viewRadius);

        // 생성
        foreach (Vector2Int coord in requiredCoords)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                ActivateChunk(coord);
            }
            else
                pendingRelease.Remove(coord);
        }

        // 제거
        toRemove.Clear();

        foreach (Vector2Int coord in activeChunks.Keys)
        {
            if (!requiredCoords.Contains(coord))
            {
                toRemove.Add(coord);
            }
        }

        foreach (Vector2Int coord in toRemove)
        {
            pendingRelease[coord] = Time.time + releaseDelay;
        }
    }

    private void ProcessPendingRelease()
    {
        removeList.Clear();

        foreach (var pair in pendingRelease)
        {
            if (Time.time >= pair.Value)
            {
                removeList.Add(pair.Key);
            }
        }

        foreach (Vector2Int coord in removeList)
        {
            ReleaseChunk(coord);
            pendingRelease.Remove(coord);
        }
    }

    private void ActivateChunk(Vector2Int chunkCoord)
    {
        MapChunk chunk = chunkPool.GetChunk();
        chunk.transform.position = ChangeCoordinateToWorld(chunkCoord);
        chunk.Initialize(chunkCoord, mapData.chunkSize);
        activeChunks.Add(chunkCoord, chunk);

        // 추가
        if (DroppedItemManager.Instance != null)
            DroppedItemManager.Instance.RespawnDropsInChunk(chunkCoord);
    }

    private void ReleaseChunk(Vector2Int chunkCoord)
    {
        if (activeChunks.TryGetValue(chunkCoord, out MapChunk chunk))
        {
            // 추가
            if (DroppedItemManager.Instance != null)
                DroppedItemManager.Instance.ClearSpawnedDropsInChunk(chunkCoord);

            chunkPool.ReturnChunk(chunk);
            activeChunks.Remove(chunkCoord);
        }
    }

    private Vector3 ChangeCoordinateToWorld(Vector2Int chunkCoord)
    {
        float worldX = chunkCoord.x * chunkWorldSize;
        float worldY = chunkCoord.y * chunkWorldSize;

        return new Vector3(worldX, worldY, 0f);
    }
}
