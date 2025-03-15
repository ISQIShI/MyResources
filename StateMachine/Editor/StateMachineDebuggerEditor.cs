using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    [CustomEditor(typeof(StateMachineDebugger))]
    public class StateMachineDebuggerEditor : UnityEditor.Editor
    {
        private SerializedProperty stateMachineIdProperty;
        private SerializedProperty debuggerNameProperty;

        private void OnEnable()
        {
            stateMachineIdProperty = serializedObject.FindProperty("stateMachineId");
            debuggerNameProperty = serializedObject.FindProperty("debuggerName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            StateMachineDebugger debugger = (StateMachineDebugger)target;

            // 调试器名称
            EditorGUILayout.PropertyField(debuggerNameProperty, new GUIContent("调试器名称"));

            EditorGUI.BeginChangeCheck();

            // 显示当前状态机ID
            EditorGUILayout.PropertyField(stateMachineIdProperty, new GUIContent("状态机ID"));

            // 应用更改
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                // 移除对Initialize()方法的调用，或者用适当的方法替换
                // debugger.Initialize(); - 移除这行，改为下面的逻辑

                // 如果在播放模式中，刷新调试器
                if (Application.isPlaying)
                {
                    // 调用刷新方法（如果存在）
                    System.Reflection.MethodInfo refreshMethod = debugger.GetType().GetMethod("RefreshStateMachine",
                                                                                       System.Reflection.BindingFlags.Public |
                                                                                       System.Reflection.BindingFlags.Instance);
                    if (refreshMethod != null)
                    {
                        refreshMethod.Invoke(debugger, null);
                    }
                }
            }

            EditorGUILayout.Space();

            // 运行时状态信息
            if (Application.isPlaying)
            {
                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.textColor = Color.white;
                boxStyle.fontStyle = FontStyle.Bold;
                boxStyle.alignment = TextAnchor.MiddleLeft;
                boxStyle.padding = new RectOffset(10, 10, 10, 10);

                EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(boxStyle);

                // 显示当前状态
                string currentState = debugger.CurrentStateName;
                if (string.IsNullOrEmpty(currentState))
                {
                    EditorGUILayout.LabelField("当前状态: <无>", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("当前状态: " + currentState, EditorStyles.boldLabel);
                }

                // 显示状态列表
                if (debugger.States != null && debugger.States.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("状态列表:", EditorStyles.boldLabel);

                    foreach (string stateName in debugger.States)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // 标记当前状态
                        if (stateName == currentState)
                        {
                            GUILayout.Label("►", GUILayout.Width(20));
                        }
                        else
                        {
                            GUILayout.Label(" ", GUILayout.Width(20));
                        }

                        // 状态名称
                        GUILayout.Label(stateName);

                        // 添加切换按钮
                        if (GUILayout.Button("切换", GUILayout.Width(50)))
                        {
                            debugger.ChangeState(stateName);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("未找到状态，请确保状态机已初始化");
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("在播放模式下可以查看和操作状态机", MessageType.Info);
            }

            EditorGUILayout.Space();

            // 添加打开调试窗口的按钮
            if (GUILayout.Button("打开状态机调试窗口", GUILayout.Height(30)))
            {
                // 使用新的方法打开调试窗口
                OpenDebugWindowWithDebugger(debugger);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // 打开调试窗口并选择指定的调试器
        private void OpenDebugWindowWithDebugger(StateMachineDebugger debugger)
        {
            // 打开调试窗口
            StateMachineDebugWindow window = EditorWindow.GetWindow<StateMachineDebugWindow>();
            window.titleContent = new GUIContent("状态机调试器");
            window.Show();

            // 延迟一帧后选择调试器
            EditorApplication.delayCall += () =>
            {
                // 使用反射调用SelectDebugger方法
                System.Reflection.MethodInfo selectMethod = window.GetType().GetMethod("SelectDebugger");
                if (selectMethod != null)
                {
                    selectMethod.Invoke(window, new object[] { debugger });
                }
                // 如果没有SelectDebugger方法，至少刷新窗口
                else
                {
                    window.Repaint();
                }
            };
        }
    }
}