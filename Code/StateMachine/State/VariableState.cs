using System;

namespace StateMachineFrame
{
    public class VariableState<TStateName> : StateBase<TStateName>
    {
        /// <summary>
        /// 获取当前状态的附加信息
        /// </summary>
        /// <remarks>子类可重写该方法,修改返回值</remarks>
        /// <returns>"VariableState&lt;类型名&gt;"</returns>
        public override string GetStateAdditionalInformationAsString()
        {
            return $"VariableState<{typeof(TStateName).Name}>";
        }

        protected Action onInit;
        protected Action onEnter;
        protected Action onUpdate;
        protected Action onExit;

        public VariableState<TStateName> SetOnInit(Action onInit, bool isAdditive = false)
        {
            if (isAdditive) this.onInit += onInit;
            else this.onInit = onInit;
            return this;
        }

        public VariableState<TStateName> SetOnEnter(Action onEnter, bool isAdditive = false)
        {
            if (isAdditive) this.onEnter += onEnter;
            else this.onEnter = onEnter;
            return this;
        }

        public VariableState<TStateName> SetOnUpdate(Action onUpdate, bool isAdditive = false)
        {
            if (isAdditive) this.onUpdate += onUpdate;
            else this.onUpdate = onUpdate;
            return this;
        }

        public VariableState<TStateName> SetOnExit(Action onExit, bool isAdditive = false)
        {
            if (isAdditive) this.onExit += onExit;
            else this.onExit = onExit;
            return this;
        }

        public VariableState(Action onInit = null, Action onEnter = null, Action onUpdate = null, Action onExit = null)
        {
            this.onInit = onInit;
            this.onEnter = onEnter;
            this.onUpdate = onUpdate;
            this.onExit = onExit;
        }
        protected override void OnInit()
        {
            onInit?.Invoke();
        }

        protected override void OnEnter()
        {
            onEnter?.Invoke();
        }

        protected override void OnUpdate()
        {
            onUpdate?.Invoke();
        }

        protected override void OnExit()
        {
            onExit?.Invoke();

        }

    }

    public class VariableState : VariableState<string>
    {
        public VariableState(Action onInit = null, Action onEnter = null, Action onUpdate = null, Action onExit = null) : base(onInit, onEnter, onUpdate, onExit)
        {
        }
    }
}
