// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper methods for working with <see cref="EditorWindow"/>.
    /// </summary>
    public static class WindowUtils
    {
        public const string OkButtonText = "OK";
        public const string CancelButtonText = "Cancel";

        private const float ShortButtonWidth = 100.0f;

        /// <summary>
        /// Creates a right-aligned fixed-width button with the specified button text. If the button is clicked,
        /// the specified action will be invoked.
        /// </summary>
        public static void CreateRightAlignedButton(string text, Action action)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(text, GUILayout.Width(ShortButtonWidth)))
            {
                action();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Returns true if the running instance of Unity is headless (e.g. a command line build),
        /// and false if it's a normal instance of Unity.
        /// </summary>
        public static bool IsHeadlessMode()
        {
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        }
    }
}