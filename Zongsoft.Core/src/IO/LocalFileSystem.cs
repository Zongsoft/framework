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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.IO;

public class LocalFileSystem : IFileSystem
{
	#region 单例字段
	public static readonly LocalFileSystem Instance = new();
	#endregion

	#region 私有构造
	private LocalFileSystem() { }
	#endregion

	#region 公共属性
	/// <summary>获取本地文件目录系统的方案，始终返回“zfs.local”。</summary>
	public string Scheme => "zfs.local";
	public IFile File => LocalFileProvider.Instance;
	public IDirectory Directory => LocalDirectoryProvider.Instance;
	#endregion

	#region 公共方法
	public string GetUrl(string path) => String.IsNullOrEmpty(path) ? null : GetLocalPath(path);
	public string GetUrl(Path path) => TryGetLocalPath(path, out var result) ? result : path.Url;
	#endregion

	#region 路径解析
	public static string GetLocalPath(string text) => TryGetLocalPath(Path.Parse(text), out var path) ? path : throw new PathException($"Illegal path format: `{text}`.");

	private static bool TryGetLocalPath(Path path, out string result)
	{
		switch(Environment.OSVersion.Platform)
		{
			case PlatformID.MacOSX:
			case PlatformID.Unix:
				result = path.FullPath;
				return true;
		}

		if(path.Anchor != PathAnchor.Root)
		{
			result = path.FullPath;
			return true;
		}

		var driveName = path.HasSegments ? path.Segments[0] : null;
		if(string.IsNullOrEmpty(driveName))
		{
			result = null;
			return false;
		}

		if(driveName.Length > 1)
		{
			var drives = System.IO.DriveInfo.GetDrives();
			var matched = false;

			foreach(var drive in drives)
			{
				matched = MatchDriver(drive, driveName);

				if(matched)
				{
					driveName = drive.Name[0].ToString();
					break;
				}
			}

			if(!matched)
			{
				result = null;
				return false;
			}
		}

		if(path.Segments.Length > 1)
			result = driveName + ":/" + string.Join('/', path.Segments, 1, path.Segments.Length - 1);
		else
			result = driveName + ":/";

		return true;
	}

	private static bool MatchDriver(System.IO.DriveInfo drive, string driveName)
	{
		if(drive == null)
			return false;

		try
		{
			return string.Equals(drive.VolumeLabel, driveName, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
	#endregion

	#region 嵌套子类
	internal sealed class LocalDirectoryProvider : IDirectory
	{
		#region 单例字段
		public static readonly LocalDirectoryProvider Instance = new();
		#endregion

		#region 私有构造
		private LocalDirectoryProvider() { }
		#endregion

		#region 公共方法
		public ValueTask<bool> CreateAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellation) : ValueTask.FromResult(this.Create(path, properties));
		public bool Create(string path, IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			var fullPath = GetLocalPath(path);

			if(System.IO.Directory.Exists(fullPath))
				return false;

			return System.IO.Directory.CreateDirectory(fullPath) != null;
		}

		public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellation) : ValueTask.FromResult(this.Delete(path));
		public bool Delete(string path)
		{
			var fullPath = GetLocalPath(path);

			try
			{
				System.IO.Directory.Delete(fullPath, true);
				return true;
			}
			catch(DirectoryNotFoundException)
			{
				return false;
			}
		}

		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return ValueTask.FromCanceled(cancellation);

			this.Move(source, destination);
			return ValueTask.CompletedTask;
		}

		public void Move(string source, string destination)
		{
			var sourcePath = GetLocalPath(source);
			var destinationPath = GetLocalPath(destination);
			System.IO.Directory.Move(sourcePath, destinationPath);
		}

		public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellation) : ValueTask.FromResult(this.Exists(path));
		public bool Exists(string path)
		{
			var fullPath = GetLocalPath(path);
			return System.IO.Directory.Exists(fullPath);
		}

		public ValueTask<DirectoryInfo> GetInfoAsync(string path, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<DirectoryInfo>(cancellation) : ValueTask.FromResult(this.GetInfo(path));
		public DirectoryInfo GetInfo(string path)
		{
			var fullPath = GetLocalPath(path);
			var info = new System.IO.DirectoryInfo(fullPath);

			if(info == null || !info.Exists)
				return null;

			return new DirectoryInfo(info.FullName, info.CreationTime, info.LastWriteTime, LocalFileSystem.Instance.GetUrl(path));
		}

		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();

		public IEnumerable<PathInfo> GetChildren(string path) => this.GetChildren(path, null, false);
		public IEnumerable<PathInfo> GetChildren(string path, string pattern, bool recursive = false)
		{
			var fullPath = GetLocalPath(path);
			var entries = Search(pattern, p => System.IO.Directory.EnumerateFileSystemEntries(fullPath, p, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			return new InfoEnumerator<PathInfo>(entries);
		}

		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string path, CancellationToken cancellation = default) => this.GetChildrenAsync(path, null, false, cancellation);
		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string path, string pattern, CancellationToken cancellation = default) => this.GetChildrenAsync(path, pattern, false, cancellation);
		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string path, string pattern, bool recursive, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return Zongsoft.Collections.Enumerable.Asynchronize<PathInfo>([]);

			var fullPath = GetLocalPath(path);
			var paths = Search(pattern, p => System.IO.Directory.EnumerateFileSystemEntries(fullPath, p, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			return Zongsoft.Collections.Enumerable.Asynchronize(
				paths.Select(path => new PathInfo(path))
			);
		}

		public IEnumerable<DirectoryInfo> GetDirectories(string path) => this.GetDirectories(path, null, false);
		public IEnumerable<DirectoryInfo> GetDirectories(string path, string pattern, bool recursive = false)
		{
			var fullPath = GetLocalPath(path);
			var entries = Search(pattern, p => System.IO.Directory.EnumerateDirectories(fullPath, p, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			return new InfoEnumerator<DirectoryInfo>(entries);
		}

		public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string path, CancellationToken cancellation = default) => this.GetDirectoriesAsync(path, null, false, cancellation);
		public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string path, string pattern, CancellationToken cancellation = default) => this.GetDirectoriesAsync(path, pattern, false, cancellation);
		public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string path, string pattern, bool recursive, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return Zongsoft.Collections.Enumerable.Asynchronize<DirectoryInfo>([]);

			var fullPath = GetLocalPath(path);
			var paths = Search(pattern, p => System.IO.Directory.EnumerateDirectories(fullPath, p, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			return Zongsoft.Collections.Enumerable.Asynchronize(
				paths.Select(path => new System.IO.DirectoryInfo(path))
				.Select(info => new DirectoryInfo(info.FullName, info.CreationTime, info.LastWriteTime))
			);
		}

		public IEnumerable<FileInfo> GetFiles(string path) => this.GetFiles(path, null, false);
		public IEnumerable<FileInfo> GetFiles(string path, string pattern, bool recursive = false)
		{
			var fullPath = GetLocalPath(path);
			var entries = Search(pattern, p => System.IO.Directory.EnumerateFiles(fullPath, p, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			return new InfoEnumerator<FileInfo>(entries);
		}

		public IAsyncEnumerable<FileInfo> GetFilesAsync(string path, CancellationToken cancellation = default) => this.GetFilesAsync(path, null, false, cancellation);
		public IAsyncEnumerable<FileInfo> GetFilesAsync(string path, string pattern, CancellationToken cancellation = default) => this.GetFilesAsync(path, pattern, false, cancellation);
		public IAsyncEnumerable<FileInfo> GetFilesAsync(string path, string pattern, bool recursive, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return Zongsoft.Collections.Enumerable.Asynchronize<FileInfo>([]);

			var fullPath = GetLocalPath(path);
			var paths = Search(pattern, p => System.IO.Directory.EnumerateFiles(fullPath, p, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			return Zongsoft.Collections.Enumerable.Asynchronize(
				paths.Select(path => new System.IO.FileInfo(path))
				.Select(info => new FileInfo(info.FullName, info.Length, info.CreationTime, info.LastWriteTime))
			);
		}
		#endregion

		#region 私有方法
		private static readonly Regex _regex = new(@"(?<delimiter>[/\|\\]).+\k<delimiter>", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private static IEnumerable<string> Search(string pattern, Func<string, IEnumerable<string>> searcher, Predicate<string> filter = null)
		{
			var regularPattern = string.Empty;
			IEnumerable<string> result;

			if(string.IsNullOrEmpty(pattern))
			{
				result = searcher("*");
			}
			else
			{
				var matches = _regex.Matches(pattern);

				if(matches == null || matches.Count <= 0)
				{
					result = searcher(pattern);
				}
				else
				{
					var position = 0;

					foreach(Match match in matches)
					{
						if(match.Index > 0)
							regularPattern += EscapePattern(pattern.Substring(position, match.Index - position));

						regularPattern += match.Value.Substring(1, match.Value.Length - 2);

						//移动当前指针位置
						position = match.Index + match.Length;
					}

					if(position < pattern.Length)
						regularPattern += EscapePattern(pattern.Substring(position));

					result = searcher("*");

					//设置正则匹配模式为完整匹配
					if(regularPattern != null && regularPattern.Length > 0)
						regularPattern = "^" + regularPattern + "$";
				}
			}

			if(result == null)
				yield break;

			foreach(var item in result)
			{
				if(string.IsNullOrEmpty(regularPattern))
				{
					if(filter == null || filter(item))
						yield return item;
				}
				else
				{
					var fileName = System.IO.Path.GetFileName(item);

					if(Regex.IsMatch(fileName, regularPattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture) && (filter == null || filter(item)))
						yield return item;
				}
			}
		}

		internal static string EscapePattern(string pattern)
		{
			if(string.IsNullOrWhiteSpace(pattern))
				return pattern;

			string result = string.Empty;

			foreach(var chr in pattern)
			{
				result += chr switch
				{
					'.' or '^' or '*' or '?' or '+' or '-' or '|' or '\\' or '(' or ')' or '[' or ']' or '{' or '}' or '<' or '>' => $"\\{chr}",
					_ => chr,
				};
			}

			return result;
		}
		#endregion

		#region 嵌套子类
		private class InfoEnumerator<T>(IEnumerable<string> source) : IEnumerable<T>, IEnumerator<T> where T : PathInfo
		{
			#region 私有字段
			private IEnumerator<string> _source = source?.GetEnumerator();
			#endregion

			#region 公共成员
			public T Current
			{
				get
				{
					if(_source == null)
						return null;

					var item = _source.Current;

					if(string.IsNullOrEmpty(item))
						return null;

					if(typeof(T) == typeof(FileInfo))
						return LocalFileSystem.Instance.File.GetInfo(item) as T;
					else if(typeof(T) == typeof(DirectoryInfo))
						return LocalFileSystem.Instance.Directory.GetInfo(item) as T;
					else if(typeof(T) == typeof(PathInfo))
						return (T)new PathInfo(item);

					throw new InvalidOperationException();
				}
			}

			public bool MoveNext()
			{
				if(_source != null)
					return _source.MoveNext();

				return false;
			}

			public void Reset() => _source?.Reset();
			#endregion

			#region 显式实现
			object System.Collections.IEnumerator.Current => this.Current;
			void IDisposable.Dispose() => _source = null;
			#endregion

			#region 枚举遍历
			public IEnumerator<T> GetEnumerator() => this;
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this;
			#endregion
		}
		#endregion
	}

	private sealed class LocalFileProvider : IFile
	{
		#region 单例字段
		public static readonly LocalFileProvider Instance = new();
		#endregion

		#region 私有构造
		private LocalFileProvider() { }
		#endregion

		#region 公共方法
		public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellation) : ValueTask.FromResult(this.Delete(path));
		public bool Delete(string path)
		{
			var fullPath = GetLocalPath(path);

			try
			{
				System.IO.File.Delete(fullPath);
				return true;
			}
			catch(FileNotFoundException)
			{
				return false;
			}
		}

		public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellation) : ValueTask.FromResult(this.Exists(path));
		public bool Exists(string path)
		{
			var fullPath = GetLocalPath(path);
			return System.IO.File.Exists(fullPath);
		}

		public void Copy(string source, string destination, bool overwrite = true)
		{
			var sourcePath = GetLocalPath(source);
			var destinationPath = GetLocalPath(destination);
			System.IO.File.Copy(sourcePath, destinationPath, overwrite);
		}

		public ValueTask CopyAsync(string source, string destination, bool overwrite, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return ValueTask.FromCanceled(cancellation);

			this.Copy(source, destination, overwrite);
			return ValueTask.CompletedTask;
		}

		public void Move(string source, string destination)
		{
			var sourcePath = GetLocalPath(source);
			var destinationPath = GetLocalPath(destination);
			System.IO.File.Move(sourcePath, destinationPath);
		}

		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return ValueTask.FromCanceled(cancellation);

			this.Move(source, destination);
			return ValueTask.CompletedTask;
		}

		public ValueTask<FileInfo> GetInfoAsync(string path, CancellationToken cancellation = default) => cancellation.IsCancellationRequested ? ValueTask.FromCanceled<FileInfo>(cancellation) : ValueTask.FromResult(this.GetInfo(path));
		public FileInfo GetInfo(string path)
		{
			var fullPath = GetLocalPath(path);
			var info = new System.IO.FileInfo(fullPath);

			if(info == null || !info.Exists)
				return null;

			return new FileInfo(info.FullName, info.Length, info.CreationTime, info.LastWriteTime, LocalFileSystem.Instance.GetUrl(path));
		}

		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();

		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			var fullPath = GetLocalPath(path);
			return System.IO.File.Open(fullPath, mode, access, share);
		}

		public ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
		{
			if(cancellation.IsCancellationRequested)
				return ValueTask.FromCanceled<Stream>(cancellation);

			var stream = this.Open(path, mode, access, share, properties);
			return ValueTask.FromResult(stream);
		}
		#endregion
	}
	#endregion
}
