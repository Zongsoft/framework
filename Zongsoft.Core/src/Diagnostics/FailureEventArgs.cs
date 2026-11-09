/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Diagnostics;

/// <summary>提供故障异常、处理标志及关联状态。</summary>
[Serializable]
public class FailureEventArgs : EventArgs
{
	#region 构造函数
	/// <summary>初始化未处理的故障通知。</summary>
	/// <param name="exception">与故障关联的异常。</param>
	/// <param name="state">与故障关联的调用方状态。</param>
	public FailureEventArgs(Exception exception, object state = null) : this(exception, false, state) { }
	/// <summary>初始化故障通知。</summary>
	/// <param name="exception">与故障关联的异常。</param>
	/// <param name="exceptionHandled">指示异常是否已经由订阅者处理。</param>
	/// <param name="state">与故障关联的调用方状态。</param>
	public FailureEventArgs(Exception exception, bool exceptionHandled, object state = null)
	{
		this.Exception = exception;
		this.ExceptionHandled = exceptionHandled;
		this.State = state;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置与故障关联的异常。</summary>
	public Exception Exception { get; set; }
	/// <summary>获取或设置一个值，指示异常是否已经由订阅者处理；该标志的作用由事件发布者定义。</summary>
	public bool ExceptionHandled { get; set; }
	/// <summary>获取与故障关联的调用方状态。</summary>
	public object State { get; }
	#endregion
}
