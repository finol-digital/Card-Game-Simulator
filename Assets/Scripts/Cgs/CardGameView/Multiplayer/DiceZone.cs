/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public class DiceZone : CgsNetPlayable
    {
        public const string RollXDicePrompt = "Roll {0} Dice?";

        public IReadOnlyList<Die> DiceInZone => _diceInZone;
        private readonly List<Die> _diceInZone = new();

        public Text sumLabel;

        private Collider2D _collider2D;

        protected override void OnAwakePlayable()
        {
            _collider2D = gameObject.GetOrAddComponent<Collider2D>();
        }

        private void Update()
        {
            _diceInZone.Clear();
            var diceSum = 0;

            List<Collider2D> overlapResults = new(32);
            _collider2D.Overlap(overlapResults);
            foreach (var die in overlapResults
                         .Select(overlapResult => overlapResult.GetComponent<Die>())
                         .Where(die => die != null))
            {
                _diceInZone.Add(die);
                diceSum += die.Value;
            }

            sumLabel.text = diceSum.ToString();
        }

        [UsedImplicitly]
        public void RollDiceInZone()
        {
            CardGameManager.Instance.Messenger.Prompt(string.Format(RollXDicePrompt, DiceInZone.Count),
                () =>
                {
                    foreach (var die in DiceInZone)
                        die.Roll();
                });
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            // Disable preview
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            // Disable preview
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            // Disable select
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            // Disable select
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            // Disable select
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void PostDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void ActOnDrag()
        {
            // Disable drag
        }
    }
}
