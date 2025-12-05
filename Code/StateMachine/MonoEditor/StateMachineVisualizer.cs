using UnityEngine;

namespace StateMachineFrame
{
    /// <summary>
    /// 提供运行时状态机可视化功能的组件
    /// 可以显示状态机的当前状态和可用转换
    /// </summary>
    [AddComponentMenu("StateMachine<string> Framework/状态机可视化")]
    [ExecuteInEditMode]
    public class StateMachineVisualizer : MonoBehaviour
    {
        [SerializeField]
        private StateMachine<string> stateMachine;

        [Header("可视化设置")]
        [SerializeField]
        private bool visualizeInGameView = true;

        [SerializeField]
        private bool showTransitions = true;

        [SerializeField]
        private Color backgroundColor = new Color(0, 0, 0, 0.7f);

        [SerializeField]
        private Color textColor = Color.white;

        [SerializeField]
        private Color currentStateColor = Color.green;

        [SerializeField]
        private Vector2 position = new Vector2(10, 10);

        [SerializeField]
        private float width = 250;

        [SerializeField]
        private int fontSize = 14;

        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private bool stylesInitialized = false;
        private Texture2D backgroundTexture;

        private void OnGUI()
        {
            // 只在游戏运行时且开启了可视化时显示
            if (!visualizeInGameView || stateMachine == null || !Application.isPlaying)
                return;

            // 延迟初始化样式，确保在OnGUI上下文中
            if (!stylesInitialized)
            {
                InitStyles();
            }

            DrawVisualization();
        }

        private void InitStyles()
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = textColor;
            labelStyle.fontSize = fontSize;

            headerStyle = new GUIStyle(labelStyle);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = fontSize + 2;

            boxStyle = new GUIStyle(GUI.skin.box);
            backgroundTexture = MakeTexture(2, 2, backgroundColor);
            boxStyle.normal.background = backgroundTexture;
            boxStyle.normal.textColor = textColor;
            boxStyle.fontSize = fontSize;
            boxStyle.padding = new RectOffset(10, 10, 10, 10);

            stylesInitialized = true;
        }

        private void OnDisable()
        {
            // 清理纹理资源
            if (backgroundTexture != null)
            {
                DestroyImmediate(backgroundTexture);
                backgroundTexture = null;
            }
            stylesInitialized = false;
        }

        private void DrawVisualization()
        {
            StateBase<string> currentState = stateMachine.GetCurrentState();
            if (currentState == null)
                return;

            // 计算所需的高度
            float height = 80; // 基础高度

            // 添加所有转换的高度
            if (showTransitions)
            {
                // 使用反射获取转换信息
                try
                {
                    var stateChangeInfosField = currentState.GetType().BaseType.GetField("stateChangeInfos",
                                              System.Reflection.BindingFlags.Instance |
                                              System.Reflection.BindingFlags.NonPublic);

                    if (stateChangeInfosField != null)
                    {
                        var stateChangeInfos = stateChangeInfosField.GetValue(currentState) as System.Collections.ICollection;
                        if (stateChangeInfos != null)
                        {
                            height += stateChangeInfos.Count * 30 + 20;
                        }
                    }
                }
                catch (System.Exception)
                {
                    // 忽略异常，使用默认高度
                }
            }

            // 绘制背景框
            GUI.Box(new Rect(position.x, position.y, width, height), "", boxStyle);

            // 绘制状态机名称
            float y = position.y + 10;
            GUI.Label(new Rect(position.x + 10, y, width - 20, 25), "状态机", headerStyle);
            y += 25;

            // 绘制当前状态信息
            GUI.contentColor = currentStateColor;
            GUI.Label(new Rect(position.x + 10, y, width - 20, 20), "当前状态: " + currentState.stateName, labelStyle);
            y += 20;

            GUI.contentColor = textColor;
            GUI.Label(new Rect(position.x + 10, y, width - 20, 20), "类型: " + currentState.GetType().Name, labelStyle);
            y += 20;

            // 绘制转换信息
            if (showTransitions)
            {
                y += 10;
                GUI.Label(new Rect(position.x + 10, y, width - 20, 20), "可用转换:", headerStyle);
                y += 25;

                // 使用反射获取当前状态的转换
                try
                {
                    var stateChangeInfosField = currentState.GetType().BaseType.GetField("stateChangeInfos",
                                              System.Reflection.BindingFlags.Instance |
                                              System.Reflection.BindingFlags.NonPublic);

                    if (stateChangeInfosField != null)
                    {
                        var stateChangeInfos = stateChangeInfosField.GetValue(currentState) as System.Collections.IEnumerable;
                        if (stateChangeInfos != null)
                        {
                            foreach (var info in stateChangeInfos)
                            {
                                if (info == null) continue;

                                try
                                {
                                    // 获取目标状态和条件
                                    var infoType = info.GetType();
                                    var targetStateProperty = infoType.GetProperty("targetState");
                                    var conditionSignProperty = infoType.GetField("conditionSign");

                                    if (targetStateProperty != null)
                                    {
                                        var targetState = targetStateProperty.GetValue(info) as StateBase<string>;
                                        string conditionSign = null;

                                        if (conditionSignProperty != null)
                                        {
                                            conditionSign = conditionSignProperty.GetValue(info) as string;
                                        }

                                        if (targetState != null && targetState.stateName != null)
                                        {
                                            string transitionText = "→ " + targetState.stateName;
                                            if (!string.IsNullOrEmpty(conditionSign))
                                            {
                                                transitionText += " [" + conditionSign + "]";
                                            }

                                            GUI.Label(new Rect(position.x + 20, y, width - 30, 20), transitionText, labelStyle);
                                            y += 20;
                                        }
                                    }
                                }
                                catch (System.Exception)
                                {
                                    // 忽略单个转换的异常
                                    continue;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    GUI.Label(new Rect(position.x + 20, y, width - 30, 20), "无法获取转换信息", labelStyle);
                }
            }
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 设置要可视化的状态机
        /// </summary>
        public void SetStateMachine(StateMachine<string> machine)
        {
            stateMachine = machine;
        }

        /// <summary>
        /// 切换可视化显示
        /// </summary>
        public void ToggleVisibility()
        {
            visualizeInGameView = !visualizeInGameView;
        }

        /// <summary>
        /// 修改背景颜色
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            stylesInitialized = false; // 强制重新创建样式
        }

        /// <summary>
        /// 修改字体大小
        /// </summary>
        public void SetFontSize(int size)
        {
            fontSize = size;
            stylesInitialized = false; // 强制重新创建样式
        }
    }
}