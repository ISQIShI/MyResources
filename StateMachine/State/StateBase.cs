using System;
using System.Collections.Generic;

namespace StateMachineFrame
{

    public abstract class StateBase<TStateName>
    {
        /// <summary>
        /// 当前状态名
        /// </summary>
        public TStateName stateName { get; protected set; } = default;

        /// <summary>
        /// 为子类提供设置状态名的方法
        /// </summary>
        /// <param name="state"></param>
        /// <param name="name"></param>
        protected void SetStateName(StateBase<TStateName> state, TStateName name)
        {
            if (state == null) return;
            state.stateName = name;
        }

        /// <summary>
        /// 当前状态所属状态机
        /// </summary>
        public StateMachine<TStateName> stateMachine { get; protected set; } = default;

        /// <summary>
        /// 为子类提供设置状态机的方法
        /// </summary>
        /// <param name="state"></param>
        /// <param name="machine"></param>
        protected void SetStateMachine(StateBase<TStateName> state, StateMachine<TStateName> machine)
        {
            if (state == null) return;
            state.stateMachine = machine;
        }

        /// <summary>
        /// 获取当前状态的附加信息
        /// </summary>
        /// <remarks>子类可重写该方法,修改返回值</remarks>
        /// <returns>"StateBase&lt;类型名&gt;"</returns>
        public virtual string GetStateAdditionalInformationAsString()
        {
            return $"StateBase<{typeof(TStateName).Name}>";
        }


        /// <summary>
        /// 为子类(特指状态机)提供销毁状态的方法
        /// </summary>
        /// <param name="state"></param>
        protected void DestroyState(StateBase<TStateName> state)
        {
            if (state == null) return;
            state.ClearStateChangeInfos();
            state.stateMachine = default;
            state.stateName = default;
        }

        /// <summary>
        /// 空的状态切换信息,作为默认值
        /// </summary>
        protected static readonly List<StateChangeInfo<TStateName>> emptyStateChangeInfos = new List<StateChangeInfo<TStateName>>(0);

        /// <summary>
        /// 存储状态切换信息
        /// </summary>
        protected List<StateChangeInfo<TStateName>> stateChangeInfos = emptyStateChangeInfos;


        protected const int DEFAULT_CAPACITY_StateChangeInfosList = 4;

        /// <summary>
        /// 添加状态切换信息
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="changeCondition"></param>
        /// <param name="conditionSign"></param>
        public StateBase<TStateName> AddStateChangeInfo(TStateName targetName, Func<bool> changeCondition, bool autoAddLackingState, string conditionSign = null)
        {
            //检查状态机是否存在 以及 目标状态是否为当前状态
            if (stateMachine == null || targetName.Equals(stateName))
            {
                StateMachineException.HandleException(new StateMachineException($"StateBase-AddStateChangeInfo: 状态机不存在 或 目标状态:{targetName} 为当前状态"));
            }
            //检查目标状态是否存在
            if (!stateMachine.ContainsState(targetName))
            {
                if (autoAddLackingState) stateMachine.AddState(targetName, new TempPlaceholderState<TStateName>());
                else StateMachineException.HandleException(new StateMachineException($"StateBase-AddStateChangeInfo: 目标状态:{targetName} 不存在"));
            }
            if (stateChangeInfos == emptyStateChangeInfos)
            {
                stateChangeInfos = new List<StateChangeInfo<TStateName>>(DEFAULT_CAPACITY_StateChangeInfosList);
            }
            //如果已经存在相同的状态切换信息,则只添加条件
            var temp = stateChangeInfos.Find((info) => (info.targetState.Equals(targetName)) && (info.conditionSign == conditionSign));
            if (temp != null)
            {
                temp.changeCondition += changeCondition;
                return this;
            }
            //否则添加新的状态切换信息
            stateChangeInfos.Add(new StateChangeInfo<TStateName>(this, stateMachine.GetState(targetName), changeCondition, conditionSign));
            return this;
        }


        /// <summary>
        /// 移除当前状态向目标状态的指定切换信息
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="conditionSign"></param>
        public void RemoveStateChangeInfo(TStateName targetName, string conditionSign = null)
        {
            if (stateChangeInfos == emptyStateChangeInfos) StateMachineException.HandleException(new StateMachineException("StateBase-RemoveStateChangeInfo: 状态切换信息为空"));
            var temp = stateChangeInfos.RemoveAll((info) => (info.targetState.Equals(targetName)) && (info.conditionSign == conditionSign));
            if (temp == 0) StateMachineException.HandleException(new StateMachineException("StateBase-RemoveStateChangeInfo: 未找到指定的状态切换信息"));
        }


        /// <summary>
        /// 移除当前状态向目标状态的所有切换信息
        /// </summary>
        /// <param name="targetName"></param>
        public void RemoveWholeStateChangeInfo(TStateName targetName)
        {
            if (stateChangeInfos == emptyStateChangeInfos) StateMachineException.HandleException(new StateMachineException("StateBase-RemoveWholeStateChangeInfo: 状态切换信息为空"));
            var temp = stateChangeInfos.RemoveAll((info) => info.targetState.Equals(targetName));
            if (temp == 0) StateMachineException.HandleException(new StateMachineException("StateBase-RemoveWholeStateChangeInfo: 未找到指定的状态切换信息"));
        }

        /// <summary>
        /// 尝试移除当前状态向目标状态的所有切换信息
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public bool TryRemoveWholeStateChangeInfo(TStateName targetName)
        {
            if (stateChangeInfos == emptyStateChangeInfos) return false;
            var temp = stateChangeInfos.RemoveAll((info) => info.targetState.Equals(targetName));
            if (temp == 0) return false;
            return true;
        }

        /// <summary>
        /// 清空状态切换信息
        /// </summary>
        public void ClearStateChangeInfos()
        {
            stateChangeInfos = emptyStateChangeInfos;
        }

        /// <summary>
        /// 将状态B的状态切换信息添加到状态A中状态切换信息列表的末尾或开头
        /// </summary>
        /// <param name="stateReceiver"></param>
        /// <param name="stateProvider"></param>
        /// <param name="addToTheEnd"></param>
        protected void MergeStateChangeInfos(StateBase<TStateName> stateReceiver, StateBase<TStateName> stateProvider, bool addToTheEnd)
        {
            if (stateReceiver == null || stateProvider == null) return;
            if (addToTheEnd) stateReceiver.stateChangeInfos.AddRange(stateProvider.stateChangeInfos);
            else stateReceiver.stateChangeInfos.InsertRange(0, stateProvider.stateChangeInfos);
        }

        /// <summary>
        /// 检查是否可以切换状态
        /// </summary>
        /// <returns></returns>
        public bool CheckStateChange(out StateBase<TStateName> state)
        {
            foreach (var info in stateChangeInfos)
            {
                foreach (Func<bool> condition in info.changeCondition.GetInvocationList())
                {
                    if (condition.Invoke())
                    {
                        state = info.targetState;
                        return true;
                    }
                }
            }
            state = null;
            return false;
        }

        /// <summary>
        /// 初始化状态,仅在添加该状态时调用一次
        /// </summary>
        protected abstract void OnInit();
        protected void InvokeOnInit(StateBase<TStateName> state) { state?.OnInit(); }

        /// <summary>
        /// 进入状态时调用一次
        /// </summary>
        protected abstract void OnEnter();
        protected void InvokeOnEnter(StateBase<TStateName> state) { state?.OnEnter(); }

        /// <summary>
        /// 状态更新,每帧调用
        /// </summary>
        protected abstract void OnUpdate();
        protected void InvokeOnUpdate(StateBase<TStateName> state) { state?.OnUpdate(); }

        /// <summary>
        /// 离开状态时调用一次
        /// </summary>
        protected abstract void OnExit();
        protected void InvokeOnExit(StateBase<TStateName> state) { state?.OnExit(); }
    }
    public abstract class StateBase : StateBase<string>
    {

    }
}
