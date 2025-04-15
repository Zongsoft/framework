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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Components;

public abstract class ExecutorContextBase : IExecutorContext
{
	#region 成员字段
	private Exception _exception;
	#endregion

	#region 构造函数
	protected ExecutorContextBase(IExecutor executor)
	{
		this.Executor = executor;
		this.Parameters = new Collections.Parameters();
	}

	protected ExecutorContextBase(IExecutor executor, Collections.Parameters parameters)
	{
		this.Executor = executor;
		this.Parameters = parameters ?? new Collections.Parameters();
	}

	protected ExecutorContextBase(IExecutor executor, IEnumerable<KeyValuePair<string, object>> parameters)
	{
		this.Executor = executor;
		this.Parameters = new Collections.Parameters(parameters);
	}
	#endregion

	#region 公共属性
	public IExecutor Executor { get; }
	public Collections.Parameters Parameters { get; }
	#endregion

	#region 显式实现
	protected abstract object GetArgument();
	object IExecutorContext.Argument => this.GetArgument();
	#endregion

	#region 公共方法
	public void Error(Exception exception) => _exception = exception;
	public bool HasError(out Exception exception) => (exception = _exception) != null;
	#endregion
}
