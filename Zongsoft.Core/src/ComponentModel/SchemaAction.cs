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
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace Zongsoft.ComponentModel
{
	public class SchemaAction : IEquatable<SchemaAction>
	{
		#region 成员变量
		private string _name;
		private string _title;
		private string _description;
		private bool _visible;
		private string[] _alias;
		#endregion

		#region 构造函数
		public SchemaAction(string name) : this(name, null, null) { }
		public SchemaAction(string name, string title) : this(name, title, null) { }
		public SchemaAction(string name, string title, string description)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_title = string.IsNullOrWhiteSpace(title) ? _name : title;
			_description = description;
			_visible = true;
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get => _name;
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		[TypeConverter(typeof(AliasConverter))]
		public string[] Alias
		{
			get => _alias;
			set => _alias = value;
		}

		public string Title
		{
			get => _title;
			set => _title = value;
		}

		public string Description
		{
			get => _description;
			set => _description = value;
		}

		public bool Visible
		{
			get => _visible;
			set => _visible = value;
		}
		#endregion

		#region 重写方法
		public bool Equals(SchemaAction action)
		{
			if(action == null)
				return false;

			return string.Equals(_name, action.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return string.Equals(_name, ((SchemaAction)obj).Name, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode()
		{
			if(_name == null)
				return base.GetHashCode();

			return _name.ToLowerInvariant().GetHashCode();
		}

		public override string ToString()
		{
			if(_name == null)
				return string.Empty;

			return _name;
		}
		#endregion

		#region 嵌套子类
		private class AliasConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				if(sourceType == typeof(string))
					return true;

				return base.CanConvertFrom(context, sourceType);
			}

			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
			{
				if(destinationType == typeof(string))
					return true;

				return base.CanConvertTo(context, destinationType);
			}

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if(value is string text)
					return Zongsoft.Common.StringExtension.Slice(text, chr => chr == ',' || chr == ';' || chr == '|').ToArray();

				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(destinationType == typeof(string))
					return value == null ? null : string.Join(",", (string[])value);

				return base.ConvertTo(context, culture, value, destinationType);
			}
		}
		#endregion
	}
}
