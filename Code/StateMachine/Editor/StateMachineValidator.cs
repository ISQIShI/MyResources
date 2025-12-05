using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    /// <summary>
    /// 状态机结构验证工具，用于调试子状态机识别问题
    /// </summary>
    public class StateMachineValidator : EditorWindow
    {
        private StateMachineDebugger debugger;
        private Vector2 scrollPosition;

        [MenuItem("Window/StateMachine/StateMachine Validator")]
        private static void Init()
        {
            StateMachineValidator window = GetWindow<StateMachineValidator>();
            window.titleContent = new GUIContent("状态机验证器");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // 选择状态机调试器
            debugger = EditorGUILayout.ObjectField("状态机调试器", debugger, typeof(StateMachineDebugger), true) as StateMachineDebugger;

            EditorGUILayout.Space();

            if (debugger == null)
            {
                EditorGUILayout.HelpBox("请选择一个StateMachineDebugger组件", MessageType.Info);
            }
            else
            {
                // 按钮区域
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("分析状态机结构", GUILayout.Height(30)))
                {
                    AnalyzeStateMachine();
                }

                if (GUILayout.Button("检查子状态识别", GUILayout.Height(30)))
                {
                    ValidateSubStateMachines();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // 显示基本信息
                EditorGUILayout.LabelField("状态机名称:", debugger.name);
                EditorGUILayout.LabelField("当前状态:", debugger.CurrentStateName);
                EditorGUILayout.LabelField("状态数量:", debugger.States.Count.ToString());

                // 滚动区域显示详细信息
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                if (debugger.States != null && debugger.States.Count > 0)
                {
                    EditorGUILayout.LabelField("状态列表:", EditorStyles.boldLabel);

                    foreach (string stateName in debugger.States)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                        // 检查是否是当前状态
                        bool isCurrent = (stateName == debugger.CurrentStateName);
                        if (isCurrent)
                        {
                            EditorGUILayout.LabelField("►", GUILayout.Width(15));
                        }
                        else
                        {
                            EditorGUILayout.LabelField(" ", GUILayout.Width(15));
                        }

                        EditorGUILayout.LabelField(stateName);

                        // 如果在播放模式，添加"跳转"按钮
                        if (Application.isPlaying)
                        {
                            if (GUILayout.Button("跳转", GUILayout.Width(50)))
                            {
                                debugger.ChangeState(stateName);
                            }
                        }

                        // 添加"检查"按钮
                        if (GUILayout.Button("检查", GUILayout.Width(50)))
                        {
                            CheckState(stateName);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 分析状态机基本结构
        /// </summary>
        private void AnalyzeStateMachine()
        {
            if (debugger == null)
                return;

            try
            {
                // 获取主状态机对象
                FieldInfo stateMachineField = debugger.GetType().GetField("stateMachine",
                                                                        BindingFlags.Instance |
                                                                        BindingFlags.NonPublic);
                if (stateMachineField == null)
                {
                    Debug.LogError("无法找到stateMachine字段");
                    return;
                }

                object mainStateMachine = stateMachineField.GetValue(debugger);
                if (mainStateMachine == null)
                {
                    Debug.LogError("状态机对象为空");
                    return;
                }

                // 输出状态机类型信息
                Type stateMachineType = mainStateMachine.GetType();
                Debug.Log($"状态机类型: {stateMachineType.FullName}");

                // 检查是否为StateMachine<string>类型
                bool isCorrectType = typeof(StateMachine<string>).IsAssignableFrom(stateMachineType);
                Debug.Log($"是否继承自StateMachine<string>: {isCorrectType}");

                // 输出状态数量
                if (debugger.States != null)
                {
                    Debug.Log($"状态总数: {debugger.States.Count}");
                    Debug.Log($"状态列表: {string.Join(", ", debugger.States)}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"分析状态机时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 验证子状态机识别
        /// </summary>
        private void ValidateSubStateMachines()
        {
            if (debugger == null)
                return;

            try
            {
                int subMachineCount = 0;
                Dictionary<string, List<string>> subStates = new Dictionary<string, List<string>>();

                foreach (string stateName in debugger.States)
                {
                    // 获取状态对象
                    StateBase<string> state = GetStateObject(stateName);
                    if (state == null)
                    {
                        Debug.LogWarning($"无法获取状态对象: {stateName}");
                        continue;
                    }

                    // 输出状态的类型信息
                    Type stateType = state.GetType();
                    Debug.Log($"状态 '{stateName}' 的类型: {stateType.FullName}");

                    // 检查是否是子状态机
                    bool isSubMachine = IsStateMachine(state);
                    Debug.Log($"状态 '{stateName}' 是子状态机: {isSubMachine}");

                    if (isSubMachine)
                    {
                        subMachineCount++;

                        // 获取子状态
                        List<string> childStates = GetSubStates(state);
                        subStates[stateName] = childStates;

                        Debug.Log($"子状态机 '{stateName}' 包含 {childStates.Count} 个子状态: {string.Join(", ", childStates)}");

                        // 检查反射访问字段
                        CheckReflectionFields(state);
                    }
                }

                Debug.Log($"在状态机 {debugger.name} 中发现 {subMachineCount} 个子状态机");
            }
            catch (Exception ex)
            {
                Debug.LogError($"验证子状态机时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查单个状态的详细信息
        /// </summary>
        private void CheckState(string stateName)
        {
            try
            {
                // 获取状态对象
                StateBase<string> state = GetStateObject(stateName);
                if (state == null)
                {
                    Debug.LogError($"无法获取状态对象: {stateName}");
                    return;
                }

                // 输出状态基本信息
                Debug.Log($"========== 状态 '{stateName}' 详细信息 ==========");
                Debug.Log($"类型: {state.GetType().FullName}");

                // 检查继承关系
                Type currentType = state.GetType();
                string inheritanceChain = currentType.Name;

                while (currentType.BaseType != null && currentType.BaseType != typeof(object))
                {
                    currentType = currentType.BaseType;
                    inheritanceChain += " → " + currentType.Name;
                }

                Debug.Log($"继承链: {inheritanceChain}");

                // 检查是否为子状态机
                bool isSubMachine = IsStateMachine(state);
                Debug.Log($"是子状态机: {isSubMachine}");

                // 输出其他有用信息
                Debug.Log($"附加信息: {state.GetStateAdditionalInformationAsString()}");

                // 输出转换信息
                List<TransitionInfo> transitions = debugger.GetStateTransitions(stateName);
                Debug.Log($"转换数量: {transitions?.Count ?? 0}");

                if (transitions != null && transitions.Count > 0)
                {
                    Debug.Log("转换列表:");
                    foreach (TransitionInfo t in transitions)
                    {
                        Debug.Log($"  → {t.TargetState} {(string.IsNullOrEmpty(t.ConditionSign) ? "" : $"[{t.ConditionSign}]")}");
                    }
                }

                // 如果是子状态机，检查子状态
                if (isSubMachine)
                {
                    CheckSubStateMachine(state, stateName);
                }

                Debug.Log($"========== 状态 '{stateName}' 检查结束 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError($"检查状态时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查子状态机的详细信息
        /// </summary>
        private void CheckSubStateMachine(StateBase<string> state, string stateName)
        {
            try
            {
                Debug.Log($"检查子状态机 '{stateName}' 的内部结构:");

                // 获取子状态
                List<string> subStates = GetSubStates(state);
                Debug.Log($"子状态数量: {subStates.Count}");

                if (subStates.Count > 0)
                {
                    Debug.Log($"子状态列表: {string.Join(", ", subStates)}");
                }

                // 获取当前子状态
                StateBase<string> currentSubState = GetCurrentSubState(state);
                if (currentSubState != null)
                {
                    Debug.Log($"当前子状态: {currentSubState.stateName}");
                }
                else
                {
                    Debug.LogWarning("无法获取当前子状态");
                }

                // 检查statesDic字段
                CheckReflectionFields(state);
            }
            catch (Exception ex)
            {
                Debug.LogError($"检查子状态机时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查可通过反射访问的关键字段
        /// </summary>
        private void CheckReflectionFields(StateBase<string> state)
        {
            if (state == null)
                return;

            Type stateType = state.GetType();

            // 检查关键字段
            string[] fieldNames = new string[] {
                "statesDic", "defaultState", "currentState", "stateChangeInfos"
            };

            foreach (string fieldName in fieldNames)
            {
                FieldInfo field = null;
                Type currentType = stateType;

                // 逐级查找字段
                while (currentType != null && field == null)
                {
                    field = currentType.GetField(fieldName,
                                              BindingFlags.Instance |
                                              BindingFlags.NonPublic);

                    if (field == null)
                        currentType = currentType.BaseType;
                }

                if (field != null)
                {
                    Debug.Log($"找到字段 '{fieldName}' 在类型 {currentType.Name} 中");

                    try
                    {
                        object fieldValue = field.GetValue(state);
                        Debug.Log($"字段值类型: {(fieldValue != null ? fieldValue.GetType().Name : "null")}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"无法获取字段值: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到字段 '{fieldName}'");
                }
            }
        }

        // 以下方法与ImguiStateMachineGraphView中的相同

        private StateBase<string> GetStateObject(string stateName)
        {
            // 实现与ImguiStateMachineGraphView中的GetStateObject相同
            if (debugger == null || string.IsNullOrEmpty(stateName))
                return null;

            try
            {
                // 获取debugger中的stateMachine字段
                FieldInfo stateMachineField = debugger.GetType().GetField("stateMachine",
                                                                        BindingFlags.Instance |
                                                                        BindingFlags.NonPublic);

                if (stateMachineField == null)
                    return null;

                // 获取stateMachine对象
                object stateMachine = stateMachineField.GetValue(debugger);
                if (stateMachine == null)
                    return null;

                // 获取statesDic字段
                FieldInfo statesDicField = stateMachine.GetType().GetField("statesDic",
                                                                      BindingFlags.Instance |
                                                                      BindingFlags.NonPublic);

                if (statesDicField == null)
                    return null;

                // 获取states字典
                object statesDict = statesDicField.GetValue(stateMachine);
                if (statesDict == null)
                    return null;

                // 尝试使用TryGetValue方法获取状态
                MethodInfo tryGetValueMethod = statesDict.GetType().GetMethod("TryGetValue");
                if (tryGetValueMethod == null)
                    return null;

                // 调用TryGetValue
                object[] parameters = new object[] { stateName, null };
                bool result = (bool)tryGetValueMethod.Invoke(statesDict, parameters);

                if (result && parameters[1] != null)
                {
                    return parameters[1] as StateBase<string>;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool IsStateMachine(StateBase<string> state)
        {
            if (state == null)
                return false;

            // 方法1：使用is操作符
            if (state is StateMachine<string>)
                return true;

            // 方法2：检查类型继承关系
            Type currentType = state.GetType();
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType &&
                    currentType.GetGenericTypeDefinition() == typeof(StateMachine<>) &&
                    currentType.GetGenericArguments()[0] == typeof(string))
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            // 方法3：检查是否有statesDic字段
            FieldInfo statesDicField = null;
            currentType = state.GetType();

            while (currentType != null && statesDicField == null)
            {
                statesDicField = currentType.GetField("statesDic", BindingFlags.Instance | BindingFlags.NonPublic);
                if (statesDicField == null)
                    currentType = currentType.BaseType;
            }

            if (statesDicField != null)
            {
                object statesValue = statesDicField.GetValue(state);
                if (statesValue != null && statesValue.GetType().IsGenericType &&
                    statesValue.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return true;
                }
            }

            return false;
        }

        private List<string> GetSubStates(StateBase<string> state)
        {
            List<string> subStateNames = new List<string>();

            if (state == null || !IsStateMachine(state))
                return subStateNames;

            try
            {
                // 获取statesDic字段
                FieldInfo statesDicField = null;
                Type currentType = state.GetType();

                while (currentType != null && statesDicField == null)
                {
                    statesDicField = currentType.GetField("statesDic", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (statesDicField == null)
                        currentType = currentType.BaseType;
                }

                if (statesDicField == null)
                    return subStateNames;

                // 获取states字典
                object statesDict = statesDicField.GetValue(state);
                if (statesDict == null)
                    return subStateNames;

                // 获取字典的Keys属性
                PropertyInfo keysProperty = statesDict.GetType().GetProperty("Keys");
                if (keysProperty == null)
                    return subStateNames;

                // 获取键集合
                object keysCollection = keysProperty.GetValue(statesDict);
                if (!(keysCollection is System.Collections.IEnumerable keys))
                    return subStateNames;

                // 遍历所有键（状态名）
                foreach (object key in keys)
                {
                    if (key != null)
                    {
                        subStateNames.Add(key.ToString());
                    }
                }
            }
            catch
            {
                // 忽略异常
            }

            return subStateNames;
        }

        private StateBase<string> GetCurrentSubState(StateBase<string> state)
        {
            if (state == null || !IsStateMachine(state))
                return null;

            try
            {
                // 获取currentState字段
                FieldInfo currentStateField = null;
                Type currentType = state.GetType();

                while (currentType != null && currentStateField == null)
                {
                    currentStateField = currentType.GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (currentStateField == null)
                        currentType = currentType.BaseType;
                }

                if (currentStateField != null)
                {
                    return currentStateField.GetValue(state) as StateBase<string>;
                }
            }
            catch
            {
                // 忽略异常
            }

            return null;
        }
    }
}