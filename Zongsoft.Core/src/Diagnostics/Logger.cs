/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Diagnostics
{
	public sealed class Logger
	{
		#region 静态字段
		private static readonly ConcurrentDictionary<string, Logger> _factory = new(StringComparer.Ordinal);
		private static readonly List<ILogger> _loggers = new();
		#endregion

		#region 私有构造
		private Logger(string source) => this.Source = source ?? throw new ArgumentNullException(nameof(source));
		#endregion

		#region 静态属性
		public static ICollection<ILogger> Loggers => _loggers;
		#endregion

		#region 实例属性
		public string Source { get; }
		#endregion

		#region 静态方法
		public static Logger GetLogger<T>(T _) => GetLogger(typeof(T));
		public static Logger GetLogger<T>() => GetLogger(typeof(T));
		public static Logger GetLogger(Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			return _factory.GetOrAdd(type.Assembly.GetName().Name, key => new Logger(key));
		}
		public static Logger GetLogger(Assembly assembly)
		{
			if(assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			return _factory.GetOrAdd(assembly.GetName().Name, key => new Logger(key));
		}
		public static Logger GetLogger(string source)
		{
			if(string.IsNullOrEmpty(source))
				throw new ArgumentNullException(nameof(source));

			return _factory.GetOrAdd(source, key => new Logger(key));
		}
		#endregion

		#region 日志方法
		public void Trace(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Trace, this.Source, exception, data));
		public void Trace(string message, object data = null) => Log(new LogEntry(LogLevel.Trace, this.Source, message, data));

		public void Debug(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Debug, this.Source, exception, data));
		public void Debug(string message, object data = null) => Log(new LogEntry(LogLevel.Debug, this.Source, message, data));

		public void Info(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Info, this.Source, exception, data));
		public void Info(string message, object data = null) => Log(new LogEntry(LogLevel.Info, this.Source, message, data));

		public void Warn(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Warn, this.Source, exception, data));
		public void Warn(string message, object data = null) => Log(new LogEntry(LogLevel.Warn, this.Source, message, data));

		public void Error(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Error, this.Source, exception, data));
		public void Error(string message, object data = null) => Log(new LogEntry(LogLevel.Error, this.Source, message, data));

		public void Fatal(Exception exception, object data = null) => Log(new LogEntry(LogLevel.Fatal, this.Source, exception, data));
		public void Fatal(string message, object data = null) => Log(new LogEntry(LogLevel.Fatal, this.Source, message, data));

		public void Log(LogLevel level, Exception exception, object data = null) => Log(new LogEntry(level, this.Source, exception, data));
		public void Log(LogLevel level, string message, object data = null) => Log(new LogEntry(level, this.Source, message, data));
		#endregion

		#region 私有方法
		private static void Log(LogEntry entry)
		{
			if(entry == null)
				return;

			if(_loggers.Count == 0)
				TextFileLogger.Default.Log(entry);
			else
				System.Threading.Tasks.Parallel.ForEach(_loggers, logger => logger?.Log(entry));
		}
		#endregion
	}
}