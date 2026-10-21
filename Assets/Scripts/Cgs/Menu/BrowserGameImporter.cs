/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.IO;
using Cgs.Localization;
using UnityEngine;
using System.Runtime.InteropServices;
using AOT;

namespace Cgs.Menu
{
    internal static class BrowserGameImporter
    {
        internal const string DownloadHelp = "The browser could not download this game. Its host may block browser access, or the download service may be unavailable. Try again, or download a .cgs.zip file and choose Load from CGS ZIP File.";
        private const string ReadError = "Could not read the selected CGS ZIP file. Please try again.";
        private static string _directory;
        private static bool _captureKeyboard;

        [DllImport("__Internal")]
        private static extern void CgsPickGameZip(string directory, string title, string cancel,
            Action<int, IntPtr> completed);

        public static void Open()
        {
            if (_directory != null)
                return;

            _directory = Path.Combine(Application.temporaryCachePath, "CgsImport-" + Guid.NewGuid().ToString("N"));
            _captureKeyboard = WebGLInput.captureAllKeyboardInput;
            try
            {
                Directory.CreateDirectory(_directory);
                WebGLInput.captureAllKeyboardInput = false;
                CgsPickGameZip(_directory,
                    CgsLocalization.Text(new UiMessage("files.select.zip", GamesManagementMenu.SelectZipFilePrompt)),
                    CgsLocalization.Text(new UiMessage("common.cancel", "Cancel")), Complete);
            }
            catch (Exception exception)
            {
                WebGLInput.captureAllKeyboardInput = _captureKeyboard;
                ReportError(exception.Message);
                Cleanup();
            }
        }

        [MonoPInvokeCallback(typeof(Action<int, IntPtr>))]
        private static void Complete(int status, IntPtr value)
        {
            WebGLInput.captureAllKeyboardInput = _captureKeyboard;
            try
            {
                if (status == 1)
                {
                    var fileName = Marshal.PtrToStringUTF8(value);
                    if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
                        throw new IOException(ReadError);

                    // The importer may wait for an overwrite decision. Keep the ZIP until it finishes.
                    CardGameManager.Instance.ImportCardGame(Path.Combine(_directory, fileName), Cleanup);
                    return;
                }

                if (status == 3)
                {
                    var fileName = Marshal.PtrToStringUTF8(value);
                    CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.zip.invalid",
                        string.Format(CardGameManager.InvalidCgsZipFileErrorMessage, fileName), ("detail", fileName)));
                }
                else if (status != 0)
                    ReportError(ReadError);
            }
            catch (Exception exception)
            {
                ReportError(exception.Message);
            }
            Cleanup();
        }

        private static void ReportError(string detail)
        {
            Debug.LogWarning(CardGameManager.ImportFailureErrorMessage + detail);
            CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.import",
                CardGameManager.ImportFailureErrorMessage + detail, ("detail", detail)));
        }

        private static void Cleanup()
        {
            var directory = _directory;
            _directory = null;
            if (string.IsNullOrEmpty(directory))
                return;

            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(CardGameManager.ImportFailureErrorMessage + exception.Message);
            }
        }
    }
}
#endif
