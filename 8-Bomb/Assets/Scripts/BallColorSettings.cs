using UnityEngine;

[CreateAssetMenu(fileName = "BallColorSettings", menuName = "ChromaBomb/BallColorSettings")]
public class BallColorSettings : ScriptableObject
{
    [System.Serializable]
    public class ColorMapping
    {
        public int ballValue;
        public Color color;
    }

    public ColorMapping[] mappings;
    public Color defaultColor = Color.gray; // 啞彈或未知錯誤的顏色

    public ColorMapping GetMapping(int value)
    {
        foreach (var mapping in mappings)
        {
            if (mapping.ballValue == value) return mapping;
        }
        return new ColorMapping { color = defaultColor };
    }
}