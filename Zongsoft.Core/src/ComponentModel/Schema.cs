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

namespace Zongsoft.ComponentModel
{
	[System.Reflection.DefaultMember(nameof(Actions))]
	public class Schema : IEquatable<Schema>
	{
		#region 成员变量
		private string _name;
		private string _title;
		private string _description;
		private bool? _visible;
		private SchemaActionCollection _actions;
		#endregion

		#region 构造函数
		public Schema()
		{
		}

		public Schema(string name) : this(name, name, string.Empty)
		{
		}

		public Schema(string name, string title) : this(name, title, string.Empty)
		{
		}

		public Schema(string name, string title, string description)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Replace('-', '.');
			_title = string.IsNullOrEmpty(title) ? _name : title;
			_description = description;
			_visible = null;
		}

		public Schema(string name, string title, string description, bool visible)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Replace('-', '.');
			_title = string.IsNullOrEmpty(title) ? _name : title;
			_description = description;
			_visible = visible;
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Replace('-', '.');
			}
		}

		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				if(!string.IsNullOrEmpty(value))
					_title = value;
			}
		}

		public string Description
		{
			get
			{
				return _description;
			}
			set
			{
				_description = value;
			}
		}

		public bool Visible
		{
			get
			{
				if(_visible.HasValue)
					return _visible.Value;

				return this.HasActions;
			}
			set
			{
				_visible = value;
			}
		}

		[Runtime.Serialization.SerializationMember(Ignored = true)]
		public bool HasActions
		{
			get
			{
				return _actions != null && _actions.Count > 0;
			}
		}

		public SchemaActionCollection Actions
		{
			get
			{
				if(_actions == null)
					System.Threading.Interlocked.CompareExchange(ref _actions, new SchemaActionCollection(this), null);

				return _actions;
			}
		}
		#endregion

		#region 重写方法
		public bool Equals(Schema schema)
		{
			if(schema == null)
				return false;

			return string.Equals(_name, schema.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return string.Equals(_name, ((Schema)obj).Name, StringComparison.OrdinalIgnoreCase);
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
	}
}
