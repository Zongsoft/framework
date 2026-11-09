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
