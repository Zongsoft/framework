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
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata.Profiles
{
	/// <summary>
	/// 表示数据命令的元数据类。
	/// </summary>
	public class MetadataCommand : IDataCommand, IEquatable<IDataCommand>
	{
		#region 成员字段
		private string _name;
		private IDataMetadataProvider _provider;
		private Collections.INamedCollection<IDataCommandParameter> _parameters;
		#endregion

		#region 构造函数
		public MetadataCommand(IDataMetadataProvider provider, string name, string alias = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			this.Alias = alias;
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_parameters = new Collections.NamedCollection<IDataCommandParameter>(p => p.Name);
			this.Scriptor = new MetadataCommandScriptor(this);
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据命令所属的提供程序。</summary>
		public IDataMetadataProvider Metadata { get => _provider; }

		/// <summary>获取或设置数据命令的名称。</summary>
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

		/// <summary>获取或设置数据命令的类型。</summary>
		public DataCommandType Type { get; set; }

		/// <summary>获取或设置数据命令的别名（表名、存储过程名）。</summary>
		public string Alias { get; set; }

		/// <summary>获取或设置数据命令的变化性。</summary>
		public CommandMutability Mutability { get; set; }

		/// <summary>获取数据命令的参数集合。</summary>
		public Collections.INamedCollection<IDataCommandParameter> Parameters { get => _parameters; }

		/// <summary>获取数据命令的脚本对象。</summary>
		public MetadataCommandScriptor Scriptor { get; }

		/// <inheritdoc />
		IDataCommandScriptor IDataCommand.Scriptor { get => this.Scriptor; }
		#endregion

		#region 重写方法
		public bool Equals(IDataCommand other)
		{
			return other != null && string.Equals(other.Name, _name);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return this.Equals((IDataCommand)obj);
		}

		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}

		public override string ToString()
		{
			return $"{this.Type}:{this.Name}()";
		}
		#endregion
	}
}
