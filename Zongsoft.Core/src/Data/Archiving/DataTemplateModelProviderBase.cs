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
using System.Threading.Tasks;

using Zongsoft.Services;

namespace Zongsoft.Data.Archiving;

public abstract class DataTemplateModelProviderBase : IDataTemplateModelProvider, IMatchable
{
	#region 构造函数
	protected DataTemplateModelProviderBase(string name, IServiceProvider services)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.Services = services ?? throw new ArgumentNullException(nameof(services));
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public IServiceProvider Services { get; }
	#endregion

	#region 抽象方法
	public abstract ValueTask<IDataTemplateModel> GetModelAsync(IDataTemplate template, object argument, CancellationToken cancellation = default);
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object parameter) => this.OnMatch(parameter);
	protected virtual bool OnMatch(object parameter) => parameter switch
	{
		string name => string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase),
		IDataTemplate template => string.Equals(template?.Name, this.Name, StringComparison.OrdinalIgnoreCase),
		_ => false,
	};
	#endregion
}
