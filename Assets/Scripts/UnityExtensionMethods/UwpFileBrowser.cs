#if ENABLE_WINMD_SUPPORT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Application = UnityEngine.WSA.Application;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.ViewManagement;

namespace UnityExtensionMethods
{
    public static class UwpFileBrowser
    {
        private static readonly PickerLocationId CurrentLocation = PickerLocationId.ComputerFolder;
        private static readonly PickerViewMode CurrentViewMode = PickerViewMode.List;

        private static readonly List<Extension> Extensions =
            new List<Extension> {new Extension("All files", "*")};

        public static List<string> Selection { get; } = new List<string>();

        public static List<StorageFile> LastOpenFiles { get; } = new List<StorageFile>();

        public static StorageFile LastOpenFile => (LastOpenFiles.Count > 0) ? LastOpenFiles[0] : null;

        private static bool IsBusy { get; set; }

        public static string OpenFilePanel()
        {
            IsBusy = true;

            Application.InvokeOnUIThread(OpenFile, false);

            do
            {
                //wait
            } while (IsBusy);

            return Selection.FirstOrDefault();
        }

        private static async void OpenFile()
        {
            if (ApplicationView.Value != ApplicationViewState.Snapped || ApplicationView.TryUnsnap())
            {
                Debug.Log("OpenFile...");

                Selection.Clear();
                LastOpenFiles.Clear();
                IsBusy = true;

                try
                {
                    FileOpenPicker openPicker = new FileOpenPicker();
                    openPicker.ViewMode = CurrentViewMode;
                    openPicker.SuggestedStartLocation = CurrentLocation;

                    foreach (string ext in Extensions.SelectMany(extension => extension.Extensions))
                        openPicker.FileTypeFilter.Add(ext.StartsWith("*") ? ext : "." + ext);

                    StorageFile file = await openPicker.PickSingleFileAsync();
                    if (file != null)
                    {
                        Selection.Add(file.Path);
                        LastOpenFiles.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }

                IsBusy = false;

                Debug.Log("OpenFile end: " + Selection.Count);
            }
            else
                Debug.LogError("OpenFile: could not unsnap! " + Selection.Count);
        }
    }

    public readonly struct Extension
    {
        public readonly string Name;
        public readonly string[] Extensions;

        public Extension(string name, params string[] extensions)
        {
            Name = name;
            Extensions = extensions;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(GetType().Name);
            stringBuilder.Append(" {");

            stringBuilder.Append("Name='");
            stringBuilder.Append(Name);
            stringBuilder.Append("', ");

            stringBuilder.Append("Extensions='");
            stringBuilder.Append(Extensions.Length);
            stringBuilder.Append("'");

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }
}
#endif
