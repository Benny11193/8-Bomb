using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Map Settings (地圖與資源設定)")]
    [Range(10, 30)] public int gridSize = 15;
    public int normalBallCount = 10;     // 1~7 號球每種的數量
    public int protagonistBallCount = 4; // 8 號球 (主角) 的數量
    [Range(0f, 1f)] public float starSpawnRate = 0.5f; // 空牆壁藏有星星的機率

    [Header("Dynamic Chest Settings (寶箱動態注入設定)")]
    public int wallsToDestroyForChest = 30; // 玩家必須炸毀多少牆壁，寶箱才會生成
    
    // 狀態追蹤
    public CellData[,] grid;
    public Vector2Int playerPos;
    public int score = 0;
    
    public int destroyedWallCount { get; private set; }
    public bool isChestSpawned { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitGame();
    }

    public void InitGame()
    {
        score = 0;
        destroyedWallCount = 0;
        isChestSpawned = false;
        
        grid = new CellData[gridSize, gridSize];
        for (int y = 0; y < gridSize; y++)
            for (int x = 0; x < gridSize; x++)
                grid[x, y] = new CellData();

        // 🌟 隨機決定保底球的延伸方向 (上, 下, 左, 右)
        Vector2Int[] dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int dir = dirs[Random.Range(0, 4)];

        // 1. 決定隨機出生點 (將範圍縮小到 3~gridSize-3，確保延伸的安全區不會挖穿最外圍牆壁)
        int spawnX = Random.Range(3, gridSize - 3);
        int spawnY = Random.Range(3, gridSize - 3);
        playerPos = new Vector2Int(spawnX, spawnY);

        // 定義安全區：主角周圍 3x3，加上往 dir 方向多兩格用來放開局保底球
        HashSet<Vector2Int> safeZone = new HashSet<Vector2Int>();
        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                safeZone.Add(new Vector2Int(spawnX + i, spawnY + j));
        
        // 額外挖空放置第二顆保底球的位子 (距離主角 2 格)
        safeZone.Add(new Vector2Int(spawnX + dir.x * 2, spawnY + dir.y * 2)); 

        // 2. 佈置地形與牆壁
        List<Vector2Int> wallCells = new List<Vector2Int>();
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);

                // 最外圍圈為邊界走道
                if (x == 0 || x == gridSize - 1 || y == 0 || y == gridSize - 1)
                {
                    grid[x, y].isBorder = true;
                    grid[x, y].isWall = false;
                }
                // 安全區挖空
                else if (safeZone.Contains(currentPos)) 
                {
                    grid[x, y].isWall = false;
                }
                // 其他皆為牆壁
                else
                {
                    grid[x, y].isWall = true;
                    wallCells.Add(currentPos);
                }
            }
        }

        // 3. 建立並洗牌資源包
        List<int> bag = new List<int>();
        for (int i = 1; i <= 7; i++)
            for (int j = 0; j < normalBallCount; j++) bag.Add(i);
        
        for (int j = 0; j < protagonistBallCount; j++) bag.Add(8);

        // 🚨 扣除直接放置在場上的實體球，確保總數量「物質守恆」
        bag.Remove(7);
        bag.Remove(5);
        bag.Remove(8);
        bag.Remove(8);

        ShuffleList(bag);
        ShuffleList(wallCells);

        int itemsToHide = Mathf.Min(bag.Count, wallCells.Count);

        // 4. 將物資藏進牆壁中
        for (int i = 0; i < itemsToHide; i++)
        {
            Vector2Int pos = wallCells[i];
            grid[pos.x, pos.y].hiddenBall = bag[i];
        }

        // 5. 處理剩下的空牆壁：藏星星
        if (wallCells.Count > itemsToHide)
        {
            List<Vector2Int> emptyWalls = wallCells.GetRange(itemsToHide, wallCells.Count - itemsToHide);
            foreach (Vector2Int w in emptyWalls)
            {
                if (Random.value < starSpawnRate)
                {
                    grid[w.x, w.y].hiddenStar = true;
                }
            }
        }

        // 6. 🌟 放置初始實體球 (雙主角與四向保底球)
        // 中央主角
        grid[spawnX, spawnY].ballValue = 8;
        // 沿著隨機方向擺放 7 與 5
        grid[spawnX + dir.x, spawnY + dir.y].ballValue = 7;
        grid[spawnX + dir.x * 2, spawnY + dir.y * 2].ballValue = 5;
        
        // 外圍第二主角 (固定在左側走道中央)
        grid[0, gridSize / 2].ballValue = 8;
        
        Debug.Log($"地圖生成！出生點: ({spawnX}, {spawnY})，保底球 7 與 5 位於方向 {dir}。");

        // 7. 通知 VisualManager 繪製地圖
        VisualManager.Instance.RenderBoard();
    }

    /// <summary>
    /// 供爆炸邏輯呼叫。摧毀牆壁並處理寶箱的動態生成。
    /// </summary>
    public void DestroyWall(int x, int y)
    {
        if (!IsValidPos(x, y) || !grid[x, y].isWall) return;

        grid[x, y].isWall = false;
        destroyedWallCount++;

        // 檢查是否達到寶箱生成門檻
        if (!isChestSpawned && destroyedWallCount >= wallsToDestroyForChest)
        {
            InjectChestIntoRandomWall();
        }
    }

    private void InjectChestIntoRandomWall()
    {
        List<Vector2Int> activeWalls = new List<Vector2Int>();
        
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // 必須是牆壁，且「沒有寶箱、沒有隱藏數字球、也沒有星星」
                if (grid[x, y].isWall && 
                    !grid[x, y].hasChest && 
                    grid[x, y].hiddenBall == 0 && 
                    !grid[x, y].hiddenStar)
                {
                    activeWalls.Add(new Vector2Int(x, y));
                }
            }
        }

        // 如果完全空的牆壁都被炸光了，退而求其次找「沒有數字球」的牆壁 (把星星擠掉)
        if (activeWalls.Count == 0)
        {
            for (int y = 0; y < gridSize; y++)
                for (int x = 0; x < gridSize; x++)
                    if (grid[x, y].isWall && !grid[x, y].hasChest && grid[x, y].hiddenBall == 0)
                        activeWalls.Add(new Vector2Int(x, y));
        }

        if (activeWalls.Count > 0)
        {
            Vector2Int chestPos = activeWalls[Random.Range(0, activeWalls.Count)];
            
            grid[chestPos.x, chestPos.y].hasChest = true;
            grid[chestPos.x, chestPos.y].hiddenStar = false; // 強制互斥：確保該格絕對沒有星星

            isChestSpawned = true;
            Debug.Log($"🎊 寶箱已生成於空牆壁 ({chestPos.x}, {chestPos.y})！");
        }
        else
        {
            Debug.LogWarning("場上已經沒有空牆壁可以藏寶箱了！");
        }
    }

    public bool IsValidPos(int x, int y)
    {
        return x >= 0 && x < gridSize && y >= 0 && y < gridSize;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void RestartGame()
    {
        Debug.Log("🔄 遊戲軟重啟中...");

        PlayerController.Instance.ResetState();
        VisualManager.Instance.ClearBoard();
        InitGame(); 
    }
}
