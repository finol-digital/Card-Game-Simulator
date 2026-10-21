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
    internal static class BrowserFileBrowser
    {
        internal const string DownloadHelp = "The browser could not download this game. Its host may block browser access, or the download service may be unavailable. Try again, or download a .cgs.zip file and choose Load from CGS ZIP File.";
        private const string ReadError = "Could not read the selected files. Please try again.";
        private static string _directory;
        private static bool _captureKeyboard;
        private static Action<string, Action> _selected;
        private static bool _zipOnly;

        [DllImport("__Internal")]
        private static extern void CgsPickFiles(string directory, string title, string cancel,
            string accept, int folder, int zipOnly, Action<int, IntPtr> completed);

        [DllImport("__Internal")]
        private static extern int CgsDownloadFile(string path, string fileName);

        public static void OpenGameZip()
        {
            Open(new UiMessage("files.select.zip", GamesManagementMenu.SelectZipFilePrompt),
                ".cgs.zip,.zip,application/zip,application/x-zip-compressed", false, true,
                (path, release) => CardGameManager.Instance.ImportCardGame(path, release));
        }

        public static void OpenImage(UiMessage title, Action<string> selected)
        {
            Open(title, "image/*", false, false, (path, release) =>
            {
                try
                {
                    // Image handlers copy the file into their preview cache before returning.
                    selected(path);
                }
                finally
                {
                    release();
                }
            });
        }

        public static void OpenFolder(UiMessage title, Action<string, Action> selected)
        {
            Open(title, string.Empty, true, false, selected);
        }

        public static void Download(string path)
        {
            if (CgsDownloadFile(path, Path.GetFileName(path)) != 1)
                throw new IOException("Could not start the browser download. Please try again.");
        }

        private static void Open(UiMessage title, string accept, bool folder, bool zipOnly,
            Action<string, Action> selected)
        {
            if (_directory != null)
                return;

            _directory = Path.Combine(Application.temporaryCachePath, "CgsImport-" + Guid.NewGuid().ToString("N"));
            _captureKeyboard = WebGLInput.captureAllKeyboardInput;
            _selected = selected;
            _zipOnly = zipOnly;
            try
            {
                Directory.CreateDirectory(_directory);
                WebGLInput.captureAllKeyboardInput = false;
                CgsPickFiles(_directory, CgsLocalization.Text(title),
                    CgsLocalization.Text(new UiMessage("common.cancel", "Cancel")), accept,
                    folder ? 1 : 0, zipOnly ? 1 : 0, Complete);
            }
            catch (Exception exception)
            {
                WebGLInput.captureAllKeyboardInput = _captureKeyboard;
                ReportError(exception.Message);
                Cleanup(_directory);
                _directory = null;
                _selected = null;
            }
        }

        [MonoPInvokeCallback(typeof(Action<int, IntPtr>))]
        private static void Complete(int status, IntPtr value)
        {
            if (_directory == null)
                return;

            WebGLInput.captureAllKeyboardInput = _captureKeyboard;
            var directory = _directory;
            var selected = _selected;
            _directory = null;
            _selected = null;
            try
            {
                if (status == 1)
                {
                    var fileName = Marshal.PtrToStringUTF8(value);
                    if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
                        throw new IOException(ReadError);

                    // ZIP confirmations and folder imports keep their files until the consumer releases them.
                    selected(Path.Combine(directory, fileName), () => Cleanup(directory));
                    return;
                }

                if (status == 3 && _zipOnly)
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
            Cleanup(directory);
        }

        private static void ReportError(string detail)
        {
            Debug.LogWarning(CardGameManager.ImportFailureErrorMessage + detail);
            CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.import",
                CardGameManager.ImportFailureErrorMessage + detail, ("detail", detail)));
        }

        private static void Cleanup(string directory)
        {
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
