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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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
