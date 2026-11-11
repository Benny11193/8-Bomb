[System.Serializable]
public class CellData
{
    public bool isWall = false;
    public bool isBorder = false;
    public bool hasChest = false;
    public bool hasStar = false;
    
    // 0 代表該格子沒有數字球
    public int ballValue = 0; 
    
    // 0 代表沒有隱藏球
    public int hiddenBall = 0; 
    public bool hiddenStar = false;
}