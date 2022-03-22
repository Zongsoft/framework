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
using System.Reflection;
using System.ComponentModel;

namespace Zongsoft.Data.Common
{
	public struct EntityMember
	{
		#region 私有变量
		private readonly MemberInfo _member;
		private readonly Action<object, object> _setter;
		private readonly EntityEmitter.Populator _populate;
		#endregion

		#region 公共字段
		public readonly string Name;
		public readonly Type Type;
		public readonly TypeConverter Converter;
		#endregion

		#region 构造函数
		public EntityMember(FieldInfo field, TypeConverter converter, EntityEmitter.Populator populate)
		{
			_member = field ?? throw new ArgumentNullException(nameof(field));

			this.Name = field.Name;
			this.Type = field.FieldType;
			this.Converter = converter;

			_setter = (entity, value) => field.SetValue(entity, value);
			_populate = populate ?? throw new ArgumentNullException(nameof(populate));
		}

		public EntityMember(PropertyInfo property, TypeConverter converter, EntityEmitter.Populator populate)
		{
			_member = property ?? throw new ArgumentNullException(nameof(property));

			this.Name = property.Name;
			this.Type = property.PropertyType;
			this.Converter = converter;

			_setter = (entity, value) => property.SetValue(entity, value);
			_populate = populate ?? throw new ArgumentNullException(nameof(populate));
		}
		#endregion

		#region 公共属性
		public bool IsEmpty => _member == null;
		#endregion

		#region 公共方法
		public void Populate(ref object entity, IDataRecord record, int ordinal)
		{
			_populate.Invoke(ref entity, record, ordinal, this.Converter);
		}

		public void SetValue(object entity, object value)
		{
			_setter.Invoke(entity, value);
		}
		#endregion

		#region 内部方法
		internal void EnsureConvertFrom(DbType dbType) => EnsureConvertFrom(Utility.FromDbType(dbType));
		internal void EnsureConvertFrom(Type type)
		{
			var converter = this.Converter;

			if(converter == null)
				return;

			if(converter != null && !converter.CanConvertFrom(type))
				throw new DataException($"The '{converter.GetType().Name}' converter for the '{_member.DeclaringType.Name}.{_member.Name}' field does not support conversion from {type.Name} type to '{this.Type.Name}' type.");
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.Name + " : " + this.Type.Name;
		#endregion
	}
}
