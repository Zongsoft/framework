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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Caching;

namespace Zongsoft.Diagnostics;

public abstract class FileLogger<TLog, TModel> : LoggerBase<TLog, TModel>, IAsyncDisposable where TLog : ILog
{
	#region 常量定义
	internal const int FILE_LIMIT     = 1024 * 1024;
	internal const int LOGGING_LIMIT  = 100;
	internal const int LOGGING_PERIOD = 1; //Unit: Seconds
	#endregion

	#region 事件声明
	/// <summary>周期写入失败时发生；周期运行停止，显式写入异常仍由原操作传播。</summary>
	/// <remarks>在所有订阅者收到通知后传播通知异常；通知内不得等待自身刷新或异步释放。</remarks>
	public event EventHandler<FailureEventArgs> Failed;
	#endregion

	#region 静态字段
	private static readonly AsyncLocal<Callback> _current = new();
	#endregion

	#region 私有变量
	private readonly object _sync = new();
	private readonly TaskCompletionSource _idle = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource _disposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
	private int _disposed;
	private int _operations;
	#endregion

	#region 构造函数
	protected FileLogger(string filePath, int fileLimit = FILE_LIMIT) : this(TimeSpan.FromSeconds(LOGGING_PERIOD), LOGGING_LIMIT, filePath, fileLimit) { }
	protected FileLogger(TimeSpan period, int capacity = LOGGING_LIMIT) : this(period, capacity, null, FILE_LIMIT) { }
	protected FileLogger(TimeSpan period, int capacity, string filePath, int fileLimit = FILE_LIMIT)
	{
		this.FilePath = filePath?.Trim();
		this.FileLimit = Math.Max(fileLimit, 0);
		this.Logging = period > TimeSpan.Zero || capacity > 1 ? new(this.OnFlushAsync, period, capacity) : null;

		if(this.Logging != null)
			this.Logging.Failed += this.OnFailed;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置文件路径。</summary>
	public string FilePath { get; set; }

	/// <summary>获取或设置日志文件的大小限制，单位为字节(Byte)，默认为<c>1</c>MB。</summary>
	public int FileLimit { get; set; }
	#endregion

	#region 保护属性
	/// <summary>获取日志缓冲器。</summary>
	protected Spooler<TLog> Logging { get; }
	#endregion

	#region 重写方法
	protected override ValueTask<bool> CanLogAsync(TLog log, CancellationToken cancellation)
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

		if(this.Predication == null && log.Level <= LogLevel.Debug)
			return ValueTask.FromResult(false);

		return base.CanLogAsync(log, cancellation);
	}
	#endregion

	#region 日志方法
	protected override async ValueTask OnLogAsync(TLog log, CancellationToken cancellation)
	{
		this.Enter();

		try
		{
			var logging = this.Logging;

			if(logging == null)
				await this.OnFlushAsync([log], cancellation).ConfigureAwait(false);
			else
			{
				await logging.PutAsync(log, cancellation).ConfigureAwait(false);

				if(log.Level >= LogLevel.Error)
					await logging.FlushAsync(cancellation).ConfigureAwait(false);
			}
		}
		finally { this.Exit(); }
	}

	protected override async ValueTask OnFlushAsync(CancellationToken cancellation)
	{
		this.Enter();

		try
		{
			if(this.Logging != null)
				await this.Logging.FlushAsync(cancellation).ConfigureAwait(false);
		}
		finally { this.Exit(); }
	}

	private async ValueTask OnFlushAsync(IEnumerable<TLog> logs, CancellationToken cancellation)
	{
		var callback = new Callback(this, _current.Value);
		_current.Value = callback;

		try
		{
			foreach(var group in logs.GroupBy(log => new LogFileToken(log.Source, this.ResolveSequence(log))))
			{
				if(group.Key.IsEmpty)
					continue;

				var semaphore = _semaphores.GetOrAdd(group.Key.Source, key => new(1, 1));

				//进入临界区确保同源只有一个写入操作
				await semaphore.WaitAsync(cancellation).ConfigureAwait(false);

				try
				{
					//以写模式打开日志文件
					using var stream = new FileStream(group.Key.FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);

					//批量写入日志文件
					await this.WriteLogsAsync(stream, group, cancellation).ConfigureAwait(false);
				}
				finally { semaphore.Release(); }
			}
		}
		finally
		{
			Volatile.Write(ref callback.Active, false);
			_current.Value = callback.Parent;
		}
	}
	#endregion

	#region 事件处理
	private void OnFailed(object sender, FailureEventArgs args)
	{
		var callback = new Callback(this, _current.Value);
		_current.Value = callback;

		try
		{
			var handlers = this.Failed;
			if(handlers == null)
				return;

			List<Exception> errors = null;

			foreach(EventHandler<FailureEventArgs> handler in handlers.GetInvocationList())
			{
				try { handler(this, args); }
				catch(Exception exception) { (errors ??= []).Add(exception); }
			}

			if(errors != null)
				throw new AggregateException(errors);
		}
		finally
		{
			Volatile.Write(ref callback.Active, false);
			_current.Value = callback.Parent;
		}
	}
	#endregion

	#region 处置方法
	/// <summary>停止接收并等待在途写入及周期故障通知结束，然后释放文件写入的同步资源；不排空缓冲。</summary>
	/// <returns>释放完成的操作；周期运行或故障通知的异常由该操作传播。</returns>
	/// <exception cref="InvalidOperationException">写入回调或故障通知试图等待自身释放。</exception>
	public async ValueTask DisposeAsync()
	{
		this.ThrowIfSelfWaiting();

		try { await this.DisposeAsyncCore().ConfigureAwait(false); }
		finally
		{
			_ = this.CloseAsync();
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>等待所有在途写入及资源清理；派生类应先等待基类实现，再释放写入所依赖的资源。</summary>
	/// <returns>共享的释放完成结果。</returns>
	protected virtual ValueTask DisposeAsyncCore() => new(this.CloseAsync());

	private Task CloseAsync()
	{
		lock(_sync)
		{
			if(_disposed != 0)
				return _disposal.Task;

			Volatile.Write(ref _disposed, 1);

			if(_operations == 0)
				_idle.TrySetResult();
		}

		_ = FinishAsync();
		return _disposal.Task;

		async Task FinishAsync()
		{
			Exception failure = null;

			try
			{
				if(this.Logging != null)
					await this.Logging.DisposeAsync().ConfigureAwait(false);
			}
			catch(Exception exception) { failure = exception; }

			try
			{
				await _idle.Task.ConfigureAwait(false);

				if(this.Logging != null)
					this.Logging.Failed -= this.OnFailed;

				foreach(var semaphore in _semaphores.Values)
					semaphore.Dispose();

				_semaphores.Clear();
			}
			catch(Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }

			if(failure == null)
				_disposal.TrySetResult();
			else
				_disposal.TrySetException(failure);
		}
	}

	private void Enter()
	{
		lock(_sync)
		{
			ObjectDisposedException.ThrowIf(_disposed != 0, this);
			_operations++;
		}
	}

	private void Exit()
	{
		lock(_sync)
		{
			if(--_operations == 0 && _disposed != 0)
				_idle.TrySetResult();
		}
	}

	private void ThrowIfSelfWaiting()
	{
		for(var callback = _current.Value; callback != null; callback = callback.Parent)
		{
			if(ReferenceEquals(callback.Owner, this) && Volatile.Read(ref callback.Active))
				throw new InvalidOperationException(Properties.Resources.FileLogger_SelfWait_Message);
		}
	}
	#endregion

	#region 抽象方法
	protected abstract ValueTask WriteLogsAsync(Stream output, IEnumerable<TLog> logs, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	protected virtual string GetFilePath(ILog log)
	{
		var filePath = this.FilePath;

		if(string.IsNullOrEmpty(filePath))
			filePath = $"~/logs/{log.Timestamp:yyyyMM}/{(string.IsNullOrEmpty(log.Source) ? Zongsoft.Diagnostics.Logging.Default.Name : log.Source)}-{{sequence}}.log";

		filePath = filePath.Replace((Path.DirectorySeparatorChar == '/' ? '\\' : '/'), Path.DirectorySeparatorChar).Trim();

		if(filePath[0] == '/' || filePath[0] == '\\')
		{
			filePath = Path.Combine(Path.GetPathRoot(GetApplicationDirectory()), filePath[1..]);

			if(!Directory.Exists(Path.GetDirectoryName(filePath)))
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			return filePath;
		}

		var directoryPath = filePath.StartsWith("~/") || filePath.StartsWith("~\\") ?
			EnsureApplicationDirectory(Path.GetDirectoryName(filePath[2..])) :
			EnsureApplicationDirectory(Path.GetDirectoryName(filePath));

		return Path.Combine(directoryPath, Path.GetFileName(filePath));
	}
	#endregion

	#region 私有方法
	private static string GetApplicationDirectory()
	{
		var direcotry = Services.ApplicationContext.Current?.ApplicationPath;

		if(string.IsNullOrEmpty(direcotry))
			return AppContext.BaseDirectory;

		return direcotry;
	}

	private static string EnsureApplicationDirectory(string relativePath)
	{
		if(string.IsNullOrEmpty(relativePath))
			return GetApplicationDirectory();

		var path = GetApplicationDirectory();
		var parts = relativePath.Split('/', '\\');

		for(int i = 0; i < parts.Length; i++)
		{
			path = Path.Combine(path, parts[i]);

			if(!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		return path;
	}

	private string ResolveSequence(ILog log)
	{
		const string PATTERN = @"(?<no>\d+)";
		const string SEQUENCE = "{sequence}";

		var maximum = 0;
		var result = string.Empty;
		var filePath = this.GetFilePath(log);

		if(string.IsNullOrEmpty(filePath) || (!filePath.Contains(SEQUENCE)))
			return filePath;

		if(this.FileLimit < 1)
			return filePath.Replace(SEQUENCE, string.Empty);

		var fileName = System.IO.Path.GetFileName(filePath);
		var infos = Zongsoft.IO.LocalFileSystem.Instance.Directory.GetFiles(System.IO.Path.GetDirectoryName(filePath), fileName.Replace(SEQUENCE, "|" + PATTERN + "|"), false);
		var pattern = string.Empty;
		int index = 0, position = 0;

		while((index = fileName.IndexOf(SEQUENCE, index)) >= 0)
		{
			if(index > 0)
				pattern += Zongsoft.IO.LocalFileSystem.LocalDirectoryProvider.EscapePattern(fileName[position..index]);

			pattern += PATTERN;
			index += SEQUENCE.Length;
			position = index;
		}

		if(position < fileName.Length)
			pattern += Zongsoft.IO.LocalFileSystem.LocalDirectoryProvider.EscapePattern(fileName[position..]);

		//设置正则匹配模式为完整匹配
		if(pattern != null && pattern.Length > 0)
			pattern = "^" + pattern + "$";

		foreach(var info in infos)
		{
			var match = System.Text.RegularExpressions.Regex.Match(info.Name, pattern);

			if(match.Success)
			{
				var number = int.Parse(match.Groups["no"].Value);

				if(number > maximum)
				{
					maximum = number;

					if(info.Size < this.FileLimit)
						result = info.Url;
				}
			}
		}

		if(string.IsNullOrEmpty(result))
			return filePath.Replace(SEQUENCE, (maximum + 1).ToString());

		return result;
	}
	#endregion

	#region 嵌套结构
	private readonly struct LogFileToken(string source, string filePath) : IEquatable<LogFileToken>
	{
		public readonly string Source = source;
		public readonly string FilePath = filePath;
		public bool IsEmpty => string.IsNullOrEmpty(this.FilePath);

		public bool Equals(LogFileToken other) => string.Equals(this.FilePath, other.FilePath);
		public override bool Equals(object obj) => obj is LogFileToken other && base.Equals(other);
		public override int GetHashCode() => this.FilePath.GetHashCode();
		public override string ToString() => this.FilePath;
	}
	#endregion

	#region 回调相关
	private sealed class Callback(FileLogger<TLog, TModel> owner, Callback parent)
	{
		public readonly FileLogger<TLog, TModel> Owner = owner;
		public readonly Callback Parent = parent;
		public bool Active = true;
	}
	#endregion
}
