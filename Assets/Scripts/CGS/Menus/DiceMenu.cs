/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

using CGS.Play;

namespace CGS.Menus
{
    public class DiceMenu : MonoBehaviour
    {
        public const int DefaultMin = 1;
        public const int DefaultMax = 6;

        public GameObject diePrefab;
        public Text minText;
        public Text maxText;

        public int Min
        {
            get { return _min; }
            set
            {
                _min = value;
                minText.text = _min.ToString();
            }
        }
        private int _min;

        public int Max
        {
            get { return _max; }
            set
            {
                _max = value;
                maxText.text = _max.ToString();
            }
        }
        private int _max;

        protected RectTransform Target { get; set; }

        void Start()
        {
            Min = DefaultMin;
            Max = DefaultMax;
        }

        void Update()
        {
            if (!Input.anyKeyDown || gameObject != CardGameManager.Instance.TopMenuCanvas?.gameObject)
                return;

            if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
                CreateAndHide();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();
        }

        public void Show(RectTransform playArea)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Target = playArea;
        }

        public void DecrementMin()
        {
            Min--;
        }

        public void IncrementMin()
        {
            Min++;
        }

        public void DecrementMax()
        {
            Max--;
        }

        public void IncrementMax()
        {
            Max++;
        }

        public void CreateAndHide()
        {
            Die die = Instantiate(diePrefab, Target.parent).GetOrAddComponent<Die>();
            die.transform.SetParent(Target);
            die.Min = Min;
            die.Max = Max;
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
