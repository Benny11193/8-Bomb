using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VisualManager : MonoBehaviour
{
    public static VisualManager Instance { get; private set; }

    [Header("Background Setup")]
    public Tilemap bgTilemap;
    public TileBase bgTile;
    public int bgOverscan = 25;

    [Header("Tilemap Setup")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap;
    public TileBase groundTile;
    public TileBase wallTile;

    [Header("Dynamic Prefabs")]
    public GameObject ballPrefab;
    public GameObject chestPrefab;
    public GameObject starPrefab;

    [Header("Player Indicator")]
    public GameObject playerIndicatorPrefab;
    private GameObject spawnedIndicator;

    [Header("VFX Prefabs")]
    public GameObject explosionVFXPrefab;
    public GameObject starCollectVFXPrefab;

    [Header("Settings")]
    public BallColorSettings colorSettings;

    [Header("Containers")]
    public Transform ballContainer;
    public Transform itemContainer;

    // 儲存動態物件的字典
    public Dictionary<Vector2Int, GameObject> activeBalls = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, GameObject> activeItems = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 將二維陣列邏輯座標 (x, y) 轉換為 Tilemap 的座標 (Vector3Int)
    // 這裡我們直接將地圖置中處理 (讓 (0,0) 位於畫面中心)
    public Vector3Int GetTilePosition(int x, int y)
    {
        int gridSize = GridManager.Instance.gridSize;
        int offsetX = -gridSize / 2;
        int offsetY = gridSize / 2;

        // Unity Tilemap Y 軸朝上，二維陣列 Y 軸朝下，所以這裡 y 取負值
        return new Vector3Int(x + offsetX, -y + offsetY, 0);
    }

    // 取得格子在世界空間的絕對置中座標 (用來生成球、寶箱等物件)
    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector3Int tilePos = GetTilePosition(x, y);
        // 直接利用 Tilemap 的 API 取得格子正中心的 World Position
        return groundTilemap.GetCellCenterWorld(tilePos); 
    }

    public void RenderBoard()
    {
        int gridSize = GridManager.Instance.gridSize;
        CellData[,] grid = GridManager.Instance.grid;

        // 清空原本的 Tilemap
        bgTilemap.ClearAllTiles();
        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        for (int y = -bgOverscan; y < gridSize + bgOverscan; y++)
        {
            for (int x = -bgOverscan; x < gridSize + bgOverscan; x++)
            {
                Vector3Int tilePos = GetTilePosition(x, y);
                bgTilemap.SetTile(tilePos, bgTile);
            }
        }

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                CellData cell = grid[x, y];
                Vector2Int logicPos = new Vector2Int(x, y);
                Vector3Int tilePos = GetTilePosition(x, y);

                // 1. 鋪設底板
                groundTilemap.SetTile(tilePos, groundTile);

                // 2. 鋪設牆壁
                if (cell.isWall)
                {
                    wallTilemap.SetTile(tilePos, wallTile);
                }

                // 3. 生成數字球
                if (cell.ballValue > 0)
                {
                    SpawnBall(logicPos, cell.ballValue);
                }

                // 4. 生成外露的寶箱與星星
                if (cell.hasChest && !cell.isWall)
                {
                    SpawnChest(logicPos);
                }
                if (cell.hasStar && !cell.isWall)
                {
                    SpawnStar(logicPos);
                }
            }
        }

        // 附加玩家指示器
        AttachPlayerIndicator(GridManager.Instance.playerPos);
    }

    public void ClearBoard()
    {
        // 1. 強制停止所有進行中的移動動畫，避免跨局報錯
        StopAllCoroutines(); 
        
        // 2. 銷毀所有數字球與道具實體
        foreach (var ball in activeBalls.Values) 
            if (ball != null) Destroy(ball);
            
        foreach (var item in activeItems.Values) 
            if (item != null) Destroy(item);
            
        activeBalls.Clear();
        activeItems.Clear();
        
        // 3. 清空 Tilemap
        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        
        // 4. 重置玩家指示器
        if (spawnedIndicator != null)
        {
            spawnedIndicator.transform.SetParent(null);
            spawnedIndicator.SetActive(false);
        }
    }

    // 動態移除牆壁的 API
    public void RemoveWallVisual(int x, int y)
    {
        Vector3Int tilePos = GetTilePosition(x, y);
        wallTilemap.SetTile(tilePos, null);
    }

    public void SpawnBall(Vector2Int pos, int value)
    {
        Vector3 worldPos = GetWorldPosition(pos.x, pos.y);
        GameObject ball = Instantiate(ballPrefab, worldPos, Quaternion.identity, ballContainer);
        
        UpdateBallVisual(ball, value);
        activeBalls.Add(pos, ball);
    }

    public void SpawnChest(Vector2Int pos)
    {
        Vector3 worldPos = GetWorldPosition(pos.x, pos.y);
        GameObject chest = Instantiate(chestPrefab, worldPos, Quaternion.identity, itemContainer);
        activeItems.Add(pos, chest);
    }

    public void SpawnStar(Vector2Int pos)
    {
        Vector3 worldPos = GetWorldPosition(pos.x, pos.y);
        GameObject star = Instantiate(starPrefab, worldPos, Quaternion.identity, itemContainer);
        activeItems.Add(pos, star);
    }

    public void UpdateBallVisual(GameObject ball, int value)
    {
        BallVisual ballVisual = ball.GetComponent<BallVisual>();

        if (value >= 1 && value <= 8)
        {
            var mapping = colorSettings.GetMapping(value);
            ballVisual.SetColor(mapping.color);
            ballVisual.SetText(value.ToString());
        }
        else
        {
            ballVisual.SetColor(colorSettings.defaultColor);
            ballVisual.SetText(value.ToString());
        }
    }

    // 平滑移動指定的球
    public IEnumerator AnimateBallMove(Vector2Int from, Vector2Int to, float duration, System.Action onComplete = null)
    {
        if (activeBalls.TryGetValue(from, out GameObject ball))
        {
            // 在動畫開始前更新字典座標
            activeBalls.Remove(from);
            activeBalls[to] = ball;

            Vector3 startPos = ball.transform.position;
            Vector3 endPos = GetWorldPosition(to.x, to.y);
            float elapsed = 0;

            while (elapsed < duration)
            {
                if (ball == null) yield break;
                ball.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (ball != null) ball.transform.position = endPos;
        }
        onComplete?.Invoke();
    }

    // 將球推向邊界外並銷毀
    public IEnumerator AnimateBallDestroyAtBorder(GameObject ball, Vector2Int outOfBoundsPos, float duration)
    {
        if (ball != null)
        {
            Vector3 startPos = ball.transform.position;
            Vector3 endPos = GetWorldPosition(outOfBoundsPos.x, outOfBoundsPos.y); 
            float elapsed = 0;

            while (elapsed < duration)
            {
                if (ball == null) yield break; // 防呆
                ball.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Destroy(ball); // 滑出深淵後銷毀
        }
    }

    // 銷毀畫面上的數字球
    public void DestroyBall(Vector2Int pos)
    {
        if (activeBalls.TryGetValue(pos, out GameObject ball))
        {
            if (spawnedIndicator != null && spawnedIndicator.transform.parent == ball.transform)
            {
                spawnedIndicator.transform.SetParent(null);
                spawnedIndicator.SetActive(false);
            }
            
            Destroy(ball);
            activeBalls.Remove(pos);
        }
    }

    // 銷毀畫面上的寶箱或星星
    public void DestroyItem(Vector2Int pos)
    {
        if (activeItems.TryGetValue(pos, out GameObject item))
        {
            Destroy(item);
            activeItems.Remove(pos);
        }
    }

    // 取得特定的數字球物件
    public GameObject GetActiveBall(Vector2Int pos)
    {
        if (activeBalls.TryGetValue(pos, out GameObject ball)) return ball;
        return null;
    }

    // 將玩家指示器附加到指定位置的數字球上方
    public void AttachPlayerIndicator(Vector2Int pos)
    {
        if (spawnedIndicator == null && playerIndicatorPrefab != null)
        {
            spawnedIndicator = Instantiate(playerIndicatorPrefab);
        }

        GameObject activeBall = GetActiveBall(pos);
        if (activeBall != null && spawnedIndicator != null)
        {
            spawnedIndicator.SetActive(true);
            spawnedIndicator.transform.SetParent(activeBall.transform);
            spawnedIndicator.transform.localPosition = new Vector3(0, 0.8f, 0);
        }
    }

    // --- 特效播放方法 ---
    public void PlayExplosionVFX(Vector2Int pos)
    {
        if (explosionVFXPrefab != null)
        {
            Vector3 worldPos = GetWorldPosition(pos.x, pos.y);
            Instantiate(explosionVFXPrefab, worldPos, Quaternion.identity);
        }
    }

    public void PlayStarCollectVFX(Vector2Int pos)
    {
        if (starCollectVFXPrefab != null)
        {
            Vector3 worldPos = GetWorldPosition(pos.x, pos.y);
            Instantiate(starCollectVFXPrefab, worldPos, Quaternion.identity);
        }
    }
}