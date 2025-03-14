namespace StateMachineFrame
{
    public sealed class EmptyState<TStateName> : StateBase<TStateName>
    {

        /// <summary>
        /// 获取当前状态的附加信息
        /// </summary>
        /// <returns>"EmptyState"</returns>
        public override string GetStateAdditionalInformationAsString()
        {
            return "EmptyState";
        }
        protected override void OnEnter()
        {

        }

        protected override void OnExit()
        {

        }

        protected override void OnInit()
        {

        }

        protected override void OnUpdate()
        {

        }
    }
}