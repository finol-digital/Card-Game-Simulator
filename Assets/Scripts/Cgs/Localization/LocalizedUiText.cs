/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Cgs.Localization
{
    public sealed class LocalizedUiText : MonoBehaviour
    {
        private UiMessage _message;
        private LocalizedMessageBinding _binding;
        private UnityEngine.UI.Text _legacy;
        private TMP_Text _tmp;
        private int? _number;

        public static void SetNumber(Component target, int value)
        {
            if (target == null)
                return;
            var presenter = target.GetComponent<LocalizedUiText>();
            if (presenter != null && presenter._number == value)
                return;
            Set(target, UiMessage.With("common.count", value.ToString(), ("count", value)));
            target.GetComponent<LocalizedUiText>()._number = value;
        }

        public static void Set(Component target, UiMessage message)
        {
            if (target == null)
                return;
            DisableAuthoredBinding(target);
            var presenter = target.GetComponent<LocalizedUiText>();
            if (presenter == null)
                presenter = target.gameObject.AddComponent<LocalizedUiText>();
            presenter._legacy = target as UnityEngine.UI.Text;
            presenter._tmp = target as TMP_Text;
            presenter._message = message;
            presenter._number = null;
            presenter.Rebind();
        }

        public static void SetLiteral(Component target, string text)
        {
            if (target == null)
                return;
            DisableAuthoredBinding(target);
            var presenter = target.GetComponent<LocalizedUiText>();
            if (presenter != null)
            {
                presenter._message = null;
                presenter._number = null;
                presenter.Rebind();
            }
            if (target is UnityEngine.UI.Text legacy)
                legacy.text = text;
            else if (target is TMP_Text tmp)
                tmp.text = text;
        }

        private void OnEnable() => Rebind();

        private static void DisableAuthoredBinding(Component target)
        {
            var authored = target.GetComponent<LocalizeStringEvent>();
            if (authored != null)
                authored.enabled = false;
        }
        private void OnDisable()
        {
            _binding?.Dispose();
            _binding = null;
        }

        private void Rebind()
        {
            _binding?.Dispose();
            _binding = null;
            if (_message != null)
            {
                Apply(_message.English);
                if (isActiveAndEnabled)
                    _binding = new LocalizedMessageBinding(_message, Apply);
            }
        }

        private void Apply(string text)
        {
            if (this == null)
                return;
            if (_legacy != null)
            {
                FitButtonLabel(_legacy);
                _legacy.text = text;
            }
            if (_tmp != null)
                _tmp.text = text;
            if (transform is RectTransform rect)
                UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        internal static void FitButtonLabel(UnityEngine.UI.Text text)
        {
            if (text == null || text.resizeTextForBestFit
                || text.GetComponentInParent<UnityEngine.UI.Button>() == null)
                return;
            // Fixed toolbar buttons otherwise truncate longer translations, and
            // some single-word labels disappear entirely when wrapping cannot fit.
            text.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(text.fontSize * 0.55f));
            text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true;
        }
    }
}
