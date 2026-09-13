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
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

internal sealed class ProfileReader
{
	#region 枚举定义
	private enum LineType
	{
		Blank,
		Entry,
		Section,
		Comment,
	}
	#endregion

	#region 成员字段
	private readonly HashSet<string> _active = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
	private readonly List<string> _paths = [];
	#endregion

	#region 构造函数
	public ProfileReader(ProfileOptions options)
	{
		this.Options = options?.Clone() ?? new ProfileOptions(false);
	}
	#endregion

	#region 公共属性
	public ProfileOptions Options { get; }
	#endregion

	#region 读取方法
	public Profile Read(string path, int maximumDepth = int.MaxValue, bool optional = false, Action<string> loading = null, Action<Profile> loaded = null, ProfileReadingContext context = null)
	{
		if(string.IsNullOrWhiteSpace(path))
			throw new ArgumentNullException(nameof(path));

		if(context != null && !Path.IsPathFullyQualified(path))
		{
			if(string.IsNullOrEmpty(context.Profile.FilePath))
				throw new ProfileException(string.Format(Properties.Resources.Profiles_RelativeImportRequiresFile, path, context.LineNumber + 1));

			path = Path.Combine(Path.GetDirectoryName(context.Profile.FilePath), path);
		}

		path = Path.GetFullPath(path);
		FileStream stream;

		try
		{
			stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
		catch(FileNotFoundException) when(optional)
		{
			return null;
		}
		catch(DirectoryNotFoundException) when(optional)
		{
			return null;
		}

		return this.ReadCore(stream, null, path, maximumDepth, loading, loaded, context);
	}

	public Profile Read(Stream stream, Encoding encoding, Action<string> loading = null, Action<Profile> loaded = null) =>
		this.ReadCore(stream, encoding, stream is FileStream file ? file.Name : string.Empty, int.MaxValue, loading, loaded, null);
	#endregion

	#region 私有方法
	private Profile ReadCore(Stream stream, Encoding encoding, string path, int maximumDepth, Action<string> loading, Action<Profile> loaded, ProfileReadingContext context)
	{
		using(stream)
		{
			var identity = string.IsNullOrEmpty(path) ? null : GetIdentity(path);

			if(_paths.Count >= maximumDepth)
				throw this.CreateException(Properties.Resources.Profiles_MaximumDepth, path, context);

			if(identity != null && !_active.Add(identity))
				throw this.CreateException(Properties.Resources.Profiles_CircularImport, path, context);

			_paths.Add(path);

			try
			{
				loading?.Invoke(path);

				var profile = this.Parse(stream, encoding);

				loaded?.Invoke(profile);

				return profile;
			}
			finally
			{
				_paths.RemoveAt(_paths.Count - 1);

				if(identity != null)
					_active.Remove(identity);
			}
		}
	}

	private Profile Parse(Stream stream, Encoding encoding)
	{
		List<int> blanks = [];
		Profile profile = new Profile(stream is FileStream fileStream ? fileStream.Name : string.Empty);
		ProfileReadingContext context = new ProfileReadingContext(profile, stream, this);
		var options = this.Options;

		using(var reader = new StreamReader(stream, encoding ?? Encoding.UTF8))
		{
			string text;
			context.LineNumber = 0;

			while((text = reader.ReadLine()) != null)
			{
				//解析读取到的行文本
				switch(ParseLine(text, out var content))
				{
					case LineType.Blank:
						if(options != null && options.ReservedBlanks)
							blanks.Add(context.LineNumber);
						break;
					case LineType.Section:
						var parts = content.Split(' ', '\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

						if(parts == null || parts.Length == 0)
							context.Section = null;
						else
						{
							var sections = profile.Sections;

							for(int i = 0; i < parts.Length; i++)
							{
								if(sections.TryGetValue(parts[i], out var section))
									context.Section = section;
								else
									context.Section = sections.Add(parts[i], context.LineNumber);

								sections = context.Section.Sections;
							}
						}

						break;
					case LineType.Entry:
						var index = content.IndexOf('=');

						if(context.Section == null)
						{
							if(index < 0)
								profile.Entries.Add(context.LineNumber, content);
							else
								profile.Entries.Add(context.LineNumber, content[..index], content[(index + 1)..]);
						}
						else
						{
							if(index < 0)
								context.Section.Entries.Add(context.LineNumber, content);
							else
								context.Section.Entries.Add(context.LineNumber, content[..index], content[(index + 1)..]);
						}

						break;
					case LineType.Comment:
						var comment = context.Section == null ?
							profile.Comments.Add(content, context.LineNumber) :
							context.Section.Comments.Add(content, context.LineNumber);

						//如果是指令项则调用指令的读方法
						if(comment is ProfileDirective directive)
							context.OnRead(options, directive.Name, directive.Argument);

						break;
				}

				//递增行号
				context.LineNumber++;
			}
		}

		//更新配置文件中的空行集
		profile.Blanks = [.. blanks];

		//返回加载成功的配置文件
		return profile;
	}

	private static LineType ParseLine(ReadOnlySpan<char> text, out string result)
	{
		result = null;

		if(text.IsEmpty || text.IsWhiteSpace())
			return LineType.Blank;

		text = text.Trim();

		if(text[0] == ';' || text[0] == '#')
		{
			result = text[1..].ToString();
			return LineType.Comment;
		}

		if(text[0] == '[' && text[^1] == ']')
		{
			result = text[1..^1].ToString();
			return LineType.Section;
		}

		if(text[0] == '=')
			throw new ProfileException("Invalid format.");

		result = text.ToString();
		return LineType.Entry;
	}

	private ProfileException CreateException(string message, string path, ProfileReadingContext context) => new(string.Format(
		message, string.Join(" -> ", _paths) + " -> " + path, context?.Profile.FilePath, (context?.LineNumber ?? -1) + 1));

	private static string GetIdentity(string path)
	{
		path = Path.GetFullPath(path);

		var root = Path.GetPathRoot(path);
		var current = root;

		foreach(var part in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
		{
			current = Path.Combine(current, part);
			FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);

			if(info.LinkTarget != null)
				current = info.ResolveLinkTarget(true)?.FullName ?? throw new IOException(string.Format(Properties.Resources.Profiles_LinkResolutionFailed, current));
		}

		return current;
	}
	#endregion
}
