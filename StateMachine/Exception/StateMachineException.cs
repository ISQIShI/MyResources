using System;

namespace StateMachineFrame
{
    public class StateMachineException : Exception
    {
        private static Action<StateMachineException, string> ExceptionOutputFunc;

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public StateMachineException() : base()
        {
        }
        /// <summary>
        /// 带消息的构造函数
        /// </summary>
        /// <param name="message"></param>
        public StateMachineException(string message) : base(message)
        {
        }
        /// <summary>
        /// 带消息和内部异常的构造函数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public StateMachineException(string message, Exception innerException) : base(message, innerException)
        {
        }
        /// <summary>
        /// 用于序列化的构造函数
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public StateMachineException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="stateMachineException"></param>
        public static void HandleException(StateMachineException stateMachineException, string additionalMessage = "StateMachineFrame")
        {
            ExceptionOutputFunc?.Invoke(stateMachineException, additionalMessage);
            throw stateMachineException;
        }

        public static void HandleMessage(StateMachineException stateMachineException, string additionalMessage = "StateMachineFrame")
        {
            ExceptionOutputFunc?.Invoke(stateMachineException, additionalMessage);
        }

        /// <summary>
        /// 注册异常输出函数
        /// </summary>
        /// <param name="exceptionOutputFunc"></param>
        public static void RegisterExceptionOutputFunc(Action<StateMachineException, string> exceptionOutputFunc)
        {
            ExceptionOutputFunc = exceptionOutputFunc;
        }
    }
}
