using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Grid Setup")]
    public float cellSize = 0.32f;

    [Header("Padding & Offset")]
    public float paddingCells = 2f;
    public Vector2 cameraOffset = Vector2.zero;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // 延遲一幀執行，確保 GridManager 已經初始化
        Invoke(nameof(AutoFitGrid), 0.1f);
    }

    public void AutoFitGrid()
    {
        if (GridManager.Instance == null) return;

        float gridSize = GridManager.Instance.gridSize;

        // 相機中心點對齊世界原點 (加上手動微調值)
        Vector3 targetPosition = new Vector3(cameraOffset.x, cameraOffset.y, -10f);
        transform.position = targetPosition;

        // 1. 計算包含留白的總格數
        float totalCells = gridSize + paddingCells * 2f;

        // 2. 將格數乘上 cellSize (0.32)，轉換為真實的 Unity 物理世界大小
        float targetWorldSize = totalCells * cellSize; 

        // 3. 計算相機的 Orthographic Size (高度的一半)
        float fitHeightSize = targetWorldSize / 2f;
        
        // 如果要配合螢幕寬度，需除以 Aspect Ratio
        float fitWidthSize = fitHeightSize / cam.aspect;

        // 4. 取兩者最大值，保證畫面絕對裝得下整個棋盤
        cam.orthographicSize = Mathf.Max(fitHeightSize, fitWidthSize);
        
        Debug.Log($"📷 相機適配完畢。棋盤真實寬度: {gridSize * cellSize} 單位, 相機 Size: {cam.orthographicSize}");
    }
}