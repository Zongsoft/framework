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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zongsoft.Configuration.Profiles
{
	public class ProfileComment : ProfileItem
	{
		#region 私有变量
		private readonly StringBuilder _text;
		#endregion

		#region 构造函数
		public ProfileComment(string text, int lineNumber = -1) : base(lineNumber)
		{
			if(string.IsNullOrEmpty(text))
				_text = new StringBuilder();
			else
				_text = new StringBuilder(text);
		}
		#endregion

		#region 公共属性
		public string Text
		{
			get
			{
				return _text.ToString();
			}
		}

		public string[] Lines
		{
			get
			{
				return _text.ToString().Split('\r', '\n');
			}
		}

		public override ProfileItemType ItemType
		{
			get
			{
				return ProfileItemType.Comment;
			}
		}
		#endregion

		#region 公共方法
		public void Append(string text)
		{
			if(string.IsNullOrEmpty(text))
				return;

			_text.Append(text);
		}

		public void AppendFormat(string format, params object[] args)
		{
			if(string.IsNullOrEmpty(format))
				return;

			_text.AppendFormat(format, args);
		}

		public void AppendLine(string text)
		{
			if(text == null)
				_text.AppendLine();
			else
				_text.AppendLine(text);
		}
		#endregion
	}
}
