/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Options.Profiles
{
	[Serializable]
	public class ProfileEntry : ProfileItem
	{
		#region 构造函数
		private string _name;
		private string _value;
		#endregion

		#region 构造函数
		public ProfileEntry(string name, string value = null) : this(-1, name, value)
		{
		}

		public ProfileEntry(int lineNumber, string name, string value = null) : base(lineNumber)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			_name = name.Trim();

			if(value != null)
				_value = value.Trim();
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				return _name;
			}
		}

		public string Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}

		public ProfileSection Section
		{
			get
			{
				return base.Owner as ProfileSection;
			}
		}

		public override ProfileItemType ItemType
		{
			get
			{
				return ProfileItemType.Entry;
			}
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(_value == null)
				return _name;

			return string.Format("{0}={1}", _name, _value);
		}
		#endregion
	}
}
