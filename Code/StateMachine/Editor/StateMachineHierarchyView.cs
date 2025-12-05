using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    /// <summary>
    /// 显示层级状态机结构的视图
    /// </summary>
    public class StateMachineHierarchyView
    {
        private StateMachineDebugger currentDebugger;
        private Vector2 scrollPosition;

        private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
        private GUIStyle stateStyle;
        private GUIStyle currentStateStyle;

        public StateMachineHierarchyView()
        {
            InitializeStyles();
        }

        public void SetStateMachine(StateMachineDebugger debugger)
        {
            currentDebugger = debugger;
        }

        private void InitializeStyles()
        {
            stateStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };

            currentStateStyle = new GUIStyle(stateStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
        }

        public void Draw(Rect rect)
        {
            if (currentDebugger == null || currentDebugger.StateMachine == null)
                return;

            // 绘制层级视图框
            GUI.Box(rect, "", EditorStyles.helpBox);

            // 开始滚动区域
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Width(rect.width),
                GUILayout.Height(rect.height)
            );

            // 绘制层级结构
            EditorGUILayout.BeginVertical();

            // 使用反射检查是否有子状态机
            DrawStateMachineHierarchy(currentDebugger.StateMachine);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawStateMachineHierarchy(StateMachine<string> stateMachine, int indent = 0)
        {
            if (stateMachine == null)
                return;

            string currentStateName = GetCurrentStateName(stateMachine);

            // 显示所有状态
            foreach (var stateName in GetStateMachineStates(stateMachine))
            {
                // 检查是否为嵌套状态机
                StateBase<string> state = GetState(stateMachine, stateName);
                bool isStateMachine = state is StateMachine<string>;
                bool isCurrentState = stateName.ToString() == currentStateName;

                // 创建缩进
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indent * 20);

                // 状态名
                GUIStyle style = isCurrentState ? currentStateStyle : stateStyle;

                if (isStateMachine)
                {
                    // 确保在字典中有该状态的展开状态
                    string key = GetStatePath(stateMachine, stateName);
                    if (!expandedStates.ContainsKey(key))
                        expandedStates[key] = false;

                    // 绘制状态机折叠项
                    bool expanded = EditorGUILayout.Foldout(
                        expandedStates[key],
                        stateName + " (状态机)",
                        true,
                        style
                    );

                    // 更新展开状态
                    if (expanded != expandedStates[key])
                    {
                        expandedStates[key] = expanded;
                    }
                }
                else
                {
                    // 普通状态只显示名称
                    EditorGUILayout.LabelField(stateName.ToString(), style);
                }

                EditorGUILayout.EndHorizontal();

                // 如果是展开的状态机，递归显示其子状态
                if (isStateMachine && expandedStates[GetStatePath(stateMachine, stateName)])
                {
                    DrawStateMachineHierarchy(state as StateMachine<string>, indent + 1);
                }
            }
        }

        private string GetCurrentStateName(StateMachine<string> stateMachine)
        {
            if (stateMachine == null)
                return string.Empty;

            var currentState = stateMachine.GetCurrentState();
            return currentState != null ? currentState.stateName.ToString() : string.Empty;
        }

        private IEnumerable<string> GetStateMachineStates(StateMachine<string> stateMachine)
        {
            if (stateMachine == null)
                return new List<string>();

            // 使用反射获取状态机中的状态字典
            var statesDicField = stateMachine.GetType().GetField("statesDic",
                               System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (statesDicField == null)
                return new List<string>();

            var statesDic = statesDicField.GetValue(stateMachine);
            if (statesDic == null)
                return new List<string>();

            List<string> states = new List<string>();

            // 获取字典的键(状态名)
            var keys = statesDic.GetType().GetProperty("Keys").GetValue(statesDic);

            // 遍历键集合
            foreach (var key in (System.Collections.IEnumerable)keys)
            {
                states.Add(key.ToString());
            }

            return states;
        }

        private StateBase<string> GetState(StateMachine<string> stateMachine, string stateName)
        {
            if (stateMachine == null || string.IsNullOrEmpty(stateName))
                return null;

            // 使用反射调用GetState方法
            System.Reflection.MethodInfo getStateMethod = stateMachine.GetType().GetMethod("GetState");

            return getStateMethod.Invoke(stateMachine, new[] { stateName }) as StateBase<string>;
        }

        private string GetStatePath(StateMachine<string> stateMachine, string stateName)
        {
            if (stateMachine == null || string.IsNullOrEmpty(stateName))
                return string.Empty;

            // 为了确保唯一标识，使用机器的实例ID和状态名
            //return stateMachine.GetInstanceID() + "_" + stateName;
            return stateMachine.stateName + "_" + stateName;
        }
    }
}