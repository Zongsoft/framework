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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Diagnostics;

public abstract class FileLogger<T> : LoggerBase<T>
{
	#region 常量定义
	private const int DEFAULT_LIMIT = 1024 * 1024;
	#endregion

	#region 成员字段
	private readonly object _lock;
	private readonly ConcurrentQueue<LogEntry> _queue;
	#endregion

	#region 构造函数
	protected FileLogger() : this(null) { }
	protected FileLogger(string filePath, int limit = DEFAULT_LIMIT)
	{
		_lock = new();
		_queue = new();
		this.FilePath = filePath?.Trim();
		this.Limit = Math.Max(limit, 0);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置文件路径。</summary>
	public string FilePath { get; set; }

	/// <summary>获取或设置日志文件的大小限制，单位为字节(Byte)，默认为<c>1</c>MB。</summary>
	public int Limit { get; set; }
	#endregion

	#region 日志方法
	protected override void OnLog(LogEntry entry)
	{
		if(entry == null)
			return;

		var filePath = this.ResolveSequence(entry);
		if(string.IsNullOrWhiteSpace(filePath))
			return;

		if(entry.Level >= LogLevel.Error)
		{
			this.LogFile(filePath, entry);
		}
		else
		{
			//将日志实体加入内存队列中
			_queue.Enqueue(entry);

			//从线程池拉出一个后台线程进行具体的日志记录操作
			ThreadPool.QueueUserWorkItem(this.LogFile, filePath);
		}
	}

	private void LogFile(string filePath, LogEntry entry)
	{
		lock(_lock)
		{
			//以写模式打开日志文件
			using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);

			//将当前日志信息写入日志文件流
			this.WriteLog(entry, stream);
		}
	}

	private void LogFile(object filePath)
	{
		Stream stream = null;

		try
		{
			//当前线程获取日志写入锁
			Monitor.Enter(_lock);

			//以写模式打开日志文件
			stream = new FileStream((string)filePath, FileMode.Append, FileAccess.Write, FileShare.Read);

			//从日志队列中取出一条日志信息
			while(_queue.TryDequeue(out var entry))
			{
				//将当前日志信息写入日志文件流
				this.WriteLog(entry, stream);
			}
		}
		finally
		{
			//如果当前线程是日志写入线程
			if(Monitor.IsEntered(_lock))
			{
				//关闭日志文件流
				if(stream != null)
					stream.Dispose();

				//释放日志写入锁
				Monitor.Exit(_lock);
			}
		}
	}
	#endregion

	#region 抽象方法
	protected abstract void WriteLog(LogEntry entry, Stream output);
	#endregion

	#region 虚拟方法
	protected virtual string GetFilePath(LogEntry entry)
	{
		var filePath = this.FilePath;

		if(string.IsNullOrEmpty(filePath))
			filePath = $"~/logs/{entry.Timestamp:yyyyMM}/{(string.IsNullOrEmpty(entry.Source) ? "default" : entry.Source)}-{{sequence}}.log";

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

	protected virtual T Format(LogEntry entry)
	{
		var formatter = this.Formatter ?? throw new InvalidOperationException("Missing required formatter of the file logger.");
		return formatter.Format(entry);
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

	private string ResolveSequence(LogEntry entry)
	{
		const string PATTERN = @"(?<no>\d+)";
		const string SEQUENCE = "{sequence}";

		var maximum = 0;
		var result = string.Empty;
		var filePath = this.GetFilePath(entry);

		if(string.IsNullOrEmpty(filePath) || (!filePath.Contains(SEQUENCE)))
			return filePath;

		if(this.Limit < 1)
			return filePath.Replace(SEQUENCE, string.Empty);

		var fileName = System.IO.Path.GetFileName(filePath);
		var infos = Zongsoft.IO.LocalFileSystem.Instance.Directory.GetFiles(System.IO.Path.GetDirectoryName(filePath), fileName.Replace(SEQUENCE, "|" + PATTERN + "|"), false);
		var pattern = string.Empty;
		int index = 0, position = 0;

		while((index = fileName.IndexOf(SEQUENCE, index)) >= 0)
		{
			if(index > 0)
				pattern += Zongsoft.IO.LocalFileSystem.LocalDirectoryProvider.EscapePattern(fileName.Substring(position, index - position));

			pattern += PATTERN;
			index += SEQUENCE.Length;
			position = index;
		}

		if(position < fileName.Length)
			pattern += Zongsoft.IO.LocalFileSystem.LocalDirectoryProvider.EscapePattern(fileName.Substring(position));

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

					if(info.Size < this.Limit)
						result = info.Url;
				}
			}
		}

		if(string.IsNullOrEmpty(result))
			return filePath.Replace(SEQUENCE, (maximum + 1).ToString());

		return result;
	}
	#endregion
}
