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

using Zongsoft.Services;

namespace Zongsoft.Diagnostics.Telemetry;

public abstract class ExporterLauncherBase<TArgument> : IExporterLauncher<TArgument>, IMatchable, IMatchable<string>
{
	#region 构造函数
	protected ExporterLauncherBase(string name) => this.Name = name ?? string.Empty;
	#endregion

	#region 公共属性
	public string Name { get; }
	#endregion

	#region 公共方法
	public abstract void Launch(TArgument argument, string settings);
	#endregion

	#region 服务匹配
	bool IMatchable<string>.Match(string argument) => this.OnMatch(argument);
	bool IMatchable.Match(object argument) => this.OnMatch(argument as string);
	protected virtual bool OnMatch(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	#endregion
}
