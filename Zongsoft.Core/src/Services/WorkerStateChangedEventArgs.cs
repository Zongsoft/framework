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

namespace Zongsoft.Services;

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
