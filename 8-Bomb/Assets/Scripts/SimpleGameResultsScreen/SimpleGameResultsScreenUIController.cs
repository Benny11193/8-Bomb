using UnityEngine;
using TMPro;

public class SimpleGameResultsScreenUIController : MonoBehaviour
{
    [SerializeField] GameObject resultsScreen;
    [SerializeField] TextMeshProUGUI msgText;

    public void ShowResultsScreen(string msg)
	{
		resultsScreen.SetActive(true);
		msgText.text = msg;
	}

    public void CloseResultsScreen()
    {
        resultsScreen.SetActive(false);
    }
}
