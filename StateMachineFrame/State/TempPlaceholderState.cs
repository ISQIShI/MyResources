namespace StateMachineFrame
{
    public sealed class TempPlaceholderState<TStateName> : StateBase<TStateName>
    {
        /// <summary>
        /// 获取当前状态的附加信息
        /// </summary>
        /// <returns>"TempPlaceholderState"</returns>
        public override string GetStateAdditionalInformationAsString()
        {
            return "TempPlaceholderState";
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
