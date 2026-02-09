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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Diagnostics;

public abstract partial class LoggingBase<TLog> where TLog : ILog
{
	#region 日志方法
	public void Log(LogLevel level, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, exception, data, null, action));
	public void Log(LogLevel level, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, data, null, action));
	public void Log(LogLevel level, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, exception, data, null, action));

	public void Log<TSource>(LogLevel level, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, exception, data, this.GetSource(typeof(TSource)), action));
	public void Log<TSource>(LogLevel level, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, data, this.GetSource(typeof(TSource)), action));
	public void Log<TSource>(LogLevel level, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, exception, data, this.GetSource(typeof(TSource)), action));

	public void Log(Type source, LogLevel level, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, exception, data, this.GetSource(source), action));
	public void Log(Type source, LogLevel level, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, data, this.GetSource(source), action));
	public void Log(Type source, LogLevel level, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, exception, data, this.GetSource(source), action));

	public void Log(object source, LogLevel level, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, exception, data, this.GetSource(source), action));
	public void Log(object source, LogLevel level, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, data, this.GetSource(source), action));
	public void Log(object source, LogLevel level, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(level, message, exception, data, this.GetSource(source), action));

	public ValueTask LogAsync(LogLevel level, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, null, null, action), cancellation);
	public ValueTask LogAsync(LogLevel level, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, data, null, action), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, null, null, action), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, data, null, action), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, null, null, action), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, data, null, action), cancellation);

	public ValueTask LogAsync<TSource>(LogLevel level, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask LogAsync<TSource>(LogLevel level, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, data, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask LogAsync<TSource>(LogLevel level, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask LogAsync<TSource>(LogLevel level, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, data, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask LogAsync<TSource>(LogLevel level, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask LogAsync<TSource>(LogLevel level, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, data, this.GetSource(typeof(TSource)), action), cancellation);

	public ValueTask LogAsync(Type source, LogLevel level, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(Type source, LogLevel level, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, data, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(Type source, LogLevel level, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, null, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(Type source, LogLevel level, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, data, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(Type source, LogLevel level, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(Type source, LogLevel level, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, data, this.GetSource(source), action), cancellation);

	public ValueTask LogAsync(object source, LogLevel level, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(object source, LogLevel level, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, exception, data, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(object source, LogLevel level, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, null, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(object source, LogLevel level, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, data, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(object source, LogLevel level, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask LogAsync(object source, LogLevel level, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(level, message, exception, data, this.GetSource(source), action), cancellation);
	#endregion

	#region 保护方法
	protected virtual string GetSource(object target) => target switch
	{
		string text => text,
		Type type => this.GetSource(type),
		_ => this.GetSource(target?.GetType()),
	};

	protected virtual string GetSource(Type type)
	{
		if(type == null)
			return null;

		var attribute = ApplicationModuleAttribute.Find(type);

		if(attribute == null || string.IsNullOrEmpty(attribute.Name) || attribute.Name == "_")
			return TypeAlias.GetAlias(type, true);
		else
			return $"{attribute.Name}:{TypeAlias.GetAlias(type, true)}";
	}
	#endregion

	#region 创建日志
	protected virtual TLog CreateLog(LogLevel level, string message, object data, string source, string action) => this.CreateLog(level, message, null, data, source, action);
	protected virtual TLog CreateLog(LogLevel level, Exception exception, object data, string source, string action) => this.CreateLog(level, null, exception, data, source, action);
	protected abstract TLog CreateLog(LogLevel level, string message, Exception exception, object data, string source, string action);
	#endregion
}
