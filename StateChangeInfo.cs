using System;
namespace StateMachineFrame
{
    /// <summary>
    /// 状态切换信息
    /// </summary>
    public class StateChangeInfo<TStateName>
    {
#nullable enable
        /// <summary>
        /// 原始状态名
        /// </summary>
        public TStateName? originStateName;
#nullable restore
        /// <summary>
        /// 目标状态名
        /// </summary>
        public TStateName targetStateName;
        /// <summary>
        /// 切换条件
        /// </summary>
        public Func<bool> changeCondition;
        /// <summary>
        /// 切换条件的标识符
        /// </summary>
        public string conditionSign;

        public StateChangeInfo(object originStateName, TStateName targetStateName, Func<bool> changeCondition, string conditionSign)
        {
#nullable enable
            this.originStateName = (TStateName?)originStateName;
#nullable restore
            this.targetStateName = targetStateName;
            this.changeCondition = changeCondition;
            this.conditionSign = conditionSign;
        }
    }

}
