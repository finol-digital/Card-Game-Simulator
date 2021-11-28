/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Menu
{
    [RequireComponent(typeof(Canvas))]
    public class Modal : MonoBehaviour
    {
        public bool IsFocused => CardGameManager.Instance.ModalCanvas != null &&
                                 CardGameManager.Instance.ModalCanvas.gameObject == gameObject;

        protected virtual List<InputField> InputFields { get; set; } = new List<InputField>();
        protected virtual List<Toggle> Toggles { get; set; } = new List<Toggle>();

        protected static InputField ActiveInputField
        {
            get =>
                EventSystem.current.currentSelectedGameObject != null
                    ? EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()
                    : null;
            private set
            {
                if (EventSystem.current.alreadySelecting)
                    return;
                EventSystem.current.SetSelectedGameObject(value == null ? null : value.gameObject);
            }
        }

        protected static Toggle ActiveToggle
        {
            get => EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>()
                : null;
            private set
            {
                if (EventSystem.current.alreadySelecting)
                    return;
                EventSystem.current.SetSelectedGameObject(value == null ? null : value.gameObject);
            }
        }

        protected void FocusInputField()
        {
            if (ActiveInputField == null || InputFields.Count < 1)
            {
                InputFields.FirstOrDefault()?.ActivateInputField();
                ActiveInputField = InputFields.FirstOrDefault();
                return;
            }

            if (Inputs.IsFocusBack)
            {
                // up
                InputField previous = InputFields.Last();
                foreach (InputField inputField in InputFields)
                {
                    if (ActiveInputField == inputField)
                    {
                        previous.ActivateInputField();
                        ActiveInputField = previous;
                        break;
                    }

                    previous = inputField;
                }
            }
            else if (Inputs.IsFocusNext)
            {
                // down
                InputField next = InputFields.First();
                for (int i = InputFields.Count - 1; i >= 0; i--)
                {
                    if (ActiveInputField == InputFields[i])
                    {
                        next.ActivateInputField();
                        ActiveInputField = next;
                        break;
                    }

                    next = InputFields[i];
                }
            }
        }

        protected void FocusToggle()
        {
            if (ActiveToggle == null || Toggles.Count < 1)
            {
                ActiveToggle = Toggles.FirstOrDefault();
                return;
            }

            if (Inputs.IsVertical)
            {
                if (Inputs.IsUp && !Inputs.WasUp)
                {
                    // up
                    Toggle previous = Toggles.Last();
                    foreach (Toggle toggle in Toggles)
                    {
                        if (ActiveToggle == toggle)
                        {
                            ActiveToggle = previous;
                            break;
                        }

                        if (toggle.transform.parent != ActiveToggle.transform.parent)
                            previous = toggle;
                    }
                }
                else if (Inputs.IsDown && !Inputs.WasDown)
                {
                    // down
                    Toggle next = Toggles.First();
                    for (int i = Toggles.Count - 1; i >= 0; i--)
                    {
                        if (ActiveToggle == Toggles[i])
                        {
                            ActiveToggle = next;
                            break;
                        }

                        if (Toggles[i].transform.parent != ActiveToggle.transform.parent)
                            next = Toggles[i];
                    }
                }
            }
            else if (Inputs.IsHorizontal)
            {
                if (Inputs.IsRight && !Inputs.WasRight)
                {
                    // right
                    Toggle next = Toggles.First();
                    for (int i = Toggles.Count - 1; i >= 0; i--)
                    {
                        if (ActiveToggle == Toggles[i])
                        {
                            ActiveToggle = next;
                            break;
                        }

                        next = Toggles[i];
                    }
                }
                else if (Inputs.IsLeft && !Inputs.WasLeft)
                {
                    // left
                    Toggle previous = Toggles.Last();
                    foreach (Toggle toggle in Toggles)
                    {
                        if (ActiveToggle == toggle)
                        {
                            ActiveToggle = previous;
                            break;
                        }

                        previous = toggle;
                    }
                }
            }
        }

        protected static void ToggleEnum()
        {
            if (ActiveToggle == null)
                return;
            ActiveToggle.isOn = !ActiveToggle.isOn;
        }

        protected virtual void Start()
        {
            CardGameManager.Instance.ModalCanvases.Add(GetComponent<Canvas>());
            InputFields = new List<InputField>(GetComponentsInChildren<InputField>());
            Toggles = new List<Toggle>(GetComponentsInChildren<Toggle>());
            foreach (var canvasScaler in GetComponentsInChildren<CanvasScaler>())
                canvasScaler.referenceResolution = ResolutionManager.Resolution;
        }

        [UsedImplicitly]
        public virtual void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            foreach (var canvasScaler in GetComponentsInChildren<CanvasScaler>())
                canvasScaler.referenceResolution = ResolutionManager.Resolution;
        }

        [UsedImplicitly]
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
