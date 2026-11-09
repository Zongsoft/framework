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

namespace Zongsoft.Components;

public class WorkerStateChangedEventArgs : EventArgs
{
	#region 成员字段
	private string _actionName;
	private WorkerState _state;
	private Exception _exception;
	#endregion

	#region 构造函数
	public WorkerStateChangedEventArgs(string actionName, WorkerState state) : this(actionName, state, null) { }
	public WorkerStateChangedEventArgs(string actionName, WorkerState state, Exception exception)
	{
		if(string.IsNullOrWhiteSpace(actionName))
			throw new ArgumentNullException(nameof(actionName));

		_actionName = actionName.Trim();
		_state = state;
		_exception = exception;
	}
	#endregion

	#region 公共属性
	public string ActionName => _actionName;
	public WorkerState State => _state;
	public Exception Exception => _exception;
	#endregion
}
