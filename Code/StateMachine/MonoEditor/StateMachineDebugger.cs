using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    /// <summary>
    /// 用于将状态机暴露给编辑器进行可视化调试的组件
    /// 将此组件添加到具有状态机的GameObject上
    /// </summary>
    public class StateMachineDebugger : MonoBehaviour
    {
        [SerializeField]
        private string stateMachineName = "状态机";

        [SerializeField]
        private StateMachine<string> stateMachine;

        [SerializeField]
        private bool registerExceptionHandler = true;

        // 获取和设置当前状态机
        public StateMachine<string> StateMachine
        {
            get => stateMachine;
            set => stateMachine = value;
        }

        // 状态机名称(用于UI显示)
        public string Name => string.IsNullOrEmpty(stateMachineName) ? gameObject.name : stateMachineName;

        // 当前状态名
        public string CurrentStateName
        {
            get
            {
                if (stateMachine?.GetCurrentState()?.stateName == null)
                    return string.Empty;

                return stateMachine.GetCurrentState().stateName.ToString();
            }
        }

        // 当前状态类型名称
        public string CurrentStateType
        {
            get
            {
                if (stateMachine?.GetCurrentState() == null)
                    return string.Empty;

                return stateMachine.GetCurrentState().GetType().Name;
            }
        }

        // 当前状态附加信息
        public string CurrentStateInfo
        {
            get
            {
                if (stateMachine?.GetCurrentState() == null)
                    return string.Empty;

                return stateMachine.GetCurrentState().GetStateAdditionalInformationAsString();
            }
        }

        // 所有状态列表
        public List<string> States
        {
            get
            {
                List<string> states = new List<string>();
                if (stateMachine == null)
                {
                    Debug.LogWarning("状态机为null");
                    return states;
                }

                // 使用反射获取状态机中的状态字典
                var fieldInfo = GetStatesDictField();
                if (fieldInfo == null)
                {
                    Debug.LogWarning("无法获取statesDic字段");
                    return states;
                }

                var statesDic = fieldInfo.GetValue(stateMachine);
                if (statesDic == null)
                {
                    Debug.LogWarning("statesDic为null或为空");
                    return states;
                }
                //Debug.Log($"状态字典类型: {statesDic.GetType().Name}");

                // 获取字典的键(状态名)
                var keys = statesDic.GetType().GetProperty("Keys").GetValue(statesDic);

                if (keys == null)
                {
                    Debug.LogWarning("Keys为null");
                    return states;
                }
                // 遍历键集合
                foreach (var key in (System.Collections.IEnumerable)keys)
                {
                    states.Add(key.ToString());
                }
                //Debug.Log($"找到{states.Count}个状态");
                return states;
            }
        }

        private void Awake()
        {
            if (registerExceptionHandler)
            {
                StateMachineException.RegisterExceptionOutputFunc(OnStateMachineException);
            }
        }
        private void Start()
        {
            stateMachine = GetComponent<TestMono>().stateMachine;
        }

        private void OnStateMachineException(StateMachineException exception, string additionalMessage)
        {
            Debug.LogError($"[{additionalMessage}] {exception.Message} in {Name}");
        }

        /// <summary>
        /// 手动切换到指定状态
        /// </summary>
        public void ChangeState(string stateName)
        {
            if (stateMachine == null)
                return;

            // 获取状态的实际类型(可能是泛型参数类型)
            Type stateNameType = GetStateNameType();

            if (stateNameType == typeof(string))
            {
                stateMachine.ChangeState(stateName);
            }
            else
            {
                // 尝试将状态名转换为正确的类型
                try
                {
                    object typedStateName = Convert.ChangeType(stateName, stateNameType);

                    // 使用反射调用ChangeState
                    MethodInfo changeStateMethod = stateMachine.GetType().GetMethod("ChangeState");
                    changeStateMethod.Invoke(stateMachine, new[] { typedStateName });
                }
                catch (Exception e)
                {
                    Debug.LogError($"无法切换状态: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 获取当前状态的所有转换
        /// </summary>
        public List<TransitionInfo> GetCurrentStateTransitions()
        {
            List<TransitionInfo> transitions = new List<TransitionInfo>();

            try
            {
                if (stateMachine?.GetCurrentState() == null)
                    return transitions;

                StateBase<string> currentState = stateMachine.GetCurrentState();

                // 以最安全的方式处理反射
                Type stateBaseType = currentState.GetType();
                while (stateBaseType != null)
                {
                    // 尝试获取 stateChangeInfos 字段
                    FieldInfo stateChangeInfosField = stateBaseType.GetField("stateChangeInfos",
                                                    BindingFlags.Instance | BindingFlags.NonPublic);

                    if (stateChangeInfosField != null)
                    {
                        // 获取字段值但不做任何类型转换
                        object fieldValue = stateChangeInfosField.GetValue(currentState);

                        // 检查是否为 null
                        if (fieldValue == null)
                            return transitions;

                        // 打印类型信息以帮助调试
                        Debug.Log($"stateChangeInfos 类型: {fieldValue.GetType().FullName}");

                        // 尝试手动枚举
                        // 首先检查它是否实现了 IEnumerable 接口
                        if (fieldValue is System.Collections.IEnumerable enumerable)
                        {
                            foreach (object item in enumerable)
                            {
                                if (item == null)
                                    continue;

                                // 不进行类型转换，而是使用反射获取所需的属性
                                Type itemType = item.GetType();

                                // 尝试获取 targetState 属性/字段
                                object targetStateObj = null;
                                PropertyInfo targetStateProperty = itemType.GetProperty("targetState");
                                if (targetStateProperty != null)
                                {
                                    targetStateObj = targetStateProperty.GetValue(item);
                                }
                                else
                                {
                                    FieldInfo targetStateField = itemType.GetField("targetState");
                                    if (targetStateField != null)
                                    {
                                        targetStateObj = targetStateField.GetValue(item);
                                    }
                                }

                                // 检查 targetState 是否有效
                                if (targetStateObj == null)
                                    continue;

                                // 获取 stateName 属性
                                PropertyInfo stateNameProperty = targetStateObj.GetType().GetProperty("stateName");
                                if (stateNameProperty == null)
                                    continue;

                                object stateNameObj = stateNameProperty.GetValue(targetStateObj);
                                if (stateNameObj == null)
                                    continue;

                                // 获取 conditionSign 属性/字段
                                string conditionSign = null;
                                PropertyInfo conditionSignProperty = itemType.GetProperty("conditionSign");
                                if (conditionSignProperty != null)
                                {
                                    conditionSign = conditionSignProperty.GetValue(item) as string;
                                }
                                else
                                {
                                    FieldInfo conditionSignField = itemType.GetField("conditionSign");
                                    if (conditionSignField != null)
                                    {
                                        conditionSign = conditionSignField.GetValue(item) as string;
                                    }
                                }

                                // 添加转换信息
                                transitions.Add(new TransitionInfo
                                {
                                    TargetState = stateNameObj.ToString(),
                                    ConditionSign = conditionSign
                                });
                            }
                        }

                        // 我们找到字段并处理了它，所以可以退出循环
                        break;
                    }

                    // 如果在当前类型中找不到字段，则检查基类
                    stateBaseType = stateBaseType.BaseType;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取状态转换时发生异常: {ex.Message}\n{ex.StackTrace}");
            }

            return transitions;
        }

        /// <summary>
        /// 获取全局状态转换信息
        /// </summary>
        public List<TransitionInfo> GetGlobalStateTransitions()
        {
            List<TransitionInfo> transitions = new List<TransitionInfo>();

            try
            {
                if (stateMachine == null)
                    return transitions;

                // 获取全局状态切换信息列表字段
                FieldInfo globalStateChangeInfosField = null;
                Type currentType = stateMachine.GetType();

                while (currentType != null && globalStateChangeInfosField == null)
                {
                    globalStateChangeInfosField = currentType.GetField("globalStateChangeInfos",
                                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (globalStateChangeInfosField == null)
                        currentType = currentType.BaseType;
                }

                if (globalStateChangeInfosField == null)
                    return transitions;

                // 获取字段值
                object globalStateChangeInfosObj = globalStateChangeInfosField.GetValue(stateMachine);
                if (globalStateChangeInfosObj == null)
                    return transitions;

                // 检查是否为可枚举类型
                if (!(globalStateChangeInfosObj is System.Collections.IEnumerable globalStateChangeInfos))
                    return transitions;

                // 遍历全局状态切换信息
                foreach (object info in globalStateChangeInfos)
                {
                    if (info == null) continue;

                    try
                    {
                        Type infoType = info.GetType();

                        // 尝试获取目标状态
                        object targetStateObj = null;
                        var targetStateProperty = infoType.GetProperty("targetState");
                        if (targetStateProperty != null)
                            targetStateObj = targetStateProperty.GetValue(info);

                        if (targetStateObj == null)
                        {
                            var targetStateField = infoType.GetField("targetState");
                            if (targetStateField != null)
                                targetStateObj = targetStateField.GetValue(info);
                        }

                        if (!(targetStateObj is StateBase<string> targetState))
                            continue;

                        // 尝试获取条件标识
                        string conditionSign = null;
                        var conditionSignProperty = infoType.GetProperty("conditionSign");
                        if (conditionSignProperty != null)
                            conditionSign = conditionSignProperty.GetValue(info) as string;
                        else
                        {
                            var conditionSignField = infoType.GetField("conditionSign");
                            if (conditionSignField != null)
                                conditionSign = conditionSignField.GetValue(info) as string;
                        }

                        // 添加到转换列表
                        if (targetState.stateName != null)
                        {
                            transitions.Add(new TransitionInfo
                            {
                                TargetState = targetState.stateName.ToString(),
                                ConditionSign = conditionSign,
                                IsGlobal = true
                            });
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略处理单个转换时的异常
                    }
                }
            }
            catch (Exception)
            {
                // 忽略整体异常
            }

            return transitions;
        }

        /// <summary>
        /// 获取指定名称的状态对象
        /// </summary>
        /// <param name="stateName">状态名称</param>
        /// <returns>状态对象</returns>
        public StateBase<string> GetState(string stateName)
        {
            if (stateMachine == null)
                return null;

            // 使用反射获取状态字典
            Type type = stateMachine.GetType();
            FieldInfo statesField = type.GetField("states",
                                               System.Reflection.BindingFlags.Instance |
                                               System.Reflection.BindingFlags.NonPublic);

            if (statesField == null)
                return null;

            object statesObj = statesField.GetValue(stateMachine);
            if (statesObj == null)
                return null;

            // 尝试获取字典的索引器
            Type dictType = statesObj.GetType();
            MethodInfo getItemMethod = dictType.GetMethod("get_Item");
            if (getItemMethod == null)
                return null;

            try
            {
                return getItemMethod.Invoke(statesObj, new object[] { stateName }) as StateBase<string>;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取状态的所有转换信息
        /// </summary>
        public List<TransitionInfo> GetStateTransitions(string stateName)
        {
            List<TransitionInfo> transitions = new List<TransitionInfo>();

            try
            {
                // 使用反射获取状态对象
                StateBase<string> state = null;

                // 获取stateMachine字段
                FieldInfo stateMachineField = GetType().GetField("stateMachine",
                                                             BindingFlags.Instance |
                                                             BindingFlags.NonPublic);
                if (stateMachineField == null)
                    return transitions;

                StateMachine<string> mainMachine = stateMachineField.GetValue(this) as StateMachine<string>;
                if (mainMachine == null)
                    return transitions;

                // 获取状态字典
                FieldInfo statesField = mainMachine.GetType().GetField("states",
                                                                   BindingFlags.Instance |
                                                                   BindingFlags.NonPublic);
                if (statesField == null)
                    return transitions;

                object statesDict = statesField.GetValue(mainMachine);
                if (statesDict == null)
                    return transitions;

                // 获取状态
                Type dictType = statesDict.GetType();
                MethodInfo getItemMethod = dictType.GetMethod("get_Item");
                if (getItemMethod != null)
                {
                    try
                    {
                        state = getItemMethod.Invoke(statesDict, new object[] { stateName }) as StateBase<string>;
                    }
                    catch
                    {
                        // 忽略错误
                        return transitions;
                    }
                }

                if (state == null)
                    return transitions;

                // 获取stateChangeInfos字段
                FieldInfo stateChangeInfosField = null;
                Type currentType = state.GetType();

                // 向上查找基类，直到找到字段
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

                // 获取转换信息列表
                object infoListObj = stateChangeInfosField.GetValue(state);
                if (!(infoListObj is System.Collections.IEnumerable infoList))
                    return transitions;

                // 遍历转换信息
                foreach (object info in infoList)
                {
                    if (info == null)
                        continue;

                    // 获取目标状态
                    PropertyInfo targetStateProperty = info.GetType().GetProperty("targetState");
                    if (targetStateProperty == null)
                        continue;

                    object targetStateObj = targetStateProperty.GetValue(info);
                    if (!(targetStateObj is StateBase<string> targetState))
                        continue;

                    // 获取条件标识
                    string conditionSign = null;
                    PropertyInfo conditionSignProperty = info.GetType().GetProperty("conditionSign");
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
                Debug.LogError($"获取状态转换时出错: {ex.Message}");
            }

            return transitions;
        }


        /// <summary>
        /// 获取状态机中StatesDic字段
        /// </summary>
        private FieldInfo GetStatesDictField()
        {
            if (stateMachine == null) return null;

            Type currentType = stateMachine.GetType();
            FieldInfo field = null;

            while (currentType != null && field == null)
            {
                field = currentType.GetField("statesDic",
                       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field == null)
                    currentType = currentType.BaseType;
            }

            return field;
        }

        /// <summary>
        /// 获取状态名称的实际类型
        /// </summary>
        private Type GetStateNameType()
        {
            Type type = stateMachine?.GetType();
            if (type == null) return typeof(string);

            if (type.IsGenericType)
            {
                return type.GetGenericArguments()[0];
            }

            return typeof(string);
        }
    }

    /// <summary>
    /// 状态转换信息(用于调试UI显示)
    /// </summary>
    [Serializable]
    public class TransitionInfo
    {
        public string TargetState { get; set; }
        public string ConditionSign { get; set; }
        public bool IsGlobal { get; set; }
    }
}