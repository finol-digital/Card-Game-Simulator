#if ENABLE_WINMD_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Application = UnityEngine.WSA.Application;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;

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

        /// <summary>
        /// Lets the player choose a folder, then copies it into the app's cache, because the Store app can read
        /// a chosen folder only through the Windows storage APIs. Returns the copy's path, or null if cancelled.
        /// </summary>
        public static async Task<string> OpenFolderAsync()
        {
            var picked = new TaskCompletionSource<StorageFolder>();
            Application.InvokeOnUIThread(async () =>
            {
                try
                {
                    var folderPicker = new FolderPicker
                    {
                        ViewMode = CurrentViewMode,
                        SuggestedStartLocation = CurrentLocation
                    };
                    folderPicker.FileTypeFilter.Add("*");
                    picked.TrySetResult(await folderPicker.PickSingleFolderAsync());
                }
                catch (Exception exception)
                {
                    picked.TrySetException(exception);
                }
            }, false);

            var sourceFolder = await picked.Task;
            if (sourceFolder == null)
                return null;

            var cacheFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(
                "CgsImport-" + Guid.NewGuid().ToString("N"), CreationCollisionOption.GenerateUniqueName);
            var targetFolder = await cacheFolder.CreateFolderAsync(sourceFolder.Name,
                CreationCollisionOption.ReplaceExisting);
            await CopyFolderAsync(sourceFolder, targetFolder);
            return targetFolder.Path;
        }

        private static async Task CopyFolderAsync(StorageFolder sourceFolder, StorageFolder targetFolder)
        {
            foreach (var file in await sourceFolder.GetFilesAsync())
                await file.CopyAsync(targetFolder, file.Name, NameCollisionOption.ReplaceExisting);
            foreach (var folder in await sourceFolder.GetFoldersAsync())
                await CopyFolderAsync(folder,
                    await targetFolder.CreateFolderAsync(folder.Name, CreationCollisionOption.OpenIfExists));
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
