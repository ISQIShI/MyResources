using System;
using System.Collections.Generic;

public class EventCenter
{
    #region 事件广播主体
    //此处代码可以不做修改直接使用
    private static Dictionary<GameEventType, Delegate> EventTable = new Dictionary<GameEventType, Delegate>();
    /// <summary>
    /// 添加监听（用于添加某种事件类型的委托）个人认为更像是检测
    /// </summary>
    /// <param name="_eventType"></param>
    /// <param name="_callBack"></param>
    /// <exception cref="Exception"></exception>
    private static void OnListenerAdding(GameEventType _eventType, Delegate _callBack)
    {
        if (!EventTable.ContainsKey(_eventType))
        {
            //如果传进来的方法的所属事件类型为空,就创建一个新的事件类型
            EventTable.Add(_eventType, null);
        }

        Delegate d = EventTable[_eventType];//创建临时委托，令其等于该事件类型的委托类型

        if (d != null && d.GetType() != _callBack.GetType())
        {
            //d当前所添加的事件类型不为空，并且新传入的委托类型与当前 事件类型 的 委托类型 不匹配
            throw new Exception(string.Format("尝试将事件{0}添加不同类型的委托，当前事件对应的委托是{1}，要添加的委托类型为{2}", _eventType, d.GetType(), _callBack.GetType()));
        }
    }
    /// <summary>
    /// 删除监听，个人认为认为更像检测
    /// </summary>
    /// <param name="_eventType"></param>
    /// <param name="_callBack"></param>
    /// <exception cref="Exception"></exception>
    private static void OnListenerRemoving(GameEventType _eventType, Delegate _callBack)
    {
        if (EventTable.ContainsKey(_eventType))
        {
            Delegate d = EventTable[_eventType];
            if (d == null)
            {
                //要移除的事件类型下没有委托
                throw new Exception(string.Format("移除监听错误：事件{0}没有对应的委托", _eventType));
            }
            else if (d.GetType() != _callBack.GetType())
            {
                //要移除的委托类型与当前事件类型下的委托类型不符
                throw new Exception(string.Format("移除监听错误：尝试为事件{0}移除不同类型的委托，当前委托类型为{1}，要移除委托类型为{2}", _eventType, d.GetType(), _callBack.GetType()));
            }
        }
        else
        {
            //没有找到该事件类型
            throw new Exception(string.Format("移除监听错误，没有事件类型{0}", _eventType));
        }
    }
    /// <summary>
    /// 用来删除监听
    /// </summary>
    /// <param name="eventType"></param>
    private static void OnListenerRemoved(GameEventType eventType)
    {
        if (EventTable[eventType] == null)
        {
            EventTable.Remove(eventType);
        }
    }
    #endregion
    #region 0号广播类型(不含参数)
    public static void AddListener(GameEventType _eventType, CallBack _callBack)
    {
        OnListenerAdding(_eventType, _callBack);//检测能否添加，如果不能则抛出异常
        EventTable[_eventType] = (CallBack)EventTable[_eventType] + _callBack;//如果没有报错，添加该委托
    }
    public static void RemoveListener(GameEventType _eventType, CallBack _callBack)
    {
        OnListenerRemoving(_eventType, _callBack);
        EventTable[_eventType] = (CallBack)EventTable[_eventType] - _callBack;//如果没有报错，删除该委托
        OnListenerRemoved(_eventType);//如果委托为空，直接删除该事件类型
    }
    public static void Broadcast(GameEventType _eventType)
    {
        Delegate d;
        if (EventTable.TryGetValue(_eventType, out d))
        {
            CallBack callBack = d as CallBack;
            if (callBack != null)
            {
                callBack();//调用当前事件类型对应委托
            }
            else
            {
                //as类型转化错误，callback为空，说明事件所包含的委托类型不一样
                throw new Exception(string.Format("广播事件错误：事件{0}对应的委托具有不同的类型", _eventType));
            }
        }

    }
    #endregion
    #region 1号广播类型(1个参数)
    public static void AddListener<T>(GameEventType _eventType, CallBack<T> _callBack)
    {
        OnListenerAdding(_eventType, _callBack);//检测能否添加，如果不能则抛出异常
        EventTable[_eventType] = (CallBack<T>)EventTable[_eventType] + _callBack;//如果没有报错，添加该委托
    }
    public static void RemoveListener<T>(GameEventType _eventType, CallBack<T> _callBack)
    {
        OnListenerRemoving(_eventType, _callBack);
        EventTable[_eventType] = (CallBack<T>)EventTable[_eventType] - _callBack;//如果没有报错，删除该委托
        OnListenerRemoved(_eventType);//如果委托为空，直接删除该事件类型
    }
    public static void Broadcast<T>(GameEventType _eventType, T arg)
    {
        Delegate d;
        if (EventTable.TryGetValue(_eventType, out d))
        {
            CallBack<T> callBack = d as CallBack<T>;
            if (callBack != null)
            {
                callBack(arg);//调用当前事件类型对应委托
            }
            else
            {
                //as类型转化错误，callback为空，说明事件所包含的委托类型不一样
                throw new Exception(string.Format("广播事件错误：事件{0}对应的委托具有不同的类型", _eventType));
            }
        }

    }
    #endregion
    #region 2号广播类型(2个参数)
    public static void AddListener<T, X>(GameEventType _eventType, CallBack<T, X> _callBack)
    {
        OnListenerAdding(_eventType, _callBack);//检测能否添加，如果不能则抛出异常
        EventTable[_eventType] = (CallBack<T, X>)EventTable[_eventType] + _callBack;//如果没有报错，添加该委托
    }
    public static void RemoveListener<T, X>(GameEventType _eventType, CallBack<T, X> _callBack)
    {
        OnListenerRemoving(_eventType, _callBack);
        EventTable[_eventType] = (CallBack<T, X>)EventTable[_eventType] - _callBack;//如果没有报错，删除该委托
        OnListenerRemoved(_eventType);//如果委托为空，直接删除该事件类型
    }
    public static void Broadcast<T, X>(GameEventType _eventType, T arg1, X arg2)
    {
        Delegate d;
        if (EventTable.TryGetValue(_eventType, out d))
        {
            CallBack<T, X> callBack = d as CallBack<T, X>;
            if (callBack != null)
            {
                callBack(arg1, arg2);//调用当前事件类型对应委托
            }
            else
            {
                //as类型转化错误，callback为空，说明事件所包含的委托类型不一样
                throw new Exception(string.Format("广播事件错误：事件{0}对应的委托具有不同的类型", _eventType));
            }
        }

    }
    #endregion
    #region 3号广播类型(3个参数)
    public static void AddListener<T, X, Y>(GameEventType _eventType, CallBack<T, X, Y> _callBack)
    {
        OnListenerAdding(_eventType, _callBack);//检测能否添加，如果不能则抛出异常
        EventTable[_eventType] = (CallBack<T, X, Y>)EventTable[_eventType] + _callBack;//如果没有报错，添加该委托
    }
    public static void RemoveListener<T, X, Y>(GameEventType _eventType, CallBack<T, X, Y> _callBack)
    {
        OnListenerRemoving(_eventType, _callBack);
        EventTable[_eventType] = (CallBack<T, X, Y>)EventTable[_eventType] - _callBack;//如果没有报错，删除该委托
        OnListenerRemoved(_eventType);//如果委托为空，直接删除该事件类型
    }
    public static void Broadcast<T, X, Y>(GameEventType _eventType, T arg1, X arg2, Y arg3)
    {
        Delegate d;
        if (EventTable.TryGetValue(_eventType, out d))
        {
            CallBack<T, X, Y> callBack = d as CallBack<T, X, Y>;
            if (callBack != null)
            {
                callBack(arg1, arg2, arg3);//调用当前事件类型对应委托
            }
            else
            {
                //as类型转化错误，callback为空，说明事件所包含的委托类型不一样
                throw new Exception(string.Format("广播事件错误：事件{0}对应的委托具有不同的类型", _eventType));
            }
        }

    }
    #endregion
    #region 4号广播类型(4个参数)
    public static void AddListener<T, X, Y, Z>(GameEventType _eventType, CallBack<T, X, Y, Z> _callBack)
    {
        OnListenerAdding(_eventType, _callBack);//检测能否添加，如果不能则抛出异常
        EventTable[_eventType] = (CallBack<T, X, Y, Z>)EventTable[_eventType] + _callBack;//如果没有报错，添加该委托
    }
    public static void RemoveListener<T, X, Y, Z>(GameEventType _eventType, CallBack<T, X, Y, Z> _callBack)
    {
        OnListenerRemoving(_eventType, _callBack);
        EventTable[_eventType] = (CallBack<T, X, Y, Z>)EventTable[_eventType] - _callBack;//如果没有报错，删除该委托
        OnListenerRemoved(_eventType);//如果委托为空，直接删除该事件类型
    }
    public static void Broadcast<T, X, Y, Z>(GameEventType _eventType, T arg1, X arg2, Y arg3, Z arg4)
    {
        Delegate d;
        if (EventTable.TryGetValue(_eventType, out d))
        {
            CallBack<T, X, Y, Z> callBack = d as CallBack<T, X, Y, Z>;
            if (callBack != null)
            {
                callBack(arg1, arg2, arg3, arg4);//调用当前事件类型对应委托
            }
            else
            {
                //as类型转化错误，callback为空，说明事件所包含的委托类型不一样
                throw new Exception(string.Format("广播事件错误：事件{0}对应的委托具有不同的类型", _eventType));
            }
        }

    }

    #endregion
}
