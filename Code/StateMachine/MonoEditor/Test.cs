// 在包含StateMachineDebugger组件的GameObject上添加以下脚本来验证连接
using StateMachineFrame.Editor;
using UnityEngine;

public class DebuggerValidator : MonoBehaviour
{
    private StateMachineDebugger debugger;

    private void Start()
    {
        debugger = GetComponent<StateMachineDebugger>();
        if (debugger == null)
        {
            Debug.LogError("未找到StateMachineDebugger组件");
            return;
        }

        if (debugger.StateMachine == null)
        {
            Debug.LogError("StateMachineDebugger未连接到状态机");
            return;
        }

        Debug.Log($"状态机已连接: {debugger.StateMachine.GetType().Name}");
        Debug.Log($"当前状态: {(debugger.StateMachine.GetCurrentState() != null ? debugger.StateMachine.GetCurrentState().stateName : "无")}");
        Debug.Log($"状态数量: {debugger.States.Count}");
    }
}