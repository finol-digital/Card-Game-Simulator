/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using Cgs.UI.ScrollRects;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.EditMode
{
    public class ScrollNavigationWiringTests
    {
        [Test]
        public void GeneralSettingsMainViewKeepsSelectionVisible()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Settings.unity");
            try
            {
                var scroll = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ScrollRect>(true))
                    .Single(view => view.name == "Settings ScrollView");
                Assert.IsNotNull(scroll.GetComponent<ScrollRectAutoScroll>());
                Assert.IsNotNull(scroll.content);
                Assert.IsNotNull(scroll.viewport);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [TestCase("Assets/Prefabs/Menus/Play Settings Menu.prefab")]
        [TestCase("Assets/Prefabs/Menus/Card Editor Menu.prefab")]
        [TestCase("Assets/Prefabs/Menus/Card Game Editor Menu.prefab")]
        [TestCase("Assets/Prefabs/Menus/CGS Games Browser Menu.prefab")]
        [TestCase("Assets/Prefabs/Menus/Move Menu.prefab")]
        [TestCase("Assets/Prefabs/Components/Dropdown.prefab")]
        public void MenuViewsAndDropdownsKeepSelectionVisible(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab);
            var views = prefab.GetComponentsInChildren<ScrollRect>(true);
            Assert.IsNotEmpty(views);
            foreach (var view in views)
            {
                Assert.IsNotNull(view.GetComponent<ScrollRectAutoScroll>(), path + ": " + view.name);
                Assert.IsNotNull(view.content, path + ": " + view.name);
            }
        }
    }
}
