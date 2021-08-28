/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CardGameDef.Unity;
using Cgs.Decks;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;
using Random = System.Random;

namespace Cgs.CardGameView.Multiplayer
{
    public class SaveStack : MonoBehaviour
    {
        private UnityDeck _currentDeck;
        private CardStack _stack;

        private void Start()
        {
            _stack = GetComponent<CardStack>();
        }

        public void SaveToFile()
        {
            _currentDeck = new UnityDeck(CardGameManager.Current, _stack.Name + " " + DateTime.Now,
                CardGameManager.Current.DeckFileType,_stack.Cards);
            SaveToFile(_currentDeck);
        }

        private static void SaveToFile(UnityDeck deck, OnDeckSavedDelegate deckSaveCallback = null)
        {
            try
            {
                if (!Directory.Exists(CardGameManager.Current.DecksDirectoryPath))
                    Directory.CreateDirectory(CardGameManager.Current.DecksDirectoryPath);
                File.WriteAllText(deck.FilePath, deck.ToString());
            }
            catch (Exception e)
            {
                //Debug.LogError(DeckSaveErrorMessage + e.Message);
            }

            deckSaveCallback?.Invoke(deck);
        }
    }
}
