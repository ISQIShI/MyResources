using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    /// <summary>
    /// 使用IMGUI绘制的状态机图表视图
    /// </summary>
    public class ImguiStateMachineGraphView
    {
        // 状态机引用和数据
        private StateMachineDebugger debugger;
        private Dictionary<string, NodeData> nodeData = new Dictionary<string, NodeData>();
        private Dictionary<string, List<string>> childStates = new Dictionary<string, List<string>>();
        private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();

        // 视图控制
        private Vector2 scrollPosition;
        private float zoomLevel = 1.0f;
        private Vector2 viewSize = new Vector2(2000, 2000);

        // 拖拽交互状态
        private string draggingNode;
        private Vector2 dragOffset;

        // 视觉样式
        private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        private Color activeColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private Color subMachineColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        private Color activeSubMachineColor = new Color(0.4f, 0.6f, 0.9f, 1f);
        private Color transitionColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
        private Color subTransitionColor = new Color(0.7f, 0.9f, 1f, 0.8f);

        // 节点数据类
        public class NodeData
        {
            public Rect rect;
            public bool isSubMachine;
            public bool isCurrent;
            public List<TransitionInfo> transitions = new List<TransitionInfo>();
        }

        /// <summary>
        /// 设置状态机
        /// </summary>
        public void SetStateMachine(StateMachineDebugger debugger)
        {
            this.debugger = debugger;

            // 分析状态机结构
            AnalyzeStateMachine();

            // 初始化节点布局
            if (nodeData.Count == 0)
            {
                InitializeLayout();
            }
            else
            {
                // 更新节点数据
                UpdateNodeData();
            }
        }

        /// <summary>
        /// 绘制图表
        /// </summary>
        public void Draw(Rect rect)
        {
            if (debugger == null)
            {
                EditorGUI.HelpBox(rect, "未设置状态机", MessageType.Info);
                return;
            }

            // 绘制背景和网格
            DrawBackground(rect);

            // 开始滚动视图
            EditorGUI.DrawRect(rect, backgroundColor);
            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, viewSize.x, viewSize.y));

            try
            {
                // 绘制连线
                DrawTransitions();

                // 绘制节点
                DrawNodes();

                // 处理交互
                HandleInteraction(Event.current);
            }
            catch (Exception ex)
            {
                Debug.LogError($"绘制状态图时发生错误: {ex.Message}\n{ex.StackTrace}");
            }

            GUI.EndScrollView();

            // 绘制信息面板
            DrawInfoPanel(rect);
        }

        /// <summary>
        /// 绘制背景网格
        /// </summary>
        private void DrawBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, backgroundColor);

            // 绘制网格线
            Handles.color = gridColor;
            float gridSize = 50f * zoomLevel;

            // 垂直线
            for (float x = gridSize; x < rect.width; x += gridSize)
            {
                Handles.DrawLine(
                    new Vector3(x, 0, 0),
                    new Vector3(x, rect.height, 0)
                );
            }

            // 水平线
            for (float y = gridSize; y < rect.height; y += gridSize)
            {
                Handles.DrawLine(
                    new Vector3(0, y, 0),
                    new Vector3(rect.width, y, 0)
                );
            }
        }

        /// <summary>
        /// 分析状态机结构，识别子状态机
        /// </summary>
        private void AnalyzeStateMachine()
        {
            childStates.Clear();

            if (debugger == null || debugger.States == null || debugger.States.Count == 0)
                return;

            Debug.Log($"===== 分析状态机 {debugger.name} 结构开始 =====");
            Debug.Log($"总状态数: {debugger.States.Count}");

            // 获取当前状态名
            string currentStateName = debugger.CurrentStateName;

            foreach (string stateName in debugger.States)
            {
                // 获取状态对象
                StateBase<string> state = GetStateObject(stateName);

                if (state == null)
                {
                    Debug.LogWarning($"无法获取状态对象: {stateName}");
                    continue;
                }

                // 检查状态类型
                Type stateType = state.GetType();
                Debug.Log($"状态 '{stateName}' 类型: {stateType.FullName}");

                // 尝试确定是否为子状态机
                bool isSubMachine = IsStateMachine(state);
                Debug.Log($"状态 '{stateName}' 是子状态机: {isSubMachine}");

                // 更新节点数据
                if (!nodeData.ContainsKey(stateName))
                {
                    nodeData[stateName] = new NodeData
                    {
                        rect = new Rect(0, 0, 150, isSubMachine ? 120 : 40),
                        isSubMachine = isSubMachine,
                        isCurrent = (stateName == currentStateName)
                    };
                }
                else
                {
                    nodeData[stateName].isSubMachine = isSubMachine;
                    nodeData[stateName].isCurrent = (stateName == currentStateName);
                }

                // 获取状态的转换信息
                nodeData[stateName].transitions = debugger.GetStateTransitions(stateName);

                // 如果是子状态机，获取子状态
                if (isSubMachine)
                {
                    Debug.Log($"检测到子状态机: {stateName}");
                    List<string> subStates = GetSubStates(state);

                    if (subStates.Count > 0)
                    {
                        childStates[stateName] = subStates;

                        // 默认展开
                        if (!expandedStates.ContainsKey(stateName))
                        {
                            expandedStates[stateName] = true;
                        }

                        Debug.Log($"子状态机 '{stateName}' 包含 {subStates.Count} 个子状态: {string.Join(", ", subStates)}");
                    }
                }
            }

            Debug.Log($"===== 分析状态机结构完成 =====");
            Debug.Log($"识别出 {childStates.Count} 个子状态机");
            foreach (var entry in childStates)
            {
                Debug.Log($"子状态机 '{entry.Key}' 包含: {string.Join(", ", entry.Value)}");
            }
        }

        /// <summary>
        /// 更新节点数据（当前状态等）
        /// </summary>
        private void UpdateNodeData()
        {
            if (debugger == null)
                return;

            string currentStateName = debugger.CurrentStateName;

            // 更新主状态的当前状态标记
            foreach (var pair in nodeData)
            {
                // 只处理主状态（不包含点的状态名）
                if (!pair.Key.Contains("."))
                {
                    pair.Value.isCurrent = (pair.Key == currentStateName);
                }
            }

            // 更新子状态的当前状态标记
            UpdateSubStateCurrent();
        }

        /// <summary>
        /// 判断状态是否为状态机
        /// </summary>
        private bool IsStateMachine(StateBase<string> state)
        {
            if (state == null)
                return false;

            // 尝试多种方法判断

            // 1. 直接类型判断
            if (state is StateMachine<string>)
            {
                Debug.Log($"通过is操作符确认为状态机: {state.GetType().FullName}");
                return true;
            }

            // 2. 检查继承关系
            Type currentType = state.GetType();
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType &&
                    currentType.GetGenericTypeDefinition() == typeof(StateMachine<>) &&
                    currentType.GetGenericArguments()[0] == typeof(string))
                {
                    Debug.Log($"通过继承关系确认为状态机: {state.GetType().FullName}");
                    return true;
                }

                currentType = currentType.BaseType;
            }

            // 3. 通过反射检查是否有states字段
            FieldInfo statesField = null;
            currentType = state.GetType();

            while (currentType != null && statesField == null)
            {
                statesField = currentType.GetField("statesDic", BindingFlags.Instance | BindingFlags.NonPublic);
                if (statesField == null)
                    currentType = currentType.BaseType;
                else
                    break;
            }

            if (statesField != null)
            {
                object statesValue = statesField.GetValue(state);
                if (statesValue != null && statesValue.GetType().IsGenericType &&
                    statesValue.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Debug.Log($"通过statesDic字段确认为状态机: {state.GetType().FullName}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取指定名称的状态对象
        /// </summary>
        private StateBase<string> GetStateObject(string stateName)
        {
            if (debugger == null || string.IsNullOrEmpty(stateName))
                return null;

            try
            {
                // 获取debugger中的stateMachine字段
                FieldInfo stateMachineField = debugger.GetType().GetField("stateMachine",
                                                                        BindingFlags.Instance |
                                                                        BindingFlags.NonPublic);

                if (stateMachineField == null)
                {
                    Debug.LogWarning("未找到stateMachine字段");
                    return null;
                }

                // 获取stateMachine对象
                object stateMachine = stateMachineField.GetValue(debugger);
                if (stateMachine == null)
                {
                    Debug.LogWarning("stateMachine字段为空");
                    return null;
                }

                // 获取states字段
                FieldInfo statesField = stateMachine.GetType().GetField("statesDic",
                                                                      BindingFlags.Instance |
                                                                      BindingFlags.NonPublic);

                if (statesField == null)
                {
                    Debug.LogWarning("未找到statesDic字段");
                    return null;
                }

                // 获取states字典
                object statesDict = statesField.GetValue(stateMachine);
                if (statesDict == null)
                {
                    Debug.LogWarning("states字典为空");
                    return null;
                }

                // 获取索引器方法
                MethodInfo tryGetValueMethod = statesDict.GetType().GetMethod("TryGetValue");
                if (tryGetValueMethod == null)
                {
                    Debug.LogWarning("未找到TryGetValue方法");
                    return null;
                }

                // 获取状态对象
                try
                {
                    object[] parameters = new object[] { stateName, null };
                    bool result = (bool)tryGetValueMethod.Invoke(statesDict, parameters);

                    if (result && parameters[1] != null)
                    {
                        return parameters[1] as StateBase<string>;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"获取状态对象失败: {ex.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取状态对象时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取子状态机的子状态
        /// </summary>
        private List<string> GetSubStates(StateBase<string> stateMachine)
        {
            List<string> subStateNames = new List<string>();

            if (stateMachine == null)
                return subStateNames;

            try
            {
                // 获取子状态机的statesDic字段
                FieldInfo statesDicField = null;
                Type stateType = stateMachine.GetType();

                while (stateType != null && statesDicField == null)
                {
                    statesDicField = stateType.GetField("statesDic", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (statesDicField == null)
                        stateType = stateType.BaseType;
                }

                if (statesDicField == null)
                {
                    Debug.LogWarning($"未找到子状态机的statesDic字段: {stateMachine.GetType().Name}");
                    return subStateNames;
                }

                // 获取states字典
                object statesDict = statesDicField.GetValue(stateMachine);
                if (statesDict == null)
                {
                    Debug.LogWarning("子状态机的statesDic字典为空或为静态emptyStatesDic引用");

                    // 检查是否为emptyStatesDic
                    FieldInfo emptyStatesDicField = stateMachine.GetType().GetField("emptyStatesDic",
                                                                                 BindingFlags.Static |
                                                                                 BindingFlags.NonPublic);
                    if (emptyStatesDicField != null)
                    {
                        object emptyDict = emptyStatesDicField.GetValue(null);
                        if (statesDict == emptyDict)
                        {
                            Debug.Log("子状态机使用emptyStatesDic，没有子状态");
                            return subStateNames;
                        }
                    }

                    return subStateNames;
                }

                // 获取字典的Keys属性
                PropertyInfo keysProperty = statesDict.GetType().GetProperty("Keys");
                if (keysProperty == null)
                {
                    Debug.LogWarning("未找到字典的Keys属性");
                    return subStateNames;
                }

                // 获取键集合
                object keysCollection = keysProperty.GetValue(statesDict);
                if (!(keysCollection is System.Collections.IEnumerable keys))
                {
                    Debug.LogWarning("无法获取字典键集合");
                    return subStateNames;
                }

                // 遍历所有键（状态名）
                foreach (object key in keys)
                {
                    if (key != null)
                    {
                        subStateNames.Add(key.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取子状态时发生错误: {ex.Message}");
            }

            return subStateNames;
        }

        /// <summary>
        /// 初始化节点布局
        /// </summary>
        private void InitializeLayout()
        {
            if (debugger == null || debugger.States == null)
                return;

            // 使用力导向算法布局，但这里简化为网格布局
            List<string> mainStates = debugger.States;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(mainStates.Count));
            float startX = 100;
            float startY = 100;

            // 布局主状态
            for (int i = 0; i < mainStates.Count; i++)
            {
                string stateName = mainStates[i];
                int row = i / columns;
                int col = i % columns;

                float x = startX + col * 200;
                float y = startY + row * 150;

                // 确定节点是否为子状态机
                bool isSubMachine = childStates.ContainsKey(stateName);

                // 更新节点数据
                if (!nodeData.ContainsKey(stateName))
                {
                    nodeData[stateName] = new NodeData
                    {
                        rect = new Rect(x, y, 150, isSubMachine ? 120 : 40),
                        isSubMachine = isSubMachine,
                        isCurrent = (stateName == debugger.CurrentStateName)
                    };
                }
                else
                {
                    NodeData data = nodeData[stateName];
                    data.rect = new Rect(x, y, 150, isSubMachine ? 120 : 40);
                    data.isSubMachine = isSubMachine;
                    data.isCurrent = (stateName == debugger.CurrentStateName);
                    nodeData[stateName] = data;
                }
            }

            // 布局子状态
            PlaceChildStates();
        }

        /// <summary>
        /// 布局子状态机内的子状态
        /// </summary>
        private void PlaceChildStates()
        {
            foreach (var pair in childStates)
            {
                string parentName = pair.Key;
                List<string> subStates = pair.Value;

                // 只处理已展开的子状态机
                if (!expandedStates.ContainsKey(parentName) || !expandedStates[parentName])
                    continue;

                // 确保父节点数据存在
                if (!nodeData.ContainsKey(parentName))
                    continue;

                Rect parentRect = nodeData[parentName].rect;

                // 调整父节点大小以容纳子状态
                int subColumns = Mathf.CeilToInt(Mathf.Sqrt(subStates.Count));
                int subRows = Mathf.CeilToInt((float)subStates.Count / subColumns);

                float newWidth = Math.Max(parentRect.width, subColumns * 130 + 20);
                float newHeight = 40 + subRows * 50 + 20;

                // 更新父节点大小
                NodeData parentData = nodeData[parentName];
                parentData.rect = new Rect(parentRect.x, parentRect.y, newWidth, newHeight);
                nodeData[parentName] = parentData;

                // 放置子状态
                for (int i = 0; i < subStates.Count; i++)
                {
                    int row = i / subColumns;
                    int col = i % subColumns;

                    float x = parentRect.x + 10 + col * 130;
                    float y = parentRect.y + 50 + row * 50;

                    string subStateName = subStates[i];
                    string fullSubName = parentName + "." + subStateName;

                    // 添加子状态节点
                    nodeData[fullSubName] = new NodeData
                    {
                        rect = new Rect(x, y, 120, 30),
                        isSubMachine = false,
                        isCurrent = false // 后续更新当前状态
                    };
                }
            }

            // 更新子状态的当前状态标记
            UpdateSubStateCurrent();
        }

        /// <summary>
        /// 更新子状态机中的当前状态标记
        /// </summary>
        private void UpdateSubStateCurrent()
        {
            foreach (var pair in childStates)
            {
                string parentName = pair.Key;

                // 如果父状态机不是当前状态，子状态都不是当前状态
                if (!nodeData.ContainsKey(parentName) || !nodeData[parentName].isCurrent)
                    continue;

                // 获取父状态机对象
                StateBase<string> parentState = GetStateObject(parentName);
                if (parentState == null || !IsStateMachine(parentState))
                    continue;

                // 获取父状态机的当前状态
                StateBase<string> currentSubState = GetCurrentSubState(parentState);
                if (currentSubState == null)
                    continue;

                string currentSubStateName = currentSubState.stateName;
                string fullSubName = parentName + "." + currentSubStateName;

                // 更新当前子状态
                if (nodeData.ContainsKey(fullSubName))
                {
                    NodeData data = nodeData[fullSubName];
                    data.isCurrent = true;
                    nodeData[fullSubName] = data;

                    Debug.Log($"当前子状态: {fullSubName}");
                }
            }
        }

        /// <summary>
        /// 获取子状态机的当前状态
        /// </summary>
        private StateBase<string> GetCurrentSubState(StateBase<string> stateMachine)
        {
            if (stateMachine == null || !IsStateMachine(stateMachine))
                return null;

            try
            {
                // 尝试获取currentState字段
                FieldInfo currentStateField = null;
                Type currentType = stateMachine.GetType();

                while (currentType != null && currentStateField == null)
                {
                    currentStateField = currentType.GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (currentStateField == null)
                        currentType = currentType.BaseType;
                }

                if (currentStateField != null)
                {
                    return currentStateField.GetValue(stateMachine) as StateBase<string>;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"获取当前子状态时出错: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 绘制所有节点
        /// </summary>
        private void DrawNodes()
        {
            // 先绘制子状态机容器
            foreach (var pair in nodeData)
            {
                string nodeName = pair.Key;
                NodeData data = pair.Value;

                if (data.isSubMachine && !nodeName.Contains("."))
                {
                    DrawSubMachineNode(nodeName, data);
                }
            }

            // 再绘制普通状态节点
            foreach (var pair in nodeData)
            {
                string nodeName = pair.Key;
                NodeData data = pair.Value;

                if (!data.isSubMachine && !nodeName.Contains("."))
                {
                    DrawStateNode(nodeName, data);
                }
            }

            // 最后绘制子状态
            foreach (var pair in nodeData)
            {
                string nodeName = pair.Key;
                NodeData data = pair.Value;

                if (nodeName.Contains("."))
                {
                    string displayName = nodeName.Substring(nodeName.LastIndexOf('.') + 1);
                    DrawStateNode(displayName, data);
                }
            }
        }

        /// <summary>
        /// 绘制普通状态节点
        /// </summary>
        private void DrawStateNode(string name, NodeData data)
        {
            Rect rect = data.rect;

            // 绘制节点背景
            Color bgColor = data.isCurrent ? activeColor : inactiveColor;
            EditorGUI.DrawRect(rect, bgColor);

            // 绘制边框
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(2f,
                new Vector3(rect.x, rect.y),
                new Vector3(rect.x + rect.width, rect.y),
                new Vector3(rect.x + rect.width, rect.y + rect.height),
                new Vector3(rect.x, rect.y + rect.height),
                new Vector3(rect.x, rect.y)
            );

            // 绘制状态名
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            GUI.Label(rect, name, labelStyle);
        }

        /// <summary>
        /// 绘制子状态机节点
        /// </summary>
        private void DrawSubMachineNode(string name, NodeData data)
        {
            Rect rect = data.rect;

            // 检查是否展开
            bool isExpanded = expandedStates.ContainsKey(name) && expandedStates[name];

            // 绘制节点背景
            Color bgColor = data.isCurrent ? activeSubMachineColor : subMachineColor;
            EditorGUI.DrawRect(rect, bgColor);

            // 绘制边框
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(2f,
                new Vector3(rect.x, rect.y),
                new Vector3(rect.x + rect.width, rect.y),
                new Vector3(rect.x + rect.width, rect.y + rect.height),
                new Vector3(rect.x, rect.y + rect.height),
                new Vector3(rect.x, rect.y)
            );

            // 绘制标题栏
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 25);
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.3f, 1f));

            // 绘制状态机名称
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleLeft;
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(titleRect.x + 5, titleRect.y, titleRect.width - 25, titleRect.height),
                    name + " [状态机]", titleStyle);

            // 绘制展开/折叠按钮
            Rect toggleRect = new Rect(titleRect.x + titleRect.width - 25, titleRect.y, 25, 25);
            if (GUI.Button(toggleRect, isExpanded ? "−" : "+", EditorStyles.boldLabel))
            {
                expandedStates[name] = !isExpanded;
                PlaceChildStates();
            }

            // 如果状态机是当前状态，显示标记
            if (data.isCurrent)
            {
                Rect markRect = new Rect(rect.x + 5, rect.y + 5, 10, 10);
                EditorGUI.DrawRect(markRect, Color.green);
            }
        }

        /// <summary>
        /// 绘制状态转换连线
        /// </summary>
        private void DrawTransitions()
        {
            // 绘制主状态之间的转换
            foreach (var pair in nodeData)
            {
                string fromState = pair.Key;
                NodeData fromData = pair.Value;

                // 跳过子状态（名称中包含点的状态）
                if (fromState.Contains("."))
                    continue;

                // 获取转换信息
                foreach (var transition in fromData.transitions)
                {
                    string toState = transition.TargetState;

                    // 确保目标状态存在
                    if (!nodeData.ContainsKey(toState))
                        continue;

                    NodeData toData = nodeData[toState];

                    // 绘制转换线
                    DrawTransitionLine(fromState, toState, fromData.rect, toData.rect, transition.ConditionSign, false);
                }
            }

            // 绘制子状态机内部转换
            DrawSubMachineTransitions();
        }

        /// <summary>
        /// 绘制子状态机内部的转换连线
        /// </summary>
        private void DrawSubMachineTransitions()
        {
            // 对每个展开的子状态机，绘制子状态间的转换
            foreach (var pair in childStates)
            {
                string parentName = pair.Key;

                // 只处理展开的子状态机
                if (!expandedStates.ContainsKey(parentName) || !expandedStates[parentName])
                    continue;

                // 获取子状态机
                StateBase<string> parentState = GetStateObject(parentName);
                if (parentState == null || !IsStateMachine(parentState))
                    continue;

                // 将StateMachine<string>转换为具体类型以便访问其内部状态
                StateMachine<string> subMachine = parentState as StateMachine<string>;
                if (subMachine == null)
                    continue;

                // 获取所有子状态
                List<string> subStateNames = pair.Value;

                // 处理每个子状态的转换
                foreach (string subStateName in subStateNames)
                {
                    string fullSubName = parentName + "." + subStateName;

                    if (!nodeData.ContainsKey(fullSubName))
                        continue;

                    // 获取该子状态的转换信息
                    List<TransitionInfo> transitions = GetSubStateTransitions(subMachine, subStateName);

                    if (transitions == null)
                        continue;

                    foreach (var transition in transitions)
                    {
                        string targetSubName = transition.TargetState;
                        string fullTargetName = parentName + "." + targetSubName;

                        // 确保目标状态存在
                        if (!nodeData.ContainsKey(fullTargetName))
                            continue;

                        // 绘制转换线
                        DrawTransitionLine(fullSubName, fullTargetName,
                                         nodeData[fullSubName].rect,
                                         nodeData[fullTargetName].rect,
                                         transition.ConditionSign, true);
                    }
                }
            }
        }

        /// <summary>
        /// 获取子状态机内部某个状态的所有转换
        /// </summary>
        private List<TransitionInfo> GetSubStateTransitions(StateMachine<string> subMachine, string stateName)
        {
            List<TransitionInfo> transitions = new List<TransitionInfo>();

            try
            {
                // 获取子状态机的states字段
                FieldInfo statesDicField = null;
                Type currentType = subMachine.GetType();

                while (currentType != null && statesDicField == null)
                {
                    statesDicField = currentType.GetField("statesDic", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (statesDicField == null)
                        currentType = currentType.BaseType;
                }

                if (statesDicField == null)
                    return transitions;

                // 获取states字典
                object statesDict = statesDicField.GetValue(subMachine);
                if (statesDict == null)
                    return transitions;

                // 获取索引器方法
                MethodInfo tryGetValueMethod = statesDict.GetType().GetMethod("TryGetValue");
                if (tryGetValueMethod == null)
                    return transitions;

                // 获取子状态对象
                object[] parameters = new object[] { stateName, null };
                bool result = (bool)tryGetValueMethod.Invoke(statesDict, parameters);

                if (!result || parameters[1] == null)
                    return transitions;

                StateBase<string> state = parameters[1] as StateBase<string>;
                if (state == null)
                    return transitions;

                // 获取stateChangeInfos字段
                FieldInfo stateChangeInfosField = null;
                currentType = state.GetType();

                while (currentType != null && stateChangeInfosField == null)
                {
                    stateChangeInfosField = currentType.GetField("stateChangeInfos",
                                                               BindingFlags.Instance |
                                                               BindingFlags.NonPublic);
                    if (stateChangeInfosField == null)
                        currentType = currentType.BaseType;
                }

                if (stateChangeInfosField == null)
                    return transitions;

                // 获取stateChangeInfos列表
                object stateChangeInfosList = stateChangeInfosField.GetValue(state);
                if (!(stateChangeInfosList is System.Collections.IEnumerable infosList))
                    return transitions;

                // 遍历所有转换
                foreach (object info in infosList)
                {
                    if (info == null)
                        continue;

                    // 获取targetState属性
                    PropertyInfo targetStateProperty = info.GetType().GetProperty("targetState");
                    if (targetStateProperty == null)
                        continue;

                    object targetStateObj = targetStateProperty.GetValue(info);
                    StateBase<string> targetState = targetStateObj as StateBase<string>;
                    if (targetState == null)
                        continue;

                    // 获取条件标识
                    PropertyInfo conditionSignProperty = info.GetType().GetProperty("conditionSign");
                    string conditionSign = null;

                    if (conditionSignProperty != null)
                    {
                        conditionSign = conditionSignProperty.GetValue(info) as string;
                    }

                    // 添加转换信息
                    transitions.Add(new TransitionInfo
                    {
                        TargetState = targetState.stateName,
                        ConditionSign = conditionSign
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取子状态转换时出错: {ex.Message}");
            }

            return transitions;
        }

        /// <summary>
        /// 绘制状态转换连线
        /// </summary>
        private void DrawTransitionLine(string fromState, string toState, Rect fromRect, Rect toRect, string conditionSign, bool isSubTransition)
        {
            // 确定线条颜色 - 子状态机内部转换用不同颜色
            Handles.color = isSubTransition ? subTransitionColor : transitionColor;

            // 计算连线的起点和终点（状态矩形的中心）
            Vector2 start = fromRect.center;
            Vector2 end = toRect.center;

            // 优化：计算与矩形边界的交点，使连线从矩形边缘开始和结束
            start = CalculateIntersectionPoint(start, end, fromRect);
            end = CalculateIntersectionPoint(end, start, toRect);

            // 绘制连线
            Handles.DrawAAPolyLine(2f, start, end);

            // 绘制箭头
            DrawArrow(start, end);

            // 如果有条件标签，显示在连线中点
            if (!string.IsNullOrEmpty(conditionSign))
            {
                Vector2 labelPos = Vector2.Lerp(start, end, 0.5f);

                // 计算标签尺寸
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = Color.white;
                Vector2 labelSize = labelStyle.CalcSize(new GUIContent(conditionSign));

                // 绘制背景
                Rect labelBg = new Rect(
                    labelPos.x - labelSize.x / 2 - 2,
                    labelPos.y - labelSize.y / 2 - 1,
                    labelSize.x + 4,
                    labelSize.y + 2
                );

                EditorGUI.DrawRect(labelBg, new Color(0.15f, 0.15f, 0.15f, 0.85f));
                GUI.Label(labelBg, conditionSign, labelStyle);
            }
        }

        /// <summary>
        /// 计算线段与矩形的交点
        /// </summary>
        private Vector2 CalculateIntersectionPoint(Vector2 start, Vector2 end, Rect rect)
        {
            // 计算方向向量
            Vector2 dir = (end - start).normalized;

            // 矩形中心
            Vector2 center = rect.center;

            // 矩形半宽高
            float halfWidth = rect.width / 2;
            float halfHeight = rect.height / 2;

            // 计算射线与矩形各边的相交参数
            float tx1 = (center.x - halfWidth - start.x) / dir.x;
            float tx2 = (center.x + halfWidth - start.x) / dir.x;
            float ty1 = (center.y - halfHeight - start.y) / dir.y;
            float ty2 = (center.y + halfHeight - start.y) / dir.y;

            // 找到最小的正交点参数
            float t = float.MaxValue;

            if (!float.IsNaN(tx1) && tx1 > 0 && tx1 < t)
                t = tx1;

            if (!float.IsNaN(tx2) && tx2 > 0 && tx2 < t)
                t = tx2;

            if (!float.IsNaN(ty1) && ty1 > 0 && ty1 < t)
                t = ty1;

            if (!float.IsNaN(ty2) && ty2 > 0 && ty2 < t)
                t = ty2;

            // 如果找不到交点，返回矩形中心
            if (t == float.MaxValue)
                return center;

            // 返回交点
            return start + dir * t;
        }

        /// <summary>
        /// 绘制箭头
        /// </summary>
        private void DrawArrow(Vector2 start, Vector2 end)
        {
            // 箭头尺寸
            float arrowSize = 10f;

            // 计算方向向量
            Vector2 dir = (end - start).normalized;

            // 计算垂直向量
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // 箭头三个点
            Vector2[] points = new Vector2[3];
            points[0] = end;
            points[1] = end - dir * arrowSize + perp * arrowSize * 0.5f;
            points[2] = end - dir * arrowSize - perp * arrowSize * 0.5f;

            // 绘制填充三角形
            Handles.DrawAAConvexPolygon(points.Select(p => (Vector3)p).ToArray());
        }

        /// <summary>
        /// 绘制信息面板
        /// </summary>
        private void DrawInfoPanel(Rect viewRect)
        {
            if (debugger == null)
                return;

            // 信息面板位于右上角
            float panelWidth = 200;
            float panelHeight = 100;
            Rect panelRect = new Rect(
                viewRect.width - panelWidth - 10,
                10,
                panelWidth,
                panelHeight
            );

            // 绘制半透明面板背景
            EditorGUI.DrawRect(panelRect, new Color(0.1f, 0.1f, 0.1f, 0.7f));

            // 绘制标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;

            Rect titleRect = new Rect(panelRect.x, panelRect.y, panelRect.width, 20);
            GUI.Label(titleRect, "状态机信息", titleStyle);

            // 绘制信息内容
            GUIStyle textStyle = new GUIStyle(EditorStyles.label);
            textStyle.normal.textColor = Color.white;

            float yPos = panelRect.y + 25;

            // 显示状态机名称和当前状态
            GUI.Label(new Rect(panelRect.x + 5, yPos, panelRect.width - 10, 20),
                    $"状态机: {debugger.name}", textStyle);
            yPos += 20;

            GUI.Label(new Rect(panelRect.x + 5, yPos, panelRect.width - 10, 20),
                    $"当前状态: {debugger.CurrentStateName}", textStyle);
            yPos += 20;

            GUI.Label(new Rect(panelRect.x + 5, yPos, panelRect.width - 10, 20),
                    $"状态数: {debugger.States.Count}", textStyle);
            yPos += 20;

            GUI.Label(new Rect(panelRect.x + 5, yPos, panelRect.width - 10, 20),
                    $"子状态机数: {childStates.Count}", textStyle);
        }

        /// <summary>
        /// 处理交互事件
        /// </summary>
        private void HandleInteraction(Event evt)
        {
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0) // 左键点击
                    {
                        // 检查是否点击了节点
                        Vector2 mousePos = evt.mousePosition + scrollPosition;
                        foreach (var pair in nodeData)
                        {
                            if (pair.Value.rect.Contains(mousePos))
                            {
                                string nodeName = pair.Key;

                                // 检查是否点击的是子状态机标题栏
                                if (pair.Value.isSubMachine && !nodeName.Contains("."))
                                {
                                    Rect titleRect = new Rect(pair.Value.rect.x, pair.Value.rect.y, pair.Value.rect.width, 25);
                                    if (titleRect.Contains(mousePos))
                                    {
                                        // 展开/折叠子状态机
                                        bool expanded = expandedStates.ContainsKey(nodeName) && expandedStates[nodeName];
                                        expandedStates[nodeName] = !expanded;
                                        PlaceChildStates();
                                        evt.Use();
                                        return;
                                    }
                                }

                                // 开始拖拽
                                draggingNode = nodeName;
                                dragOffset = mousePos - pair.Value.rect.position;
                                evt.Use();
                                return;
                            }
                        }
                    }
                    else if (evt.button == 1) // 右键点击
                    {
                        // 检查是否点击了节点
                        Vector2 mousePos = evt.mousePosition + scrollPosition;
                        foreach (var pair in nodeData)
                        {
                            if (pair.Value.rect.Contains(mousePos))
                            {
                                string nodeName = pair.Key;
                                ShowContextMenu(nodeName);
                                evt.Use();
                                return;
                            }
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (evt.button == 0 && !string.IsNullOrEmpty(draggingNode))
                    {
                        // 移动节点
                        Vector2 mousePos = evt.mousePosition + scrollPosition;
                        if (nodeData.ContainsKey(draggingNode))
                        {
                            NodeData data = nodeData[draggingNode];
                            Vector2 newPos = mousePos - dragOffset;

                            if (!draggingNode.Contains(".")) // 主节点
                            {
                                // 移动主节点
                                data.rect.position = newPos;
                                nodeData[draggingNode] = data;

                                // 如果是子状态机，同时移动所有子节点
                                if (data.isSubMachine && childStates.ContainsKey(draggingNode))
                                {
                                    foreach (string subName in childStates[draggingNode])
                                    {
                                        string fullSubName = draggingNode + "." + subName;
                                        if (nodeData.ContainsKey(fullSubName))
                                        {
                                            NodeData subData = nodeData[fullSubName];
                                            subData.rect.position += evt.delta;
                                            nodeData[fullSubName] = subData;
                                        }
                                    }
                                }
                            }
                            else // 子节点
                            {
                                // 只移动子节点，不影响父节点
                                data.rect.position = newPos;
                                nodeData[draggingNode] = data;
                            }
                        }
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (evt.button == 0 && !string.IsNullOrEmpty(draggingNode))
                    {
                        draggingNode = null;
                        evt.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    // 处理滚轮缩放
                    // 注: Unity编辑器中的滚动视图已经处理垂直滚动，这里暂不实现缩放功能
                    break;
            }
        }

        /// <summary>
        /// 显示节点上下文菜单
        /// </summary>
        private void ShowContextMenu(string nodeName)
        {
            GenericMenu menu = new GenericMenu();

            // 判断节点类型
            bool isSubMachine = nodeData[nodeName].isSubMachine;
            bool isSubState = nodeName.Contains(".");

            // 针对子状态机的选项
            if (isSubMachine && !isSubState)
            {
                bool expanded = expandedStates.ContainsKey(nodeName) && expandedStates[nodeName];
                menu.AddItem(new GUIContent(expanded ? "折叠" : "展开"), false, () =>
                {
                    expandedStates[nodeName] = !expanded;
                    PlaceChildStates();
                });
            }

            // 状态切换选项（仅播放模式下可用）
            if (Application.isPlaying && debugger != null)
            {
                if (isSubState)
                {
                    // 子状态切换
                    menu.AddItem(new GUIContent("切换到此状态"), false, () =>
                    {
                        TryChangeToSubState(nodeName);
                    });
                }
                else
                {
                    // 主状态切换
                    menu.AddItem(new GUIContent("切换到此状态"), false, () =>
                    {
                        debugger.ChangeState(nodeName);
                    });
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("切换到此状态 (仅播放模式)"));
            }

            // 节点居中选项
            menu.AddItem(new GUIContent("居中节点"), false, () =>
            {
                CenterOnNode(nodeName);
            });

            // 复位布局选项
            menu.AddItem(new GUIContent("重置所有节点位置"), false, () =>
            {
                InitializeLayout();
            });

            menu.ShowAsContext();
        }

        /// <summary>
        /// 尝试切换到子状态
        /// </summary>
        private void TryChangeToSubState(string fullStateName)
        {
            if (string.IsNullOrEmpty(fullStateName) || !fullStateName.Contains("."))
                return;

            string[] parts = fullStateName.Split(new char[] { '.' }, 2);
            if (parts.Length != 2)
                return;

            string parentName = parts[0];
            string subStateName = parts[1];

            // 首先切换到父状态
            debugger.ChangeState(parentName);

            // 然后尝试切换子状态
            StateBase<string> parentState = GetStateObject(parentName);
            if (parentState == null || !IsStateMachine(parentState))
                return;

            StateMachine<string> subMachine = parentState as StateMachine<string>;
            if (subMachine == null)
                return;

            // 使用反射调用子状态机的ChangeState方法
            MethodInfo changeStateMethod = subMachine.GetType().GetMethod("ChangeState",
                                                                       new Type[] { typeof(string) });

            if (changeStateMethod != null)
            {
                try
                {
                    changeStateMethod.Invoke(subMachine, new object[] { subStateName });
                    Debug.Log($"切换到子状态: {fullStateName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"切换子状态失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 居中显示某个节点
        /// </summary>
        private void CenterOnNode(string nodeName)
        {
            if (!nodeData.ContainsKey(nodeName))
                return;

            Rect nodeRect = nodeData[nodeName].rect;
            scrollPosition = nodeRect.center - new Vector2(viewSize.x / 2, viewSize.y / 2);
        }
    }
}