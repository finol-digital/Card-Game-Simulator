/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Cgs.UI.ScrollRects.Editor
{
    [CustomEditor(typeof(PassThroughScrollRect))]
    public class ScrollRectNestedEditor : ScrollRectEditor
    {
        private SerializedProperty _parentScrollRectProp;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private GUIContent _parentScrollRectGuiContent = new GUIContent("Parent ScrollRect");

        protected override void OnEnable()
        {
            base.OnEnable();
            _parentScrollRectProp = serializedObject.FindProperty("parentScrollRect");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_parentScrollRectProp, _parentScrollRectGuiContent);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
