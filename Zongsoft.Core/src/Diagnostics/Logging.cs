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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Diagnostics;

public sealed class Logging
{
	#region 静态字段
	private static readonly ConcurrentDictionary<string, Logging> _factory = new(StringComparer.Ordinal);
	private static readonly List<ILogger> _loggers = new();
	#endregion

	#region 私有构造
	private Logging(string name) => this.Name = name ?? throw new ArgumentNullException(nameof(name));
	#endregion

	#region 静态属性
	public static ICollection<ILogger> Loggers => _loggers;
	#endregion

	#region 实例属性
	public string Name { get; }
	#endregion

	#region 静态方法
	public static Logging GetLogging<T>(T instance) => GetLogging(instance == null ? typeof(T) : instance.GetType());
	public static Logging GetLogging<T>() => GetLogging(typeof(T));
	public static Logging GetLogging(Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		return _factory.GetOrAdd(type.Assembly.GetName().Name, key => new Logging(key));
	}
	public static Logging GetLogging(Assembly assembly)
	{
		if(assembly == null)
			throw new ArgumentNullException(nameof(assembly));

		return _factory.GetOrAdd(assembly.GetName().Name, key => new Logging(key));
	}
	public static Logging GetLogging(string name = null)
	{
		if(string.IsNullOrEmpty(name))
			name = Assembly.GetEntryAssembly().GetName().Name;

		return _factory.GetOrAdd(name, key => new Logging(key));
	}
	#endregion

    #region 实例日志
    public void Trace(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Trace, this.Name, exception, data));
	public void Trace(string message, object data = null) => Log(new LogEntry(LogLevel.Trace, this.Name, message, data));
	public void Trace(string message, Exception exception, object data = null) => Log(new LogEntry(LogLevel.Trace, this.Name, message, exception, data));

	public ValueTask TraceAsync(Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Trace, this.Name, exception, null), cancellation);
	public ValueTask TraceAsync(Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Trace, this.Name, exception, data), cancellation);
	public ValueTask TraceAsync(string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Trace, this.Name, message, null), cancellation);
	public ValueTask TraceAsync(string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Trace, this.Name, message, data), cancellation);
	public ValueTask TraceAsync(string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Trace, this.Name, message, exception, null), cancellation);
	public ValueTask TraceAsync(string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Trace, this.Name, message, exception, data), cancellation);

	public void Debug(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Debug, this.Name, exception, data));
	public void Debug(string message, object data = null) => Log(new LogEntry(LogLevel.Debug, this.Name, message, data));
	public void Debug(string message, Exception exception, object data = null) => Log(new LogEntry(LogLevel.Debug, this.Name, message, exception, data));

	public ValueTask DebugAsync(Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Debug, this.Name, exception, null), cancellation);
	public ValueTask DebugAsync(Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Debug, this.Name, exception, data), cancellation);
	public ValueTask DebugAsync(string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Debug, this.Name, message, null), cancellation);
	public ValueTask DebugAsync(string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Debug, this.Name, message, data), cancellation);
	public ValueTask DebugAsync(string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Debug, this.Name, message, exception, null), cancellation);
	public ValueTask DebugAsync(string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Debug, this.Name, message, exception, data), cancellation);

	public void Info(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Info, this.Name, exception, data));
	public void Info(string message, object data = null) => Log(new LogEntry(LogLevel.Info, this.Name, message, data));
	public void Info(string message, Exception exception, object data = null) => Log(new LogEntry(LogLevel.Info, this.Name, message, exception, data));

	public ValueTask InfoAsync(Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Info, this.Name, exception, null), cancellation);
	public ValueTask InfoAsync(Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Info, this.Name, exception, data), cancellation);
	public ValueTask InfoAsync(string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Info, this.Name, message, null), cancellation);
	public ValueTask InfoAsync(string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Info, this.Name, message, data), cancellation);
	public ValueTask InfoAsync(string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Info, this.Name, message, exception, null), cancellation);
	public ValueTask InfoAsync(string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Info, this.Name, message, exception, data), cancellation);

	public void Warn(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Warn, this.Name, exception, data));
	public void Warn(string message, object data = null) => Log(new LogEntry(LogLevel.Warn, this.Name, message, data));
	public void Warn(string message, Exception exception, object data = null) => Log(new LogEntry(LogLevel.Warn, this.Name, message, exception, data));

	public ValueTask WarnAsync(Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Warn, this.Name, exception, null), cancellation);
	public ValueTask WarnAsync(Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Warn, this.Name, exception, data), cancellation);
	public ValueTask WarnAsync(string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Warn, this.Name, message, null), cancellation);
	public ValueTask WarnAsync(string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Warn, this.Name, message, data), cancellation);
	public ValueTask WarnAsync(string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Warn, this.Name, message, exception, null), cancellation);
	public ValueTask WarnAsync(string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Warn, this.Name, message, exception, data), cancellation);

	public void Error(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Error, this.Name, exception, data));
	public void Error(string message, object data = null) => Log(new LogEntry(LogLevel.Error, this.Name, message, data));
	public void Error(string message, Exception exception, object data = null) => Log(new LogEntry(LogLevel.Error, this.Name, message, exception, data));

	public ValueTask ErrorAsync(Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Error, this.Name, exception, null), cancellation);
	public ValueTask ErrorAsync(Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Error, this.Name, exception, data), cancellation);
	public ValueTask ErrorAsync(string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Error, this.Name, message, null), cancellation);
	public ValueTask ErrorAsync(string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Error, this.Name, message, data), cancellation);
	public ValueTask ErrorAsync(string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Error, this.Name, message, exception, null), cancellation);
	public ValueTask ErrorAsync(string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Error, this.Name, message, exception, data), cancellation);

	public void Fatal(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Fatal, this.Name, exception, data));
	public void Fatal(string message, object data = null) => Log(new LogEntry(LogLevel.Fatal, this.Name, message, data));
	public void Fatal(string message, Exception exception, object data = null) => Log(new LogEntry(LogLevel.Fatal, this.Name, message, exception, data));

	public ValueTask FatalAsync(Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Fatal, this.Name, exception, null), cancellation);
	public ValueTask FatalAsync(Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Fatal, this.Name, exception, data), cancellation);
	public ValueTask FatalAsync(string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Fatal, this.Name, message, null), cancellation);
	public ValueTask FatalAsync(string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Fatal, this.Name, message, data), cancellation);
	public ValueTask FatalAsync(string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Fatal, this.Name, message, exception, null), cancellation);
	public ValueTask FatalAsync(string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(LogLevel.Fatal, this.Name, message, exception, data), cancellation);

	public void Log(LogLevel level, Exception exception, object data = null) => Log(new LogEntry(level, this.Name, exception, data));
	public void Log(LogLevel level, string message, object data = null) => Log(new LogEntry(level, this.Name, message, data));
	public void Log(LogLevel level, string message, Exception exception, object data = null) => Log(new LogEntry(level, this.Name, message, exception, data));

	public ValueTask LogAsync(LogLevel level, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(level, this.Name, exception, null), cancellation);
	public ValueTask LogAsync(LogLevel level, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(level, this.Name, exception, data), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, CancellationToken cancellation = default) => LogAsync(new LogEntry(level, this.Name, message, null), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(level, this.Name, message, data), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, Exception exception, CancellationToken cancellation = default) => LogAsync(new LogEntry(level, this.Name, message, exception, null), cancellation);
	public ValueTask LogAsync(LogLevel level, string message, Exception exception, object data, CancellationToken cancellation = default) => LogAsync(new LogEntry(level, this.Name, message, exception, data), cancellation);
	#endregion

	#region 静态日志
	public static void Log<TLog>(TLog log) where TLog : ILog => LogAsync(log).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
	public static ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : ILog
	{
		if(log == null)
			return ValueTask.CompletedTask;

		if(_loggers.Count == 0 && log is LogEntry entry)
			return TextFileLogger.Default.LogAsync(entry, cancellation);
		else
			return new ValueTask(Parallel.ForEachAsync(_loggers, (logger, cancellation) => logger.LogAsync(log, cancellation)));
	}
	#endregion
}