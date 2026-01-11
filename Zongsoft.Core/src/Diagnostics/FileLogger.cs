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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Caching;

namespace Zongsoft.Diagnostics;

public abstract class FileLogger<TLog, TModel> : LoggerBase<TLog, TModel> where TLog : ILog
{
	#region 常量定义
	internal const int FILE_LIMIT     = 1024 * 1024;
	internal const int LOGGING_LIMIT  = 100;
	internal const int LOGGING_PERIOD = 10; //Unit: Seconds
	#endregion

	#region 构造函数
	protected FileLogger(string filePath, int fileLimit = FILE_LIMIT) : this(TimeSpan.FromSeconds(LOGGING_PERIOD), LOGGING_LIMIT, filePath, fileLimit) { }
	protected FileLogger(TimeSpan period, int capacity = LOGGING_LIMIT) : this(period, capacity, null, FILE_LIMIT) { }
	protected FileLogger(TimeSpan period, int capacity, string filePath, int fileLimit = FILE_LIMIT)
	{
		this.FilePath = filePath?.Trim();
		this.FileLimit = Math.Max(fileLimit, 0);
		this.Logging = period > TimeSpan.Zero || capacity > 1 ? new(this.OnLogAsync, period, capacity) : null;
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
		if(this.Predication == null && log.Level <= LogLevel.Debug)
			return ValueTask.FromResult(false);

		return base.CanLogAsync(log, cancellation);
	}
	#endregion

	#region 日志方法
	protected override ValueTask OnLogAsync(TLog log, CancellationToken cancellation)
	{
		if(log.Level >= LogLevel.Error)
			return this.OnLogAsync([log], cancellation);

		var logging = this.Logging;

		if(logging == null)
			return this.OnLogAsync([log], cancellation);
		else
			return logging.PutAsync(log, cancellation);
	}

	protected virtual async ValueTask OnLogAsync(IEnumerable<TLog> logs, CancellationToken cancellation)
	{
		foreach(var group in logs.GroupBy(log => this.ResolveSequence(log)))
		{
			if(string.IsNullOrEmpty(group.Key))
				continue;

			//以写模式打开日志文件
			using var stream = new FileStream(group.Key, FileMode.Append, FileAccess.Write, FileShare.Read);

			//批量写入日志文件
			await this.WriteLogsAsync(stream, group, cancellation);
		}
	}
	#endregion

	#region 抽象方法
	protected abstract ValueTask WriteLogsAsync(Stream output, IEnumerable<TLog> logs, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	protected virtual string GetFilePath(ILog entry)
	{
		var filePath = this.FilePath;

		if(string.IsNullOrEmpty(filePath))
			filePath = $"~/logs/{entry.Timestamp:yyyyMM}/{(string.IsNullOrEmpty(entry.Source) ? Zongsoft.Diagnostics.Logging.Default.Name : entry.Source)}-{{sequence}}.log";

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

	private string ResolveSequence(ILog entry)
	{
		const string PATTERN = @"(?<no>\d+)";
		const string SEQUENCE = "{sequence}";

		var maximum = 0;
		var result = string.Empty;
		var filePath = this.GetFilePath(entry);

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
}
