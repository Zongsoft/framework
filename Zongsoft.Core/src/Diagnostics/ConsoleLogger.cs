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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Tasks;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Diagnostics;

public sealed class ConsoleLogger : ConsoleLogger<LogEntry>
{
	#region 单例字段
	public static readonly ConsoleLogger Instance = new();
	#endregion

	#region 构造函数
	public ConsoleLogger()
	{
		this.Predication = new LoggerPredication(this.Name);
	}
	#endregion

	#region 重写方法
	protected override CommandOutletContent Format(LogEntry log) => (this.Formatter ?? ConsoleFormatter.Default).Format(log);
	#endregion
}

public abstract class ConsoleLogger<TLog> : LoggerBase<TLog, CommandOutletContent> where TLog : ILog
{
	#region 重写方法
	protected override async ValueTask<bool> CanLogAsync(TLog log, CancellationToken cancellation)
	{
		return IsTerminalApplication() && await base.CanLogAsync(log, cancellation);
	}

	protected override ValueTask OnLogAsync(TLog log, CancellationToken cancellation)
	{
		try
		{
			Terminal.WriteLine(this.Format(log));
		}
		catch { }

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 虚拟方法
	protected abstract CommandOutletContent Format(TLog log);
	#endregion

	#region 私有方法
	private static bool IsTerminalApplication() =>
		string.Equals(Zongsoft.Services.ApplicationContext.Current.ApplicationType, "Console", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(Zongsoft.Services.ApplicationContext.Current.ApplicationType, "Terminal", StringComparison.OrdinalIgnoreCase);
	#endregion
}
