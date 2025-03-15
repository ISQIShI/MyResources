using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    [CustomEditor(typeof(StateMachineVisualizer))]
    public class StateMachineVisualizerEditor : UnityEditor.Editor
    {
        private SerializedProperty stateMachineProperty;
        private SerializedProperty visualizeInGameViewProperty;
        private SerializedProperty showTransitionsProperty;
        private SerializedProperty backgroundColorProperty;
        private SerializedProperty textColorProperty;
        private SerializedProperty currentStateColorProperty;
        private SerializedProperty positionProperty;
        private SerializedProperty widthProperty;
        private SerializedProperty fontSizeProperty;

        private void OnEnable()
        {
            // 确保序列化对象有效
            if (serializedObject == null)
                return;

            stateMachineProperty = serializedObject.FindProperty("stateMachine");
            visualizeInGameViewProperty = serializedObject.FindProperty("visualizeInGameView");
            showTransitionsProperty = serializedObject.FindProperty("showTransitions");
            backgroundColorProperty = serializedObject.FindProperty("backgroundColor");
            textColorProperty = serializedObject.FindProperty("textColor");
            currentStateColorProperty = serializedObject.FindProperty("currentStateColor");
            positionProperty = serializedObject.FindProperty("position");
            widthProperty = serializedObject.FindProperty("width");
            fontSizeProperty = serializedObject.FindProperty("fontSize");
        }

        public override void OnInspectorGUI()
        {
            // 确保序列化对象有效
            if (serializedObject == null)
                return;

            serializedObject.Update();

            // 安全绘制属性，确保每个属性都存在
            if (stateMachineProperty != null)
                EditorGUILayout.PropertyField(stateMachineProperty);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("可视化选项", EditorStyles.boldLabel);

            if (visualizeInGameViewProperty != null)
                EditorGUILayout.PropertyField(visualizeInGameViewProperty, new GUIContent("在游戏视图中显示"));
            if (showTransitionsProperty != null)
                EditorGUILayout.PropertyField(showTransitionsProperty, new GUIContent("显示转换"));

            EditorGUILayout.Space(5);
            if (backgroundColorProperty != null)
                EditorGUILayout.PropertyField(backgroundColorProperty, new GUIContent("背景颜色"));
            if (textColorProperty != null)
                EditorGUILayout.PropertyField(textColorProperty, new GUIContent("文本颜色"));
            if (currentStateColorProperty != null)
                EditorGUILayout.PropertyField(currentStateColorProperty, new GUIContent("当前状态颜色"));

            EditorGUILayout.Space(5);
            if (positionProperty != null)
                EditorGUILayout.PropertyField(positionProperty, new GUIContent("位置"));
            if (widthProperty != null)
                EditorGUILayout.PropertyField(widthProperty, new GUIContent("宽度"));
            if (fontSizeProperty != null)
                EditorGUILayout.PropertyField(fontSizeProperty, new GUIContent("字体大小"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            // 运行时控制按钮
            if (Application.isPlaying)
            {
                StateMachineVisualizer visualizer = target as StateMachineVisualizer;
                if (visualizer != null && visualizeInGameViewProperty != null)
                {
                    if (GUILayout.Button(visualizeInGameViewProperty.boolValue ? "隐藏可视化" : "显示可视化"))
                    {
                        visualizer.ToggleVisibility();
                        visualizeInGameViewProperty.boolValue = !visualizeInGameViewProperty.boolValue;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
}