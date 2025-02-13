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

namespace Zongsoft.Configuration.Profiles
{
	public class ProfileComment : ProfileItem
	{
		#region 私有变量
		private readonly string _text;
		private readonly string[] _lines;
		#endregion

		#region 构造函数
		internal ProfileComment(Profile profile, string text, int lineNumber = -1) : base(profile, lineNumber)
		{
			_text = text ?? string.Empty;
			_lines = string.IsNullOrEmpty(text) ? [] : text.Split('\n');
		}

		internal ProfileComment(ProfileSection section, string text, int lineNumber = -1) : base(section, lineNumber)
		{
			_text = text ?? string.Empty;
			_lines = string.IsNullOrEmpty(text) ? [] : text.Split('\n');
		}
		#endregion

		#region 公共属性
		public bool IsEmpty => string.IsNullOrEmpty(_text);
		public string Text => _text;
		public string[] Lines => _lines;
		public override ProfileItemType ItemType => ProfileItemType.Comment;
		#endregion

		#region 重写方法
		public override string ToString() => _text;
		#endregion

		#region 静态方法
		internal static ProfileComment GetComment(Profile profile, string text, int lineNumber = -1)
		{
			(var name, var argument) = ProfileDirective.Parse(text);

			if(string.IsNullOrEmpty(name))
				return new ProfileComment(profile, text, lineNumber);
			else
				return new ProfileDirective(profile, name, argument, lineNumber);
		}

		internal static ProfileComment GetComment(ProfileSection section, string text, int lineNumber = -1)
		{
			(var name, var argument) = ProfileDirective.Parse(text);

			if(string.IsNullOrEmpty(name))
				return new ProfileComment(section, text, lineNumber);
			else
				return new ProfileDirective(section, name, argument, lineNumber);
		}
		#endregion
	}
}
