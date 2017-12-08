using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
