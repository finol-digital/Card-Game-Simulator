/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.Menu;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class DiceMenu : Modal
    {
        private const int DefaultMin = 1;
        private const int DefaultMax = 6;

        public GameObject diePrefab;
        public Text minText;
        public Text maxText;

        private int Min
        {
            get => _min;
            set
            {
                _min = value;
                minText.text = _min.ToString();
            }
        }

        private int _min;

        private int Max
        {
            get => _max;
            set
            {
                _max = value;
                maxText.text = _max.ToString();
            }
        }

        private int _max;

        private RectTransform _target;

        protected override void Start()
        {
            base.Start();
            Min = DefaultMin;
            Max = DefaultMax;

            ClientScene.RegisterSpawnHandler(diePrefab.GetComponent<NetworkIdentity>().assetId, SpawnDie,
                UnSpawnDie);
        }

        private void Update()
        {
            if (!IsFocused)
                return;

            if (Inputs.IsHorizontal)
            {
                if (Inputs.IsLeft && !Inputs.WasLeft)
                    DecrementMin();
                else if (Inputs.IsRight && !Inputs.WasRight)
                    IncrementMin();
            }
            else if (Inputs.IsVertical)
            {
                if (Inputs.IsDown && !Inputs.WasDown)
                    DecrementMax();
                else if (Inputs.IsUp && !Inputs.WasUp)
                    IncrementMax();
            }

            if (Inputs.IsSubmit)
                CreateAndHide();
            else if (Inputs.IsCancel)
                Hide();
        }

        public void Show(RectTransform playArea)
        {
            Show();
            _target = playArea;
        }

        [UsedImplicitly]
        public void DecrementMin()
        {
            Min--;
        }

        [UsedImplicitly]
        public void IncrementMin()
        {
            Min++;
        }

        [UsedImplicitly]
        public void DecrementMax()
        {
            Max--;
        }

        [UsedImplicitly]
        public void IncrementMax()
        {
            Max++;
        }

        [UsedImplicitly]
        public void CreateAndHide()
        {
            Die die = CreateDie();
            die.Min = Min;
            die.Max = Max;
            if (NetworkManager.singleton.isNetworkActive)
                NetworkServer.Spawn(die.gameObject);
            Hide();
        }

        private Die CreateDie()
        {
            Transform parent = _target != null ? _target.parent : null;
            var die = Instantiate(diePrefab, parent).GetOrAddComponent<Die>();
            die.transform.SetParent(_target);
            return die;
        }

        private GameObject SpawnDie(Vector3 position, Guid assetId)
        {
            Die die = CreateDie();
            return die.gameObject;
        }

        private static void UnSpawnDie(GameObject spawned)
        {
            Destroy(spawned);
        }
    }
}
