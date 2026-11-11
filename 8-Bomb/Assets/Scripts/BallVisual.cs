using UnityEngine;
using TMPro;

public class BallVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text textcomponent;

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }

    public void SetText(string text)
    {
        textcomponent.text = text;
    }
}
