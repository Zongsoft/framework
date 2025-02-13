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

namespace Zongsoft.Configuration.Profiles;

public class ProfileDirective : ProfileComment
{
	#region 私有构造
	internal ProfileDirective(Profile profile, string name, string argument, int lineNumber = -1) : base(profile, string.IsNullOrEmpty(argument) ? $"@{name}" : $"@{name} {argument}", lineNumber)
	{
		this.Name = name;
		this.Argument = argument;
	}

	internal ProfileDirective(ProfileSection section, string name, string argument, int lineNumber = -1) : base(section, string.IsNullOrEmpty(argument) ? $"@{name}" : $"@{name} {argument}", lineNumber)
	{
		this.Name = name;
		this.Argument = argument;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Argument { get; }
	#endregion

	#region 解析方法
	internal static (string name, string argument) Parse(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty || text[0] != '@' || text.Length <= 1)
			return default;

		for(int i = 1; i < text.Length; i++)
		{
			if(text[i] == ' ' || text[i] == '\t')
				return i > 1 ? (text[1..i].ToString(), text[(i+ 1)..].Trim().ToString()) : default;

			if(!ValidName(text[i], i - 1))
				return default;
		}

		return (text[1..].ToString(), null);

		static bool ValidName(char character, int position) => position == 0 ?
			char.IsLetterOrDigit(character) || character == '_' :
			char.IsLetterOrDigit(character) || character == '_' || character == '-' || character == '.';
	}
	#endregion
}
