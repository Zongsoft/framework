﻿/*
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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.Components;

public class ExecutorContext<TArgument, TResult> : ExecutorContextBase, IExecutorContext<TArgument, TResult>
{
	#region 构造函数
	public ExecutorContext(IExecutor executor, TArgument argument, Collections.Parameters parameters = null) : base(executor, parameters)
	{
		this.Argument = argument;
	}

	public ExecutorContext(IExecutor executor, TResult result, TArgument argument, Collections.Parameters parameters = null) : base(executor, parameters)
	{
		this.Result = result;
		this.Argument = argument;
	}

	public ExecutorContext(IExecutor executor, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(executor, parameters)
	{
		this.Argument = argument;
	}

	public ExecutorContext(IExecutor executor, TResult result, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(executor, parameters)
	{
		this.Result = result;
		this.Argument = argument;
	}
	#endregion

	#region 公共属性
	public TArgument Argument { get; }
	public TResult Result { get; set; }
	#endregion

	#region 重写方法
	protected override object GetArgument() => this.Argument;
	#endregion
}
