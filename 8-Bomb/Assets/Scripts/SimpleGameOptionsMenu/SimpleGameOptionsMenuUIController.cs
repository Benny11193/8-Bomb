using UnityEngine;
using System.Collections;

public class SimpleGameOptionsMenuUIController : MonoBehaviour
{
	[SerializeField] GameObject Menu;

	public void OpenMenu()
	{
		Menu.SetActive(true);
	}

	public void CloseMenu()
	{
		Menu.SetActive(false);
	}
}

