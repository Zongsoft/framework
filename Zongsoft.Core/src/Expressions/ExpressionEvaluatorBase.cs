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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Collections.Generic;

namespace Zongsoft.Expressions;

public abstract class ExpressionEvaluatorBase(string name) : IExpressionEvaluator, Services.IMatchable, Services.IMatchable<string>
{
	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 成员变量
	private volatile int _disposing;
	private IExpressionEvaluatorOptions _options;
	#endregion

	#region 公共属性
	public string Name { get; } = name;
	public bool IsDisposed => _disposing == DISPOSED;
	public IDictionary<string, object> Global { get; protected set; }
	public IExpressionEvaluatorOptions Options
	{
		get => _options;
		set
		{
			if(_disposing == DISPOSED)
				throw new ObjectDisposedException(this.Name);

			_options = value ?? throw new ArgumentNullException(nameof(value));
		}
	}
	#endregion

	#region 公共方法
	public object Evaluate(string expression, IDictionary<string, object> variables = null) => this.Evaluate(expression, this.Options, variables);
	public abstract object Evaluate(string expression, IExpressionEvaluatorOptions options, IDictionary<string, object> variables = null);
	#endregion

	#region 服务匹配
	bool Services.IMatchable.Match(object argument) => argument is string name && string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase);
	bool Services.IMatchable<string>.Match(string name) => string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase);
	#endregion

	#region 释放资源
	public void Dispose()
	{
		var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
		if(disposing != 0)
			return;

		try
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			_disposing = DISPOSED;
		}
	}

	protected virtual void Dispose(bool disposing) => this.Global.Clear();
	#endregion
}
