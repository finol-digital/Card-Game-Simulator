/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
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
                dieActions.SetActive(_selectedPlayable is Die);
                stackActions.SetActive(_selectedPlayable is CardStack);
            }
        }

        private CgsNetPlayable _selectedPlayable;

        private CardStack Stack => SelectedPlayable as CardStack;
        private Die Dice => SelectedPlayable as Die;

        public CanvasGroup preview;
        public CanvasGroup view;
        public List<Text> valueTexts;
        public GameObject dieActions;
        public GameObject stackActions;

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

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(Reset);
        }

        private void Start()
        {
            Reset();
        }

        private void Update()
        {
            WasVisible = IsVisible;
            if (!IsVisible || SelectedPlayable == null || CardGameManager.Instance.ModalCanvas != null)
                return;

            foreach (var valueText in valueTexts)
                valueText.text = SelectedPlayable.ViewValue;

            if (EventSystem.current.currentSelectedGameObject == null && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (SelectedPlayable is Die)
            {
                if (Inputs.IsNew)
                    DecrementDie();
                else if (Inputs.IsLoad || Inputs.IsSubmit)
                    RollDie();
                else if (Inputs.IsSave)
                    IncrementDie();
                else if (Inputs.IsOption)
                    DeleteDie();
            }
            else if (SelectedPlayable is CardStack)
            {
                if (Inputs.IsNew || Inputs.IsSubmit)
                    ViewStack();
                else if (Inputs.IsLoad)
                    ShuffleStack();
                else if (Inputs.IsSave)
                    SaveStack();
                else if (Inputs.IsOption)
                    DeleteStack();
            }

            if (Inputs.IsCancel)
                SelectedPlayable = null;
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

        [UsedImplicitly]
        public void IncrementDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring increment request since there is no die selected.");
                return;
            }

            Dice.Increment();
        }

        [UsedImplicitly]
        public void DecrementDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring decrement request since there is no die selected.");
                return;
            }

            Dice.Decrement();
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
        public void DeleteDie()
        {
            if (Dice == null)
            {
                Debug.LogWarning("Ignoring delete request since there is no die selected.");
                return;
            }

            Dice.PromptDelete();
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
        public void SaveStack()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring save request since there is no stack selected.");
                return;
            }

            Stack.PromptSave();
        }

        [UsedImplicitly]
        public void DeleteStack()
        {
            if (Stack == null)
            {
                Debug.LogWarning("Ignoring save request since there is no stack selected.");
                return;
            }

            Stack.PromptDelete();
        }
    }
}
