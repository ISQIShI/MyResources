using System;
using System.Collections.Generic;
using System.Linq;

namespace StateMachineFrame
{
    public class StateMachine<TStateName> : VariableState<TStateName>
    {
        /// <summary>
        /// 空的状态字典,作为默认值
        /// </summary>
        private static readonly Dictionary<TStateName, StateBase<TStateName>> emptyStatesDic = new Dictionary<TStateName, StateBase<TStateName>>(0);

        /// <summary>
        /// 存储状态机拥有的状态的字典
        /// </summary>
        private Dictionary<TStateName, StateBase<TStateName>> statesDic = emptyStatesDic;

        /// <summary>
        /// 全局状态切换信息
        /// </summary>
        private List<StateChangeInfo<TStateName>> globalStateChangeInfos = emptyStateChangeInfos;

        /// <summary>
        /// 初始默认状态
        /// </summary>
        private StateBase<TStateName> defaultState = default;

        /// <summary>
        /// 当前状态
        /// </summary>
        private StateBase<TStateName> currentState = default;

#nullable enable
        /// <summary>
        /// 下一个状态名
        /// </summary>
        private TStateName? nextStateName = default;
#nullable restore

        /// <summary>
        /// 是否记录最后的状态
        /// </summary>
        private bool recordLastState = false;

        /// <summary>
        /// 是否正在工作
        /// </summary>
        public bool IsWorking { get; private set; } = false;

        /// <summary>
        /// 构造方法
        /// </summary>
        public StateMachine(Action onInit = null, Action onEnter = null, Action onUpdate = null, Action onExit = null) : base(onInit, onEnter, onUpdate, onExit) { }
        /// <summary>
        /// 有参构造方法
        /// </summary>
        /// <param name="recordLastState">是否记录最后的状态</param>
        public StateMachine(bool recordLastState, Action onInit = null, Action onEnter = null, Action onUpdate = null, Action onExit = null) : base(onInit, onEnter, onUpdate, onExit)
        {
            this.recordLastState = recordLastState;
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void LaunchStateMachine(TStateName defaultStateName)
        {
            if (stateMachine != null) StateMachineException.HandleException(new StateMachineException("StateMachine-LaunchStateMachine: 当前状态机为子状态机,无法启动"));
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-LaunchStateMachine: 状态机中没有状态,无法启动"));
            SetDefaultState(defaultStateName);
            currentState = defaultState;
            OnInit();
        }

        /// <summary>
        /// 运行状态机
        /// </summary>
        public void UpdateStateMachine()
        {
            if (stateMachine != null) StateMachineException.HandleException(new StateMachineException("StateMachine-UpdateStateMachine: 当前状态机为子状态机,无法运行"));
            OnUpdate();
        }

        public void SwitchStateMachineOperation(bool isWorking)
        {
            IsWorking = isWorking;
        }


        #region 对状态机中状态的操作
        /// <summary>
        /// 向状态机中添加状态
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="state"></param>
        public StateMachine<TStateName> AddState(TStateName stateName, StateBase<TStateName> state)
        {
            if (stateName == null || state == null)
            {
                StateMachineException.HandleException(new StateMachineException("StateMachine-AddState: stateName 或 state 为 null"));
            }
            if (statesDic == emptyStatesDic)
            {
                statesDic = new Dictionary<TStateName, StateBase<TStateName>>();
            }
            if (statesDic.TryGetValue(stateName, out StateBase<TStateName> valueState))
            {
                if (valueState is TempPlaceholderState<TStateName>)
                {
                    MergeStateChangeInfos(state, valueState, false);
                    statesDic[stateName] = state;
                    SetStateName(state, stateName);
                    SetStateMachine(state, this);
                    InvokeOnInit(state);
                    return this;
                }
                else StateMachineException.HandleException(new StateMachineException($"StateMachine-AddState: 状态{stateName}已经存在"));

            }
            statesDic.Add(stateName, state);
            //如果添加状态后状态机中只有一个状态,则默认状态和当前状态都为该状态
            if (statesDic.Count == 1)
            {
                defaultState = state;
                currentState = state;
            }
            //添加状态成功,初始化状态
            SetStateName(state, stateName);
            SetStateMachine(state, this);
            InvokeOnInit(state);
            return this;
        }

        /// <summary>
        /// 从状态机中移除状态
        /// </summary>
        /// <param name="stateName"></param>
        public void RemoveState(TStateName stateName)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveState: 状态机中没有状态,无法移除"));
            if (statesDic.TryGetValue(stateName, out StateBase<TStateName> stateToBeRemoved))
            {
                //销毁目标状态
                DestroyState(stateToBeRemoved);
                stateToBeRemoved = null;
                statesDic.Remove(stateName);
                //删除所有状态向该状态的状态切换信息
                ClearStateChangeInfos(stateName, false, true);
                //如果移除的是默认状态,则将默认状态设置为字典中第一个状态
                if (defaultState.stateName.Equals(stateName))
                {
                    defaultState = statesDic.ElementAtOrDefault(0).Value;
                }
                //如果移除的是当前状态,则切换为默认状态
                if (currentState.stateName.Equals(stateName))
                {
                    ChangeState(defaultState.stateName);
                }
                if (statesDic.Count == 0)
                {
                    statesDic = emptyStatesDic;
                }
                return;
            }
            StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveState:目标状态{stateName}不存在"));
        }

        /// <summary>
        /// 移除状态机中所有状态
        /// </summary>
        public void RemoveAllStates()
        {
            if (statesDic == emptyStatesDic) return;
            defaultState = default;
            ChangeState(defaultState.stateName);
            foreach (var state in statesDic.Values)
            {
                DestroyState(state);
            }
            statesDic = emptyStatesDic;
        }

        /// <summary>
        /// 设置状态机的默认状态
        /// </summary>
        /// <param name="stateName"></param>
        public void SetDefaultState(TStateName stateName)
        {
            if (statesDic == emptyStatesDic) return;
            if (statesDic.TryGetValue(stateName, out StateBase<TStateName> state))
            {
                defaultState = state;
            }
            else
            {
                StateMachineException.HandleException(new StateMachineException($"StateMachine-SetDefaultState: 状态{stateName}不存在,无法设置默认状态"));
            }
        }

        /// <summary>
        /// 设置是否记录最后的状态
        /// </summary>
        /// <param name="willRecord"></param>
        public void SetRecordLastState(bool willRecord)
        {
            recordLastState = willRecord;
        }
        #endregion

        #region 对状态机中状态切换信息的操作
        /// <summary>
        /// 添加两个状态之间的切换信息
        /// </summary>
        /// <param name="originName"></param>
        /// <param name="targetName"></param>
        /// <param name="changeCondition"></param>
        /// <param name="conditionSign"></param>
        public StateMachine<TStateName> AddStateChangeInfo(TStateName originName, TStateName targetName, Func<bool> changeCondition, bool autoAddLackingState, string conditionSign = null)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-AddStateChangeInfo: 状态机中没有状态,无法添加状态切换信息"));
            if (originName.Equals(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-AddStateChangeInfo: 原始状态和目标状态不能相同,为{originName}"));
            if (!statesDic.ContainsKey(originName))
            {
                if (autoAddLackingState) AddState(originName, new TempPlaceholderState<TStateName>());
                else StateMachineException.HandleException(new StateMachineException($"StateMachine-AddStateChangeInfo: 原始状态{originName}不存在"));
            }
            if (statesDic.TryGetValue(originName, out StateBase<TStateName> originState))
            {
                originState.AddStateChangeInfo(targetName, changeCondition, autoAddLackingState, conditionSign);
            }
            return this;
        }
        /// <summary>
        /// 移除两个状态之间的指定切换信息
        /// </summary>
        /// <param name="originName"></param>
        /// <param name="targetName"></param>
        /// <param name="conditionSign"></param>
        public void RemoveStateChangeInfo(TStateName originName, TStateName targetName, string conditionSign = null)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveStateChangeInfo: 状态机中没有状态,无法移除状态切换信息"));
            if (originName.Equals(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveStateChangeInfo: 原始状态和目标状态不能相同,为{originName}"));
            if (!statesDic.ContainsKey(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveStateChangeInfo: 目标状态{targetName}不存在"));
            if (statesDic.TryGetValue(originName, out StateBase<TStateName> originState))
            {
                originState.RemoveStateChangeInfo(targetName, conditionSign);
                return;
            }
            StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveStateChangeInfo: 原始状态{originName}不存在"));
        }
        /// <summary>
        /// 移除两个状态之间的所有切换信息
        /// </summary>
        /// <param name="originName"></param>
        /// <param name="targetName"></param>
        public void RemoveWholeStateChangeInfo(TStateName originName, TStateName targetName)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveWholeStateChangeInfo: 状态机中没有状态,无法移除状态切换信息"));
            if (originName.Equals(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveWholeStateChangeInfo: 原始状态和目标状态不能相同,为{originName}"));
            if (!statesDic.ContainsKey(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveWholeStateChangeInfo: 目标状态{targetName}不存在"));
            if (statesDic.TryGetValue(originName, out StateBase<TStateName> originState))
            {
                originState.RemoveWholeStateChangeInfo(targetName);
                return;
            }
            StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveWholeStateChangeInfo: 原始状态{originName}不存在"));
        }
        /// <summary>
        /// 清空某个状态的所有切换信息
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="clearInfosAsOrigin">清空该状态作为起始状态的状态切换信息</param>
        /// <param name="clearInfosAsTarget">清空该状态作为目标状态的状态切换信息</param>
        public void ClearStateChangeInfos(TStateName stateName, bool clearInfosAsOrigin = true, bool clearInfosAsTarget = true)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-ClearStateChangeInfos: 状态机中没有状态,无法清空对应状态的所有切换信息"));
            if (statesDic.TryGetValue(stateName, out StateBase<TStateName> targetState))
            {
                if (clearInfosAsOrigin) targetState.ClearStateChangeInfos();
                if (clearInfosAsTarget)
                {
                    //删除所有状态向该状态的状态切换信息
                    foreach (var state in statesDic.Values)
                    {
                        state.RemoveWholeStateChangeInfo(stateName);
                    }
                }
                return;
            }
            StateMachineException.HandleException(new StateMachineException($"StateMachine-ClearStateChangeInfos: 目标状态{stateName}不存在"));
        }
        /// <summary>
        /// 清空所有状态的所有切换信息
        /// </summary>
        public void ClearAllStateChangeInfos()
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-ClearAllStateChangeInfos: 状态机中没有状态,无法清空所有状态的所有切换信息"));
            foreach (var state in statesDic.Values)
            {
                state.ClearStateChangeInfos();
            }
        }
        /// <summary>
        /// 添加全局状态切换信息
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="changeCondition"></param>
        /// <param name="conditionSign"></param>
        public StateMachine<TStateName> AddGlobalStateChangeInfo(TStateName targetName, Func<bool> changeCondition, bool autoAddLackingState, string conditionSign = null)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-AddGlobalStateChangeInfo: 状态机中没有状态,无法添加全局状态切换信息"));
            if (!statesDic.ContainsKey(targetName))
            {
                if (autoAddLackingState) AddState(targetName, new TempPlaceholderState<TStateName>());
                else StateMachineException.HandleException(new StateMachineException($"StateMachine-AddGlobalStateChangeInfo: 目标状态{targetName}不存在"));
            }
            if (globalStateChangeInfos == emptyStateChangeInfos)
            {
                globalStateChangeInfos = new List<StateChangeInfo<TStateName>>();
            }
            //如果已经存在相同的全局状态切换信息,则只添加条件
            var temp = globalStateChangeInfos.Find((info) => (info.targetStateName.Equals(targetName)) && (info.conditionSign == conditionSign));
            if (temp != null)
            {
                temp.changeCondition += changeCondition;
                return this;
            }

            //否则添加新的全局状态切换信息
            globalStateChangeInfos.Add(new StateChangeInfo<TStateName>(null, targetName, changeCondition, conditionSign));
            return this;
        }
        /// <summary>
        /// 移除向目标状态的指定全局状态切换信息
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="conditionSign"></param>
        public void RemoveGlobalStateChangeInfo(TStateName targetName, string conditionSign = null)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveGlobalStateChangeInfo: 状态机中没有状态,无法移除全局状态切换信息"));
            if (!statesDic.ContainsKey(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveGlobalStateChangeInfo: 目标状态{targetName}不存在"));
            if (globalStateChangeInfos == emptyStateChangeInfos) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveGlobalStateChangeInfo: 全局状态切换信息为空"));
            var temp = globalStateChangeInfos.RemoveAll((info) => (info.targetStateName.Equals(targetName)) && (info.conditionSign == conditionSign));
            if (temp == 0) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveGlobalStateChangeInfo: 未找到指定的全局状态切换信息"));
        }

        /// <summary>
        /// 移除向目标状态的所有全局状态切换信息
        /// </summary>
        /// <param name="targetName"></param>
        public void RemoveWholeGlobalStateChangeInfo(TStateName targetName)
        {
            if (statesDic == emptyStatesDic) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveWholeGlobalStateChangeInfo: 状态机中没有状态,无法移除全局状态切换信息"));
            if (!statesDic.ContainsKey(targetName)) StateMachineException.HandleException(new StateMachineException($"StateMachine-RemoveWholeGlobalStateChangeInfo: 目标状态{targetName}不存在"));
            if (globalStateChangeInfos == emptyStateChangeInfos) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveWholeGlobalStateChangeInfo: 全局状态切换信息为空"));
            var temp = globalStateChangeInfos.RemoveAll((info) => info.targetStateName.Equals(targetName));
            if (temp == 0) StateMachineException.HandleException(new StateMachineException("StateMachine-RemoveWholeGlobalStateChangeInfo: 未找到指定的全局状态切换信息"));
        }
        /// <summary>
        /// 清空全局状态切换信息
        /// </summary>
        public void ClearGlobalStateChangeInfos()
        {
            globalStateChangeInfos = emptyStateChangeInfos;
        }
        #endregion

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="stateName"></param>
        public void ChangeState(TStateName stateName)
        {
            if (statesDic == emptyStatesDic) return;
            if (stateName == null && statesDic.Count != 0)
            {
                StateMachineException.HandleException(new StateMachineException("StateMachine-ChangeState: stateName 为 null"));
            }
            if (statesDic.TryGetValue(stateName, out StateBase<TStateName> state) || stateName == null)
            {
                //退出当前状态
                InvokeOnExit(currentState);
                //切换为目标状态
                currentState = state;
                //进入目标状态
                InvokeOnEnter(currentState);
            }
            else
            {
                currentState.TryRemoveWholeStateChangeInfo(stateName);
                StateMachineException.HandleException(new StateMachineException($"StateMachine-ChangeState: 目标状态{stateName}不存在"));
            }
        }

        /// <summary>
        /// 检查状态机中是否存在对应状态
        /// </summary>
        /// <param name="stateName"></param>
        /// <returns></returns>
        public bool ContainsState(TStateName stateName)
        {
            return statesDic.ContainsKey(stateName);
        }

        /// <summary>
        /// 获取状态机中对应状态
        /// </summary>
        /// <param name="stateName"></param>
        /// <returns></returns>
        public StateBase<TStateName> GetState(TStateName stateName)
        {
            if (statesDic.TryGetValue(stateName, out StateBase<TStateName> state))
            {
                return state;
            }
            return null;
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        /// <returns></returns>
        public StateBase<TStateName> GetCurrentState()
        {
            return currentState;
        }

        protected override void OnInit()
        {
            base.OnInit();
            if (stateMachine != null) return;
            OnEnter();
        }
        protected override void OnEnter()
        {
            base.OnEnter();
            IsWorking = true;
            if (!recordLastState) currentState = defaultState;
            InvokeOnEnter(currentState);
        }
        protected override void OnUpdate()
        {
            if (!IsWorking) return;
            base.OnUpdate();
            foreach (var globalStateChangeInfo in globalStateChangeInfos)
            {
                foreach (System.Func<bool> condition in globalStateChangeInfo.changeCondition.GetInvocationList())
                {
                    if (condition.Invoke())
                    {
                        ChangeState(globalStateChangeInfo.targetStateName);
                        return;
                    }
                }
            }
#nullable enable
            nextStateName = (TStateName?)currentState.CheckStateChange();
#nullable restore
            if (nextStateName != null)
            {
                ChangeState(nextStateName);
                return;
            }
            InvokeOnUpdate(currentState);
        }

        protected override void OnExit()
        {
            InvokeOnExit(currentState);
            base.OnExit();
            IsWorking = false;
        }

    }

    public class StateMachine : StateMachine<string>
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public StateMachine(Action onInit = null, Action onEnter = null, Action onUpdate = null, Action onExit = null) : base(onInit, onEnter, onUpdate, onExit) { }
        /// <summary>
        /// 有参构造方法
        /// </summary>
        /// <param name="recordLastState">是否记录最后的状态</param>
        public StateMachine(bool recordLastState, Action onInit = null, Action onEnter = null, Action onUpdate = null, Action onExit = null) : base(onInit, onEnter, onUpdate, onExit) { }
    }
}
