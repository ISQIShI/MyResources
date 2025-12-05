using UnityEditor;
using UnityEngine;

namespace StateMachineFrame.Editor
{
    public static class StateMachineMenu
    {
        [MenuItem("GameObject/状态机/添加状态机调试器", false, 10)]
        private static void AddStateMachineDebugger(MenuCommand menuCommand)
        {
            GameObject selectedObject = menuCommand.context as GameObject;
            if (selectedObject == null)
            {
                Debug.LogError("请先选择一个游戏对象");
                return;
            }

            // 检查是否已经有StateMachineDebugger组件
            StateMachineDebugger existingDebugger = selectedObject.GetComponent<StateMachineDebugger>();
            if (existingDebugger != null)
            {
                Debug.LogWarning($"游戏对象 {selectedObject.name} 已经有StateMachineDebugger组件");

                // 打开调试窗口并选择该调试器
                OpenDebugWindowWithDebugger(existingDebugger);
                return;
            }

            // 添加调试器组件
            StateMachineDebugger debugger = selectedObject.AddComponent<StateMachineDebugger>();
            debugger.name = "Default";

            // 打开调试窗口并选择该调试器
            OpenDebugWindowWithDebugger(debugger);

            Debug.Log($"已为游戏对象 {selectedObject.name} 添加StateMachineDebugger组件");
        }

        [MenuItem("GameObject/状态机/打开状态机调试器窗口", false, 11)]
        private static void OpenStateMachineDebuggerWindow()
        {
            // 直接打开调试窗口
            StateMachineDebugWindow.Init();
        }

        // 打开调试窗口并选择指定的调试器
        private static void OpenDebugWindowWithDebugger(StateMachineDebugger debugger)
        {
            // 打开调试窗口
            StateMachineDebugWindow window = EditorWindow.GetWindow<StateMachineDebugWindow>();
            window.titleContent = new GUIContent("状态机调试器");
            window.Show();

            // 延迟一帧后选择调试器
            EditorApplication.delayCall += () =>
            {
                window.SelectDebugger(debugger);
            };
        }
    }
}