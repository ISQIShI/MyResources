using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    public class StateMachineDebugWindow : EditorWindow
    {
        // 调试器列表
        private List<StateMachineDebugger> debuggers = new List<StateMachineDebugger>();
        private string[] debuggerNames;
        private int selectedDebuggerIndex = -1;

        // 状态机图表视图
        private ImguiStateMachineGraphView graphView = new ImguiStateMachineGraphView();

        // 窗口状态
        private Vector2 debuggerListScrollPosition;
        private bool debuggersListExpanded = true;

        [MenuItem("Window/StateMachine/StateMachine Debugger")]
        public static void Init()
        {
            StateMachineDebugWindow window = GetWindow<StateMachineDebugWindow>();
            window.titleContent = new GUIContent("状态机调试器");
            window.Show();
        }

        private void OnEnable()
        {
            // 当窗口打开或Unity编辑器重新编译脚本时调用
            UpdateDebuggersList();
        }

        private void OnFocus()
        {
            // 当窗口获得焦点时调用
            UpdateDebuggersList();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space();

            if (debuggers.Count == 0)
            {
                EditorGUILayout.HelpBox("场景中没有找到StateMachineDebugger组件。\n添加StateMachineDebugger组件到游戏对象以调试状态机。", MessageType.Info);
                return;
            }

            // 使用垂直分割布局 - 顶部显示状态机列表，底部显示图表视图
            EditorGUILayout.BeginVertical();

            // 状态机选择区域（可折叠）
            debuggersListExpanded = EditorGUILayout.Foldout(debuggersListExpanded, "状态机列表", true);
            if (debuggersListExpanded)
            {
                DrawDebuggersList();
            }

            EditorGUILayout.Space();

            // 图表区域
            if (selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count)
            {
                // 计算图表视图区域高度 - 如果列表折叠则占用整个剩余空间
                float graphHeight = position.height - (debuggersListExpanded ? 120 : 40);
                Rect graphRect = EditorGUILayout.GetControlRect(false, graphHeight);

                // 绘制图表视图
                graphView.Draw(graphRect);
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个状态机进行调试。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                UpdateDebuggersList();
            }

            GUILayout.FlexibleSpace();

            if (selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count)
            {
                if (GUILayout.Button("重置视图", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    // 重新初始化图表布局
                    graphView = new ImguiStateMachineGraphView();
                    if (selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count)
                    {
                        graphView.SetStateMachine(debuggers[selectedDebuggerIndex]);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制状态机选择列表
        /// </summary>
        private void DrawDebuggersList()
        {
            debuggerListScrollPosition = EditorGUILayout.BeginScrollView(
                debuggerListScrollPosition,
                GUILayout.Height(Mathf.Min(debuggers.Count * 22 + 30, 150))
            );

            // 使用SelectionGrid而不是弹出菜单，以便更清晰地显示所有状态机
            selectedDebuggerIndex = GUILayout.SelectionGrid(
                selectedDebuggerIndex,
                debuggerNames,
                1,
                EditorStyles.radioButton
            );

            EditorGUILayout.EndScrollView();

            // 如果选择了新的状态机，更新图表视图
            if (GUI.changed && selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count)
            {
                graphView.SetStateMachine(debuggers[selectedDebuggerIndex]);
            }
        }

        /// <summary>
        /// 更新场景中的状态机调试器列表
        /// </summary>
        private void UpdateDebuggersList()
        {
            // 查找场景中所有状态机调试器
            debuggers.Clear();
            StateMachineDebugger[] foundDebuggers = FindObjectsOfType<StateMachineDebugger>();

            foreach (StateMachineDebugger debugger in foundDebuggers)
            {
                if (debugger != null)
                {
                    debuggers.Add(debugger);
                }
            }

            // 记录当前选中的调试器
            StateMachineDebugger selectedDebugger = selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count ?
                                                 debuggers[selectedDebuggerIndex] : null;

            // 准备显示名称
            debuggerNames = new string[debuggers.Count];
            for (int i = 0; i < debuggers.Count; i++)
            {
                // 使用GameObject路径作为名称
                string path = GetGameObjectPath(debuggers[i].gameObject);
                debuggerNames[i] = $"{path} [{debuggers[i].name}]";
            }

            // 恢复之前的选择
            if (selectedDebugger != null)
            {
                selectedDebuggerIndex = debuggers.IndexOf(selectedDebugger);
            }
            else if (debuggers.Count > 0 && (selectedDebuggerIndex < 0 || selectedDebuggerIndex >= debuggers.Count))
            {
                selectedDebuggerIndex = 0;
            }

            // 更新图表视图
            if (selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count)
            {
                graphView.SetStateMachine(debuggers[selectedDebuggerIndex]);
            }

            Repaint();
        }

        /// <summary>
        /// 获取GameObject的完整路径
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// 检测播放模式变化，更新状态机视图
        /// </summary>
        private void Update()
        {
            if (EditorApplication.isPlaying && selectedDebuggerIndex >= 0 && selectedDebuggerIndex < debuggers.Count)
            {
                // 在播放模式下实时更新图表视图以反映状态变化
                graphView.SetStateMachine(debuggers[selectedDebuggerIndex]);
                Repaint();
            }
        }

        // 添加到StateMachineDebugWindow类中
        public void SelectDebugger(StateMachineDebugger targetDebugger)
        {
            if (targetDebugger == null)
                return;

            // 更新调试器列表
            UpdateDebuggersList();

            // 查找并选择目标调试器
            for (int i = 0; i < debuggers.Count; i++)
            {
                if (debuggers[i] == targetDebugger)
                {
                    selectedDebuggerIndex = i;
                    // 更新图表视图
                    graphView.SetStateMachine(debuggers[i]);
                    Repaint();
                    break;
                }
            }
        }
    }
}