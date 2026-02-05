/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Viewer
{
    public class PlayableViewer : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        public static PlayableViewer Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                var playableViewerGameObject = GameObject.FindWithTag(Tags.PlayableViewer);
                if (playableViewerGameObject != null)
                    _instance = playableViewerGameObject.GetOrAddComponent<PlayableViewer>();
                return _instance;
            }
        }

        private static PlayableViewer _instance;

        public CgsNetPlayable SelectedPlayable
        {
            get => _selectedPlayable;
            set
            {
                if (_selectedPlayable != null)
                    _selectedPlayable.HighlightMode = HighlightMode.Off;

                _selectedPlayable = value;

                if (_selectedPlayable == null && !EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);

                IsVisible = _selectedPlayable != null;
            }
        }

        private CgsNetPlayable _selectedPlayable;

        private Die Dice => SelectedPlayable as Die;
        private CardStack Stack => SelectedPlayable as CardStack;
        private Counter SelectedCounter => SelectedPlayable as Counter;

        public CanvasGroup preview;
        public CanvasGroup view;
        public CanvasGroup dieActionPanel;
        public CanvasGroup stackActionPanel;

        [FormerlySerializedAs("tokenActionPanel")]
        public CanvasGroup counterActionPanel;

        public List<Text> valueTexts;
        public InputField dieValueInputField;
        public InputField dieMaxInputField;
        public Dropdown dieDropdown;
        public InputField counterValueInputField;

        [FormerlySerializedAs("tokenDropdown")]
        public Dropdown counterDropdown;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (SelectedPlayable != null)
                    SelectedPlayable.HighlightMode = _isVisible ? HighlightMode.Selected : HighlightMode.Off;
                Redisplay();
            }
        }

        private bool _isVisible;
        public bool WasVisible { get; private set; }

        private bool IsBlocked => !IsVisible || SelectedPlayable == null
                                             || CardGameManager.Instance.ModalCanvas != null
                                             || dieValueInputField.isFocused || dieMaxInputField.isFocused
                                             || EventSystem.current.currentSelectedGameObject ==
                                             dieValueInputField.gameObject
                                             || EventSystem.current.currentSelectedGameObject ==
                                             dieMaxInputField.gameObject
                                             || EventSystem.current.currentSelectedGameObject ==
                                             counterValueInputField.gameObject || counterValueInputField.isFocused;

        private InputAction _pageAction;

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(Reset);

            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayGameSub).performed += InputSub;
            InputSystem.actions.FindAction(Tags.PlayGameAdd).performed += InputAdd;
            InputSystem.actions.FindAction(Tags.ViewerLess).performed += InputLess;
            InputSystem.actions.FindAction(Tags.ViewerMore).performed += InputMore;
            InputSystem.actions.FindAction(Tags.DecksSave).performed += InputSave;
            InputSystem.actions.FindAction(Tags.CardRotate).performed += InputRotate;
            InputSystem.actions.FindAction(Tags.CardFlip).performed += InputFlip;
            InputSystem.actions.FindAction(Tags.DecksNew).performed += InputShuffle;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed += InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private void Start()
        {
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            Reset();
        }

        private void Update()
        {
            WasVisible = IsVisible;

            if (_selectedPlayable == null)
            {
                IsVisible = false;
                return;
            }

            if (IsBlocked)
                return;

            foreach (var valueText in valueTexts)
                valueText.text = SelectedPlayable.ViewValue;

            if (EventSystem.current.currentSelectedGameObject == null && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);
            else
                switch (SelectedPlayable)
                {
                    case Die when IsVisible:
                        RedisplayDie();
                        break;
                    case Counter when IsVisible:
                        RedisplayCounter();
                        break;
                }

            var pageVector = _pageAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if (pageVector == Vector2.zero)
                return;
            var delta = PlayController.PlayableMoveSpeed * Time.deltaTime;
            _selectedPlayable.Position += pageVector * delta;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            IsVisible = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            IsVisible = false;
        }

        public void Reset()
        {
            IsVisible = false;
        }

        public void Redisplay()
        {
            HidePreview();

            view.alpha = IsVisible ? 1 : 0;
            view.interactable = IsVisible;
            view.blocksRaycasts = IsVisible;

            dieActionPanel.alpha = PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is Die ? 1 : 0;
            dieActionPanel.interactable = PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is Die;
            dieActionPanel.blocksRaycasts = PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is Die;

            stackActionPanel.alpha =
                PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is CardStack ? 1 : 0;
            stackActionPanel.interactable = PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is CardStack;
            stackActionPanel.blocksRaycasts =
                PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is CardStack;

            counterActionPanel.alpha =
                PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is Counter ? 1 : 0;
            counterActionPanel.interactable = PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is Counter;
            counterActionPanel.blocksRaycasts =
                PlaySettings.ShowActionsMenu && IsVisible && _selectedPlayable is Counter;

            if (Dice != null)
                RedisplayDie();
            else if (SelectedCounter != null)
                RedisplayCounter();
        }

        private void RedisplayDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Attempted to redisplay die without Dice!");
                return;
            }

            dieValueInputField.text = Dice.Value.ToString();
            dieMaxInputField.text = Dice.Max.ToString();
            if (Mathf.Approximately(Dice.DieColor.r, 1) && Mathf.Approximately(Dice.DieColor.g, 1) &&
                Mathf.Approximately(Dice.DieColor.b, 1) && dieDropdown.value != 0)
                dieDropdown.value = 0;
            else if (Mathf.Approximately(Dice.DieColor.r, 1) && Mathf.Approximately(Dice.DieColor.g, 0) &&
                     Mathf.Approximately(Dice.DieColor.b, 0) && dieDropdown.value != 1)
                dieDropdown.value = 1;
            else if (Mathf.Approximately(Dice.DieColor.r, 0) && Mathf.Approximately(Dice.DieColor.g, 1) &&
                     Mathf.Approximately(Dice.DieColor.b, 0) && dieDropdown.value != 2)
                dieDropdown.value = 2;
            else if (Mathf.Approximately(Dice.DieColor.r, 0) && Mathf.Approximately(Dice.DieColor.g, 0) &&
                     Mathf.Approximately(Dice.DieColor.b, 1) && dieDropdown.value != 3)
                dieDropdown.value = 3;
            else if (Dice.DieColor is { r: < 1, g: < 1, b: < 1 } && dieDropdown.value != 4)
                dieDropdown.value = 4;
        }

        private void RedisplayCounter()
        {
            if (SelectedCounter == null)
            {
                Debug.LogWarning("Attempted to redisplay counter without SelectedCounter!");
                return;
            }

            counterValueInputField.text = SelectedCounter.Value.ToString();
            if (Mathf.Approximately(SelectedCounter.CounterColor.r, 1) &&
                Mathf.Approximately(SelectedCounter.CounterColor.g, 1) &&
                Mathf.Approximately(SelectedCounter.CounterColor.b, 1) && counterDropdown.value != 0)
                counterDropdown.value = 0;
            else if (Mathf.Approximately(SelectedCounter.CounterColor.r, 1) &&
                     Mathf.Approximately(SelectedCounter.CounterColor.g, 0) &&
                     Mathf.Approximately(SelectedCounter.CounterColor.b, 0) && counterDropdown.value != 1)
                counterDropdown.value = 1;
            else if (Mathf.Approximately(SelectedCounter.CounterColor.r, 0) &&
                     Mathf.Approximately(SelectedCounter.CounterColor.g, 1) &&
                     Mathf.Approximately(SelectedCounter.CounterColor.b, 0) && counterDropdown.value != 2)
                counterDropdown.value = 2;
            else if (Mathf.Approximately(SelectedCounter.CounterColor.r, 0) &&
                     Mathf.Approximately(SelectedCounter.CounterColor.g, 0) &&
                     Mathf.Approximately(SelectedCounter.CounterColor.b, 1) && counterDropdown.value != 3)
                counterDropdown.value = 3;
            else if (SelectedCounter.CounterColor is { r: < 1, g: < 1, b: < 1 } && counterDropdown.value != 4)
                counterDropdown.value = 4;
        }

        public void Preview(CgsNetPlayable playable)
        {
            preview.alpha = 1;

            foreach (var valueText in valueTexts)
                valueText.text = playable.ViewValue;
        }

        public void HidePreview()
        {
            preview.alpha = 0;
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            switch (SelectedPlayable)
            {
                case Die:
                    RollDie();
                    break;
                case CardStack:
                    ViewStack();
                    break;
            }
        }

        private void InputSub(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            switch (SelectedPlayable)
            {
                case Die:
                    DecrementDie();
                    break;
                case Counter:
                    DecrementCounter();
                    break;
            }
        }

        [UsedImplicitly]
        public void DecrementDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring decrement request since there is no die selected.");
                return;
            }

            Dice.Value -= 1;
        }

        [UsedImplicitly]
        public void DecrementCounter()
        {
            if (SelectedCounter == null)
            {
                Debug.LogWarning("Ignoring decrement request since there is no counter selected.");
                return;
            }

            SelectedCounter.Value -= 1;
        }

        private void InputLess(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedPlayable is Die)
                DecrementDieMax();
        }

        [UsedImplicitly]
        public void DecrementDieMax()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring decrement max request since there is no die selected.");
                return;
            }

            Dice.Max -= 1;
        }

        private void InputAdd(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            switch (SelectedPlayable)
            {
                case Die:
                    IncrementDie();
                    break;
                case Counter:
                    IncrementCounter();
                    break;
            }
        }

        [UsedImplicitly]
        public void IncrementDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring increment request since there is no die selected.");
                return;
            }

            Dice.Value += 1;
        }

        [UsedImplicitly]
        public void IncrementCounter()
        {
            if (SelectedCounter == null)
            {
                Debug.LogWarning("Ignoring increment request since there is no counter selected.");
                return;
            }

            SelectedCounter.Value += 1;
        }

        private void InputMore(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedPlayable is Die)
                IncrementDieMax();
        }

        [UsedImplicitly]
        public void IncrementDieMax()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring increment max request since there is no die selected.");
                return;
            }

            Dice.Max += 1;
        }

        [UsedImplicitly]
        public void RollDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring roll request since there is no die selected.");
                return;
            }

            Dice.Roll();
        }

        [UsedImplicitly]
        public void ChangeDieValue(string value)
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring change value request since there is no die selected.");
                return;
            }

            if (int.TryParse(value, out var valueInt) && valueInt != Dice.Value)
                Dice.Value = valueInt;
        }

        [UsedImplicitly]
        public void ChangeCounterValue(string value)
        {
            if (SelectedCounter == null)
            {
                Debug.LogWarning("Ignoring change value request since there is no counter selected.");
                return;
            }

            if (int.TryParse(value, out var valueInt) && valueInt != SelectedCounter.Value)
                SelectedCounter.Value = valueInt;
        }

        [UsedImplicitly]
        public void ChangeDieMax(string max)
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring change max request since there is no die selected.");
                return;
            }

            if (int.TryParse(max, out var maxInt) && maxInt != Dice.Max)
                Dice.Max = maxInt;
        }

        [UsedImplicitly]
        public void ChangeDieColor(int option)
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring change color since there is no die selected.");
                return;
            }

            Dice.DieColor = option switch
            {
                0 => Color.white,
                1 => Color.red,
                2 => Color.green,
                3 => Color.blue,
                4 => Color.gray,
                _ => Color.white
            };
        }

        [UsedImplicitly]
        public void ViewStack()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring view request since there is no stack selected.");
                return;
            }

            Stack.View();
        }

        private void InputSave(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedPlayable is CardStack)
                SaveStack();
        }

        [UsedImplicitly]
        public void SaveStack()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring save request since there is no stack selected.");
                return;
            }

            Stack.PromptSave();
        }

        private void InputRotate(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedPlayable is CardStack)
                RotateStack();
        }

        [UsedImplicitly]
        public void RotateStack()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring rotate stack request since there is no stack selected.");
                return;
            }

            Stack.Rotation *= Quaternion.Euler(0, 0, -CardGameManager.Current.GameCardRotationDegrees);
        }

        private void InputFlip(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedPlayable is CardStack)
                FlipStackTopFace();
        }

        [UsedImplicitly]
        public void FlipStackTopFace()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring flip top face request since there is no stack selected.");
                return;
            }

            Stack.FlipTopFace();
        }

        private void InputShuffle(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (SelectedPlayable is CardStack)
                ShuffleStack();
        }

        [UsedImplicitly]
        public void ShuffleStack()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring shuffle request since there is no stack selected.");
                return;
            }

            Stack.PromptShuffle();
        }

        [UsedImplicitly]
        public void ChangeCounterColor(int option)
        {
            if (SelectedCounter == null)
            {
                Debug.LogWarning("Ignoring change color since there is no counter selected.");
                return;
            }

            SelectedCounter.CounterColor = option switch
            {
                0 => Color.white,
                1 => Color.red,
                2 => Color.green,
                3 => Color.blue,
                4 => Color.gray,
                _ => Color.white
            };
        }

        private void InputDelete(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            DeletePlayable();
        }

        [UsedImplicitly]
        public void DeletePlayable()
        {
            if (SelectedPlayable == null)
            {
                Debug.LogWarning("Ignoring delete request since there is no playable selected.");
                return;
            }

            SelectedPlayable.PromptDelete();
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            SelectedPlayable = null;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayGameSub).performed -= InputSub;
            InputSystem.actions.FindAction(Tags.PlayGameAdd).performed -= InputAdd;
            InputSystem.actions.FindAction(Tags.ViewerLess).performed -= InputLess;
            InputSystem.actions.FindAction(Tags.ViewerMore).performed -= InputMore;
            InputSystem.actions.FindAction(Tags.DecksSave).performed -= InputSave;
            InputSystem.actions.FindAction(Tags.CardRotate).performed -= InputRotate;
            InputSystem.actions.FindAction(Tags.CardFlip).performed -= InputFlip;
            InputSystem.actions.FindAction(Tags.DecksNew).performed -= InputShuffle;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed -= InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
