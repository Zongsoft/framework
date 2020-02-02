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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

namespace Zongsoft.Data.Metadata.Profiles
{
	/// <summary>
	/// 表示命令参数的元数据类。
	/// </summary>
	public class MetadataCommandParameter : IDataCommandParameter
	{
		#region 成员字段
		private IDataCommand _command;
		private string _name;
		private string _alias;
		private Type _type;
		private int _length;
		private object _value;
		private ParameterDirection _direction;
		#endregion

		#region 构造函数
		public MetadataCommandParameter(IDataCommand command, string name, Type type, ParameterDirection direction = ParameterDirection.Input)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_command = command;
			_name = name.Trim();
			_type = type ?? throw new ArgumentNullException(nameof(type));
			_direction = direction;
		}
		#endregion

		#region 公共属性
		public IDataCommand Command
		{
			get
			{
				return _command;
			}
		}

		/// <summary>
		/// 获取命令参数的名称。
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		/// <summary>
		/// 获取或设置命令参数的别名。
		/// </summary>
		public string Alias
		{
			get
			{
				return _alias;
			}
			set
			{
				_alias = value;
			}
		}

		/// <summary>
		/// 获取命令参数的类型。
		/// </summary>
		public Type Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		/// <summary>
		/// 获取或设置命令参数的最大长度。
		/// </summary>
		public int Length
		{
			get
			{
				return _length;
			}
			set
			{
				_length = value;
			}
		}

		/// <summary>
		/// 获取或设置命令参数的值。
		/// </summary>
		public object Value
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

		/// <summary>
		/// 获取或设置命令参数的传递方向。
		/// </summary>
		public ParameterDirection Direction
		{
			get
			{
				return _direction;
			}
			set
			{
				_direction = value;
			}
		}
		#endregion
	}
}
