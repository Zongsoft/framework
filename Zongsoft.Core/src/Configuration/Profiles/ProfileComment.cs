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
		#endregion

		#region 构造函数
		protected ProfileComment(string text, int lineNumber = -1) : base(lineNumber)
		{
			_text = text ?? string.Empty;
		}
		#endregion

		#region 公共属性
		public bool IsEmpty => string.IsNullOrEmpty(_text);
		public string Text => _text;
		public string[] Lines => string.IsNullOrEmpty(_text) ? [] : _text.Split('\n');
		public override ProfileItemType ItemType => ProfileItemType.Comment;
		#endregion

		#region 重写方法
		public override string ToString() => _text;
		#endregion

		#region 静态方法
		public static ProfileComment GetComment(string text, int lineNumber = -1)
		{
			(var name, var argument) = ProfileDirective.Parse(text);

			if(string.IsNullOrEmpty(name))
				return new ProfileComment(text, lineNumber);
			else
				return new ProfileDirective(name, argument, lineNumber);
		}
		#endregion
	}
}
