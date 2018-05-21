using UnityEngine;
using UnityEngine.EventSystems;

public class DieSelectable : MonoBehaviour, IPointerDownHandler
{
	public GameObject dieObject;

	public void OnPointerDown(PointerEventData eventData)
	{
		EventSystem.current.SetSelectedGameObject(dieObject, eventData);
	}
}
