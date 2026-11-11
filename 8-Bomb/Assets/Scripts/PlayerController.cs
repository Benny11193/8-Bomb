using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    public float moveDuration = 0.12f;
    private bool isMoving = false;

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
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMoveEvent += HandleMove;
            InputManager.Instance.OnSwitchEvent += SwitchToNextPlayer;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMoveEvent -= HandleMove;
            InputManager.Instance.OnSwitchEvent -= SwitchToNextPlayer;
        }
    }

    private void HandleMove(Vector2 input)
    {
        if (isMoving) return;

        int dx = Mathf.RoundToInt(input.x);
        int dy = -Mathf.RoundToInt(input.y); 

        if (Mathf.Abs(dx) == Mathf.Abs(dy)) return; 

        TryMoveAndPush(dx, dy);
    }

    private void TryMoveAndPush(int dx, int dy)
    {
        GridManager gridMgr = GridManager.Instance;
        VisualManager visMgr = VisualManager.Instance;

        Vector2Int px = gridMgr.playerPos;
        Vector2Int tx = new(px.x + dx, px.y + dy); 

        if (!gridMgr.IsValidPos(tx.x, tx.y)) return;

        CellData targetCell = gridMgr.grid[tx.x, tx.y];
        if (targetCell.isWall) return;

        // 情況 A：前方是空地，主角單純移動
        if (targetCell.ballValue == 0)
        {
            targetCell.ballValue = 8;
            gridMgr.grid[px.x, px.y].ballValue = 0;
            gridMgr.playerPos = tx;

            isMoving = true;
            StartCoroutine(visMgr.AnimateBallMove(px, tx, moveDuration, () => 
            {
                isMoving = false;
                CheckCollectibles(tx); 
                CheckGameOverState();
            }));
            
            SFXManager.Instance.PlayMove();
            return;
        }

        // 情況 B：前方有球，嘗試推動
        if (targetCell.ballValue > 0)
        {
            Vector2Int nx = new(tx.x + dx, tx.y + dy);
            int pushedBall = targetCell.ballValue;
            SFXManager.Instance.PlayPush();

            // 🚨 B-1: 推到深淵
            if (!gridMgr.IsValidPos(nx.x, nx.y))
            {
                targetCell.ballValue = 8;
                gridMgr.grid[px.x, px.y].ballValue = 0;
                gridMgr.playerPos = tx;

                isMoving = true;

                // 字典清理
                GameObject pushedBallObj = visMgr.GetActiveBall(tx);
                visMgr.activeBalls.Remove(tx); // 移出字典

                StartCoroutine(visMgr.AnimateBallDestroyAtBorder(pushedBallObj, nx, moveDuration));
                StartCoroutine(visMgr.AnimateBallMove(px, tx, moveDuration, () =>
                {
                    isMoving = false;
                    CheckGameOverState();
                }));
                return;
            }

            // 如果 nx 是合法座標，繼續抓取下一步的資料
            CellData nextCell = gridMgr.grid[nx.x, nx.y];

            // 🚨 B-2: 推到空地
            if (nextCell.ballValue == 0 && !nextCell.isWall)
            {
                nextCell.ballValue = pushedBall;
                targetCell.ballValue = 8;
                gridMgr.grid[px.x, px.y].ballValue = 0;
                gridMgr.playerPos = tx;

                isMoving = true;
                StartCoroutine(visMgr.AnimateBallMove(tx, nx, moveDuration)); 
                StartCoroutine(visMgr.AnimateBallMove(px, tx, moveDuration, () => 
                {
                    isMoving = false;
                    CheckCollectibles(nx);
                    CheckGameOverState();
                })); 
                return;
            }

            // ==========================================
            // B-3: 煉金合成與爆炸
            // ==========================================
            if (nextCell.isWall || nextCell.ballValue > 0)
            {
                int combinedVal = -1;
                List<Vector2Int> blastCells = new List<Vector2Int>();
                bool willExplode = false;

                if (nextCell.isWall)
                {
                    if (pushedBall >= 1 && pushedBall <= 8)
                    {
                        combinedVal = (pushedBall == 8) ? 1 : pushedBall; 
                        blastCells = GetExplosionCells(combinedVal, nx.x, nx.y, dx, dy);
                        willExplode = true;
                    }
                    else return; 
                }
                else
                {
                    combinedVal = pushedBall + nextCell.ballValue;
                    if (combinedVal == 16) { nextCell.ballValue = 0; } 
                    else if (combinedVal <= 8) { nextCell.ballValue = combinedVal; } 
                    else 
                    { 
                        blastCells = GetExplosionCells(combinedVal, nx.x, nx.y, dx, dy); 
                        nextCell.ballValue = 0; 
                        willExplode = true;
                    } 
                }

                // --- 音效處理 ---
                if (willExplode)
                {
                    // 根據合成數字決定爆炸大小
                    if (combinedVal >= 12)
                        SFXManager.Instance.PlayUltimateExplosion();
                    else
                        SFXManager.Instance.PlayDirectionalExplosion();
                }
                else
                {
                    // 沒有爆炸，代表只是單純合成或撞毀 (如 16)
                    SFXManager.Instance.PlayCombine();
                }

                // --- 更新資料層 ---
                targetCell.ballValue = 8;
                gridMgr.grid[px.x, px.y].ballValue = 0;
                gridMgr.playerPos = tx;
                isMoving = true;

                // --- 視覺層前置鎖定實體 ---
                GameObject pushedBallObj = visMgr.GetActiveBall(tx); // 鎖死這顆被推出去的炸彈
                GameObject targetBallObj = visMgr.GetActiveBall(nx); // 鎖死原本停在原地的球

                StartCoroutine(visMgr.AnimateBallMove(tx, nx, moveDuration, () =>
                {
                    // 動畫結束，強制銷毀鎖定的實體物件
                    if (nextCell.isWall) 
                    {
                        if (pushedBallObj != null) Destroy(pushedBallObj);
                        visMgr.activeBalls.Remove(nx); 
                    }
                    else if (combinedVal == 16) 
                    {
                        if (pushedBallObj != null) Destroy(pushedBallObj);
                        if (targetBallObj != null) Destroy(targetBallObj);
                        visMgr.activeBalls.Remove(nx);
                    }
                    else if (combinedVal <= 8)
                    {
                        if (pushedBallObj != null) visMgr.UpdateBallVisual(pushedBallObj, combinedVal);
                        if (targetBallObj != null) Destroy(targetBallObj);
                    }
                    else
                    {
                        // 大爆炸：雙雙蒸發
                        if (pushedBallObj != null) Destroy(pushedBallObj);
                        if (targetBallObj != null) Destroy(targetBallObj);
                        visMgr.activeBalls.Remove(nx);
                    }

                    if (willExplode)
                    {
                        ResolveExplosion(blastCells, tx); // 傳入主角的新座標，賦予絕對護盾
                    }
                    else
                    {
                        CheckGameOverState();
                    }
                }));

                StartCoroutine(visMgr.AnimateBallMove(px, tx, moveDuration, () => isMoving = false));
            }
        }
    }

    private void SwitchToNextPlayer()
    {
        if (isMoving) return; // 動畫播放中禁止切換

        GridManager gridMgr = GridManager.Instance;
        List<Vector2Int> allPlayerBalls = new List<Vector2Int>();

        // 掃描全圖，找出所有存活的 8 號球
        for (int y = 0; y < gridMgr.gridSize; y++)
        {
            for (int x = 0; x < gridMgr.gridSize; x++)
            {
                if (gridMgr.grid[x, y].ballValue == 8)
                {
                    allPlayerBalls.Add(new Vector2Int(x, y));
                }
            }
        }

        if (allPlayerBalls.Count <= 1) return; // 只有一顆或死光了，不用切換

        // 找出目前的 Index，並循環切換到下一顆
        int currentIndex = allPlayerBalls.IndexOf(gridMgr.playerPos);
        if (currentIndex == -1) currentIndex = 0; // 防呆

        int nextIndex = (currentIndex + 1) % allPlayerBalls.Count;
        gridMgr.playerPos = allPlayerBalls[nextIndex];

        // 將視覺標記掛到新的球身上
        VisualManager.Instance.AttachPlayerIndicator(gridMgr.playerPos);
        SFXManager.Instance.PlaySwitchPlayer();
        Debug.Log($"🔄 切換主角至座標: {gridMgr.playerPos}");
    }

    private void ResolveExplosion(List<Vector2Int> blastCells, Vector2Int safePos)
    {
        if (blastCells.Count == 0) return;

        GridManager gridMgr = GridManager.Instance;
        VisualManager visMgr = VisualManager.Instance;

        // 控制音效播放頻率，避免同一次爆炸播放過多音效
        bool hasPlayedDestroyBox = false;

        foreach (Vector2Int cell in blastCells)
        {
            if (cell == safePos) continue; // 絕對護盾

            // 播放爆炸特效
            VisualManager.Instance.PlayExplosionVFX(cell);

            CellData c = gridMgr.grid[cell.x, cell.y];
            bool wasWall = c.isWall; 
            
            // 用來保護剛掉出來的星星，不要被這次爆炸瞬間吃掉
            bool droppedStarThisFrame = false; 

            if (wasWall)
            {
                gridMgr.DestroyWall(cell.x, cell.y); 
                visMgr.RemoveWallVisual(cell.x, cell.y); 

                if (!hasPlayedDestroyBox)
                {
                    SFXManager.Instance.PlayDestroyBox();
                    hasPlayedDestroyBox = true;
                }

                if (c.hasChest)
                {
                    visMgr.SpawnChest(cell);
                    SFXManager.Instance.PlayChestAppear();
                }
                else if (c.hiddenBall > 0)
                {
                    c.ballValue = c.hiddenBall;
                    c.hiddenBall = 0;
                    visMgr.SpawnBall(cell, c.ballValue);
                }
                else if (c.hiddenStar)
                {
                    c.hasStar = true;
                    c.hiddenStar = false;
                    visMgr.SpawnStar(cell);
                    droppedStarThisFrame = true;
                }
            }

            // 只有「原本就躺在地上」的星星，才會被爆炸波及收集
            if (c.hasStar && !droppedStarThisFrame)
            {
                c.hasStar = false;
                gridMgr.score++;
                visMgr.DestroyItem(cell);
                VisualManager.Instance.PlayStarCollectVFX(cell);
                SFXManager.Instance.PlayCollectStar();
                Debug.Log($"⭐ 爆炸波及收集！目前分數: {gridMgr.score}");
            }

            // 蒸發範圍內的其他數字球
            if (!wasWall && !c.hasChest && c.ballValue > 0)
            {
                c.ballValue = 0;
                visMgr.DestroyBall(cell);
            }
        }

        CheckGameOverState();
    }

    private List<Vector2Int> GetExplosionCells(int val, int cx, int cy, int dx, int dy)
    {
        // 使用 HashSet 自動過濾重複座標，確保同一個格子絕對不會被重複結算
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        int gridSize = GridManager.Instance.gridSize;

        // 區域函數：檢查邊界後加入 HashSet
        void AddCell(int x, int y)
        {
            if (x > 0 && x < gridSize - 1 && y > 0 && y < gridSize - 1)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        AddCell(cx, cy);

        if (val >= 1 && val <= 8)
        {
            AddCell(cx + dx, cy + dy);
            AddCell(cx + dx * 2, cy + dy * 2);
        }
        else if (val == 9)
        {
            for (int i = 1; i <= 4; i++) AddCell(cx + dx * i, cy + dy * i);
        }
        else if (val == 10)
        {
            for (int i = -3; i <= 3; i++) { AddCell(cx + i, cy); AddCell(cx, cy + i); }
        }
        else if (val == 11)
        {
            for (int i = -2; i <= 2; i++)
            {
                AddCell(cx - dy * 1 + dx * i, cy - dx * 1 + dy * i);
                AddCell(cx + dy * 1 + dx * i, cy + dx * 1 + dy * i);
            }
        }
        else if (val == 12)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    AddCell(cx + dx * i - dy * j, cy + dy * i + dx * j);
                }
            }
        }
        else if (val == 13)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    if (Mathf.Abs(j) == 2 || i == 2 || j == 0)
                    {
                        AddCell(cx + dx * i - dy * j, cy + dy * i + dx * j);
                    }
                }
            }
        }
        else if (val == 14)
        {
            for (int i = -2; i <= 2; i++)
                for (int j = -2; j <= 2; j++)
                    if (Mathf.Abs(i) == 2 || Mathf.Abs(j) == 2) AddCell(cx + i, cy + j);
        }
        else if (val == 15)
        {
            for (int i = -2; i <= 2; i++)
                for (int j = -2; j <= 2; j++)
                    AddCell(cx + i, cy + j);
        }
        
        // 將 HashSet 轉回 List 回傳給結算系統
        return new List<Vector2Int>(cells);
    }

    private void CheckCollectibles(Vector2Int pos)
    {
        GridManager gridMgr = GridManager.Instance;
        CellData cell = gridMgr.grid[pos.x, pos.y];

        if (cell.hasStar)
        {
            cell.hasStar = false;
            gridMgr.score++;
            VisualManager.Instance.DestroyItem(pos);
            VisualManager.Instance.PlayStarCollectVFX(pos);
            SFXManager.Instance.PlayCollectStar();
            Debug.Log($"⭐ 主角收集！目前分數: {gridMgr.score}");
        }

        if (cell.hasChest && !cell.isWall)
        {
            Debug.Log("👑 獲得皇冠，遊戲通關！");
            SimpleGameResultsManager.Instance.ShowResultsScreen("Treasure Found!", true);
        }
    }

    private void CheckGameOverState()
    {
        GridManager gridMgr = GridManager.Instance;
        
        int playerBallCount = 0;
        int usableOtherBalls = 0;

        for (int y = 0; y < gridMgr.gridSize; y++)
        {
            for (int x = 0; x < gridMgr.gridSize; x++)
            {
                CellData c = gridMgr.grid[x, y];
                
                if (c.ballValue == 8) 
                {
                    playerBallCount++;
                }
                // 計算「非 8 號，且不在邊緣」的可用炸彈
                else if (c.ballValue > 0 && !c.isBorder) 
                {
                    usableOtherBalls++;
                }
            }
        }

        if (!gridMgr.isChestSpawned)
        {
            if (playerBallCount == 0)
            {
                Debug.LogWarning("☠️ 所有的 8 號球都被摧毀了！");
                SimpleGameResultsManager.Instance.ShowResultsScreen("Game Over!", false);
            }
            else if (playerBallCount == 1 && usableOtherBalls == 0)
            {
                // 真正的死局：只有 1 顆主角，且其他球全掉在深淵邊緣推不動了！
                Debug.LogWarning("☠️ Game Over！只剩一顆主角球，且沒有可用的炸彈了！");
                SimpleGameResultsManager.Instance.ShowResultsScreen("Game Over!", false);
            }
        }
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isMoving = false;
    }
}