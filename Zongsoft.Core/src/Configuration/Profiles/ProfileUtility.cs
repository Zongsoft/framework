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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

internal static class ProfileUtility
{
	#region 枚举定义
	internal enum LineType
	{
		Blank,
		Entry,
		Section,
		Comment,
	}
	#endregion

	#region 内部属性
	internal static StringComparer PathComparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
	#endregion

	#region 枚举方法
	public static IReadOnlyList<ProfileItem> GetItems(this Profile profile)
	{
		if(profile == null)
			return [];

		var list = new List<ProfileItem>(profile.Comments.Count + profile.Entries.Count + profile.Sections.Count);

		list.AddRange(profile.Comments);
		list.AddRange(profile.Entries);
		list.AddRange(profile.Sections);

		return list;
	}

	public static IReadOnlyList<ProfileItem> GetItems(this ProfileSection section)
	{
		if(section == null)
			return [];

		var list = new List<ProfileItem>(section.Comments.Count + section.Entries.Count + section.Sections.Count);

		list.AddRange(section.Comments);
		list.AddRange(section.Entries);
		list.AddRange(section.Sections);

		return list;
	}
	#endregion

	#region 语法解析
	internal static bool IsDirectiveName(ReadOnlySpan<char> name)
	{
		if(name.IsEmpty || !char.IsLetter(name[0]) && name[0] != '_')
			return false;

		foreach(var character in name)
		{
			if(!char.IsLetterOrDigit(character) && character is not ('_' or '-' or '.'))
				return false;
		}

		return true;
	}

	internal static bool TryGetDirective(ReadOnlySpan<char> text, out string name, out string argument)
	{
		name = null;
		argument = null;

		if(text.Length < 2 || text[0] != '@')
			return false;

		text = text[1..];
		var separator = text.IndexOfAny(' ', '\t');
		var token = separator < 0 ? text : text[..separator];

		if(!IsDirectiveName(token))
			return false;

		name = token.ToString();
		argument = separator < 0 ? string.Empty : text[(separator + 1)..].Trim().ToString();
		return true;
	}

	internal static LineType ParseLine(ReadOnlySpan<char> text, out string result)
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
			throw new ProfileException(Properties.Resources.InvalidFormat_Message);

		result = text.ToString();
		return LineType.Entry;
	}
	#endregion

	#region 路径解析
	internal static string GetIdentity(string path)
	{
		path = Path.GetFullPath(path);

		var root = Path.GetPathRoot(path);
		var current = root;

		foreach(var part in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
		{
			current = Path.Combine(current, part);
			FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);

			if(info.LinkTarget != null)
				current = info.ResolveLinkTarget(true)?.FullName ?? throw new IOException(string.Format(Properties.Resources.Profiles_LinkResolutionFailed_Message, current));
		}

		return current;
	}
	#endregion
}
