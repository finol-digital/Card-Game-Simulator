using UnityEngine;
using UnityEngine.EventSystems;

// HACK: THIS CLASS EXISTS TO DEAL WITH WONKINESS WITH SELECTING DIE BUTTONS
public class DieSelectable : MonoBehaviour, IPointerDownHandler
{
	public GameObject dieObject;

	public void OnPointerDown(PointerEventData eventData)
	{
		EventSystem.current.SetSelectedGameObject(dieObject, eventData);
	}
}
