using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace ScrollRects.Editor
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
