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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.IO;

/// <summary>提供本地目录的分段通配搜索，不使用虚拟文件系统。</summary>
public static class Searcher
{
	#region 静态字段
	private static readonly StringComparer _comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
	#endregion

	#region 公共方法
	/// <summary>搜索匹配的本地文件或目录；逻辑名称与实际读取目标分别保存在匹配结果中。</summary>
	/// <param name="directory">解析相对搜索模式的本地目录。</param>
	/// <param name="pattern">支持 *、? 和独立段 ** 的相对路径模式。</param>
	/// <param name="target">要返回的结果类型，默认为文件和目录。</param>
	/// <param name="cancellation">取消搜索的通知令牌。</param>
	public static IEnumerable<Match> Search(this System.IO.DirectoryInfo directory, string pattern, Target target = Target.Both, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentException.ThrowIfNullOrEmpty(pattern);

		if(System.IO.Path.IsPathRooted(pattern))
			throw new ArgumentException(Properties.Resources.Searcher_RelativePatternRequired_Message, nameof(pattern));

		var wildcard = false;
		foreach(var part in Split(pattern))
		{
			if(wildcard && part == "..")
				throw new ArgumentException(Properties.Resources.Searcher_ParentAfterWildcard_Message, nameof(pattern));

			wildcard |= IsWildcard(part);
		}

		// 固定逻辑路径，但不能从固定前缀开始遍历，否则会绕过模式中间的目录链接。
		var directoryPath = directory.FullName;
		var path = System.IO.Path.GetFullPath(pattern, directoryPath);
		var parts = Split(System.IO.Path.GetRelativePath(directoryPath, path));
		var index = path.IndexOfAny(['*', '?']);
		var basis = index < 0 ? System.IO.Path.GetDirectoryName(path) : path[..(path.LastIndexOfAny(Separators(), index) + 1)];
		var origin = new System.IO.DirectoryInfo(basis ?? path);

		return Enumerate(directoryPath, parts, origin, target, cancellation);
	}
	#endregion

	#region 搜索方法
	private static IEnumerable<Match> Enumerate(string directory, string[] parts, System.IO.DirectoryInfo origin, Target target, CancellationToken cancellation)
	{
		var stack = new Stack<State>();
		var visited = new Dictionary<string, HashSet<int>>(_comparer);
		var matches = new Dictionary<string, Match>(_comparer);
		stack.Push(new(directory, 0, []));

		while(stack.TryPop(out var state))
		{
			cancellation.ThrowIfCancellationRequested();

			if(!visited.TryGetValue(state.Path, out var positions))
				visited.Add(state.Path, positions = []);

			if(!positions.Add(state.Index))
				continue;

			var info = GetInfo(state.Path);
			if(info == null)
				continue;

			if(state.Index == parts.Length)
			{
				Add(info, state.Captures);
				continue;
			}

			var part = parts[state.Index];
			var linked = info.LinkTarget != null;
			var descend = info is System.IO.DirectoryInfo && (!linked || _comparer.Equals(state.Path, directory));

			if(part == "**")
			{
				if(descend)
				{
					var children = GetChildren(info.FullName, cancellation);
					for(var i = children.Length - 1; i >= 0; i--)
					{
						var child = children[i];
						var capture = Join(state.Recursive, child.Name);

						if(child is System.IO.DirectoryInfo)
							stack.Push(new(child.FullName, state.Index, state.Captures, capture));
						else if(state.Index == parts.Length - 1)
							Add(child, [.. state.Captures, capture]);
					}
				}

				// 先走零层分支，使靠前的 ** 优先消费较少层级。
				if(descend || parts[state.Index..].All(value => value == "**"))
					stack.Push(new(state.Path, state.Index + 1, [.. state.Captures, state.Recursive]));

				continue;
			}

			if(!descend)
				continue;

			if(!IsWildcard(part))
			{
				stack.Push(new(System.IO.Path.GetFullPath(part, state.Path), state.Index + 1, state.Captures));
				continue;
			}

			var entries = GetChildren(state.Path, cancellation);
			for(var i = entries.Length - 1; i >= 0; i--)
			{
				var entry = entries[i];
				if(System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(part, entry.Name, OperatingSystem.IsWindows()))
					stack.Push(new(entry.FullName, state.Index + 1, [.. state.Captures, entry.Name]));
			}
		}

		cancellation.ThrowIfCancellationRequested();
		foreach(var match in matches.Values.OrderBy(item => Normalize(System.IO.Path.GetRelativePath(origin.FullName, item.Path)), StringComparer.Ordinal))
		{
			cancellation.ThrowIfCancellationRequested();
			yield return match;
		}

		void Add(FileSystemInfo info, string[] captures)
		{
			cancellation.ThrowIfCancellationRequested();

			// 先筛选类型，搜索文件时无需解析被跳过的目录链接。
			if(!IsTarget(info, target))
				return;

			if(matches.ContainsKey(info.FullName))
				return;

			var result = Resolve(info.FullName, cancellation);
			if(result != null && IsTarget(result, target))
				matches.Add(info.FullName, new(info.FullName, result, origin, captures));
		}
	}
	#endregion

	#region 辅助方法
	private static bool IsTarget(FileSystemInfo info, Target target) => target == Target.Both || target == (info is System.IO.DirectoryInfo ? Target.Directories : Target.Files);
	private static bool IsWildcard(string text) => text.IndexOfAny(['*', '?']) >= 0;
	private static char[] Separators() => System.IO.Path.DirectorySeparatorChar == System.IO.Path.AltDirectorySeparatorChar ? [System.IO.Path.DirectorySeparatorChar] : [System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar];
	private static string[] Split(string path) => path.Split(Separators(), StringSplitOptions.RemoveEmptyEntries);
	private static string Normalize(string path) => System.IO.Path.DirectorySeparatorChar == '/' ? path : path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
	private static string Join(string path, string name) => string.IsNullOrEmpty(path) ? name : System.IO.Path.Combine(path, name);

	private static FileSystemInfo GetInfo(string path)
	{
		try
		{
			return (File.GetAttributes(path) & FileAttributes.Directory) != 0 ? new System.IO.DirectoryInfo(path) : new System.IO.FileInfo(path);
		}
		catch(FileNotFoundException)
		{
			return null;
		}
		catch(DirectoryNotFoundException)
		{
			return null;
		}
	}

	private static FileSystemInfo[] GetChildren(string path, CancellationToken cancellation)
	{
		try
		{
			var entries = new List<FileSystemInfo>();
			foreach(var entry in new System.IO.DirectoryInfo(path).EnumerateFileSystemInfos())
			{
				cancellation.ThrowIfCancellationRequested();
				entries.Add(entry);
			}

			return entries.OrderBy(entry => entry.Name, StringComparer.Ordinal).ToArray();
		}
		catch(DirectoryNotFoundException)
		{
			return [];
		}
	}

	private static FileSystemInfo Resolve(string path, CancellationToken cancellation)
	{
		var candidate = path;
		var linked = false;
		var visited = new HashSet<string>(_comparer);

		// 目标路径的祖先也可能是链接；每次替换后从根重新解析，不能只解析最后一项。
		while(visited.Add(candidate))
		{
			var current = System.IO.Path.GetPathRoot(candidate);
			var parts = Split(candidate[current.Length..]);
			var redirected = false;

			for(var i = 0; i < parts.Length; i++)
			{
				cancellation.ThrowIfCancellationRequested();
				current = System.IO.Path.Combine(current, parts[i]);
				var info = GetInfo(current);

				if(info == null)
				{
					if(linked)
						throw new FileNotFoundException(Properties.Resources.Searcher_LinkTargetMissing_Message, path);

					return null;
				}

				if(info.LinkTarget == null)
					continue;

				var target = info.ResolveLinkTarget(true) ?? throw new FileNotFoundException(Properties.Resources.Searcher_LinkTargetMissing_Message, path);
				candidate = System.IO.Path.Combine([target.FullName, .. parts.Skip(i + 1)]);

				linked = true;
				redirected = true;
				break;
			}

			if(!redirected)
				return GetInfo(current);
		}

		throw new IOException($"The selected link contains a cycle: '{path}'.");
	}
	#endregion

	#region 嵌套类型
	/// <summary>指定本地搜索的文件系统条目类型。</summary>
	public enum Target
	{
		/// <summary>文件和目录。</summary>
		Both = 0,
		/// <summary>文件。</summary>
		Files = 1,
		/// <summary>目录。</summary>
		Directories = 2,
	}

	/// <summary>表示逻辑路径的匹配及其实际读取目标。</summary>
	public readonly struct Match
	{
		internal Match(string path, FileSystemInfo result, System.IO.DirectoryInfo origin, string[] captures)
		{
			this.Path = path;
			this.Result = result;
			this.Origin = origin;
			this.Captures = Array.AsReadOnly((string[])captures.Clone());
		}

		/// <summary>获取保留链接名称的逻辑绝对路径。</summary>
		public string Path { get; }
		/// <summary>获取解析链接后的实际文件或目录。</summary>
		public FileSystemInfo Result { get; }
		/// <summary>获取逻辑模式的固定目录前缀。</summary>
		public System.IO.DirectoryInfo Origin { get; }
		/// <summary>获取按模式段顺序排列的只读通配捕获。</summary>
		public IReadOnlyList<string> Captures { get; }

		/// <summary>返回匹配的文件或目录的路径，如果匹配失败返回空字符串。</summary>
		public override string ToString() => this.Result == null ? string.Empty : this.Result.FullName;

		/// <summary>判断匹配目标是否为文件，不执行文件系统访问。</summary>
		public bool IsFile(out System.IO.FileInfo file)
		{
			file = this.Result as System.IO.FileInfo;
			return file != null;
		}

		/// <summary>判断匹配目标是否为目录，不执行文件系统访问。</summary>
		public bool IsDirectory(out System.IO.DirectoryInfo directory)
		{
			directory = this.Result as System.IO.DirectoryInfo;
			return directory != null;
		}
	}
	#endregion

	#region 私有结构
	private readonly record struct State(string Path, int Index, string[] Captures, string Recursive = "");
	#endregion
}
