using System;
namespace StateMachineFrame
{
    /// <summary>
    /// 状态切换信息
    /// </summary>
    public class StateChangeInfo<TStateName>
    {

        /// <summary>
        /// 原始状态
        /// </summary>
        public StateBase<TStateName> originState;

        /// <summary>
        /// 目标状态
        /// </summary>
        public StateBase<TStateName> targetState;
        /// <summary>
        /// 切换条件
        /// </summary>
        public Func<bool> changeCondition;
        /// <summary>
        /// 切换条件的标识符
        /// </summary>
        public string conditionSign;

        public StateChangeInfo(StateBase<TStateName> originState, StateBase<TStateName> targetState, Func<bool> changeCondition, string conditionSign)
        {
            this.originState = originState;
            this.targetState = targetState;
            this.changeCondition = changeCondition;
            this.conditionSign = conditionSign;
        }
    }

}
