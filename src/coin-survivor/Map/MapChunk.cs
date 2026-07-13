using UnityEngine;
using UnityEngine.Tilemaps;

public class MapChunk : MonoBehaviour
{
    private Vector2Int chunkCoord;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap decorationTilemap;

    [Header("Ground Tiles - 반드시 바닥용 타일만 넣기")]
    [SerializeField] private TileBase baseGroundTile;
    [SerializeField] private TileBase[] groundVariationTiles;

    [Header("Decoration Tiles - 꽃, 풀 장식만 넣기")]
    [SerializeField] private TileBase[] decorationTiles;

    [Header("Random Setting")]
    [SerializeField] private int seed = 0;
    [SerializeField, Range(0, 100)] private int groundVariationChance = 12;
    [SerializeField, Range(0, 100)] private int decorationChance = 5;

    private int chunkSize;

    // 필드- GC용
    private Vector3Int[] posBuffer;
    private TileBase[] groundBuffer;
    private TileBase[] decoBuffer;

    private void Awake()
    {
        if (groundTilemap == null)
            groundTilemap = GetComponentInChildren<Tilemap>();
    }

    public void Initialize(Vector2Int coord, int size)
    {
        if (groundTilemap == null)
        {
            Debug.LogWarning("MapChunk: groundTilemap이 없습니다.");
            return;
        }

        if (baseGroundTile == null)
        {
            Debug.LogWarning("MapChunk: baseGroundTile이 없습니다.");
            return;
        }

        chunkCoord = coord;
        chunkSize = size;
#if UNITY_EDITOR
        gameObject.name = $"Chunk ({coord.x}, {coord.y})";
#endif

        groundTilemap.ClearAllTiles();

        if (decorationTilemap != null)
            decorationTilemap.ClearAllTiles();

        int total = chunkSize * chunkSize;
        if (posBuffer == null || posBuffer.Length != total)
        {
            posBuffer = new Vector3Int[total];
            groundBuffer = new TileBase[total];
            decoBuffer = new TileBase[total];
        }

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = coord.x * chunkSize + x;
                int worldY = coord.y * chunkSize + y;

                posBuffer[y * chunkSize + x] = new Vector3Int(x, y, 0);

                groundBuffer[y * chunkSize + x] = GetGroundTile(worldX, worldY);


                decoBuffer[y * chunkSize + x] = GetDecorationTile(worldX, worldY);

            }
        }
        groundTilemap.SetTiles(posBuffer, groundBuffer);
        if (decorationTilemap != null)
            decorationTilemap.SetTiles(posBuffer, decoBuffer);
    }

    private TileBase GetGroundTile(int worldX, int worldY)
    {
        if (groundVariationTiles == null || groundVariationTiles.Length == 0)
            return baseGroundTile;

        int value = HashToPercent(worldX, worldY, 11);

        if (value >= groundVariationChance)
            return baseGroundTile;

        int index = HashToIndex(worldX, worldY, 23, groundVariationTiles.Length);

        if (groundVariationTiles[index] == null)
            return baseGroundTile;

        return groundVariationTiles[index];
    }

    private TileBase GetDecorationTile(int worldX, int worldY)
    {
        if (decorationTiles == null || decorationTiles.Length == 0)
            return null;

        int value = HashToPercent(worldX, worldY, 37);

        if (value >= decorationChance)
            return null;

        int index = HashToIndex(worldX, worldY, 41, decorationTiles.Length);

        return decorationTiles[index];
    }

    private int HashToPercent(int x, int y, int salt)
    {
        int hash = x * 73856093
                 ^ y * 19349663
                 ^ seed * 83492791
                 ^ salt * 265443576;

        return Mathf.Abs(hash) % 100;
    }

    private int HashToIndex(int x, int y, int salt, int length)
    {
        int hash = x * 73856093
                 ^ y * 19349663
                 ^ seed * 83492791
                 ^ salt * 265443576;

        return Mathf.Abs(hash) % length;
    }

    public void ResetChunk()
    {
        if (groundTilemap != null)
            groundTilemap.ClearAllTiles();

        if (decorationTilemap != null)
            decorationTilemap.ClearAllTiles();
    }
}
