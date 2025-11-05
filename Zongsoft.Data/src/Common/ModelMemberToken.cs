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

namespace Zongsoft.Data.Common;

public readonly struct ModelMemberToken : IEquatable<ModelMemberToken>
{
	#region 委托定义
	private delegate void SetValueDelegate(ref object target, object value);
	#endregion

	#region 私有变量
	private readonly IDataDriver _driver;
	private readonly MemberInfo _member;
	private readonly SetValueDelegate _setter;
	private readonly ModelMemberEmitter.Populator _populate;
	#endregion

	#region 公共字段
	public readonly string Name;
	public readonly Type Type;
	public readonly TypeConverter Converter;
	#endregion

	#region 构造函数
	public ModelMemberToken(IDataDriver driver, FieldInfo field)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
		_member = field ?? throw new ArgumentNullException(nameof(field));

		this.Name = field.Name;
		this.Type = field.FieldType;
		this.Converter = Utility.GetConverter(field);

		_setter = (ref object entity, object value) => Zongsoft.Reflection.Reflector.SetValue(field, ref entity, value);
		_populate = ModelMemberEmitter.GenerateFieldSetter(_driver, field, this.Converter);
	}

	public ModelMemberToken(IDataDriver driver, PropertyInfo property)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
		_member = property ?? throw new ArgumentNullException(nameof(property));

		this.Name = property.Name;
		this.Type = property.PropertyType;
		this.Converter = Utility.GetConverter(property);

		_setter = (ref object entity, object value) => Zongsoft.Reflection.Reflector.SetValue(property, ref entity, value);
		_populate = ModelMemberEmitter.GeneratePropertySetter(_driver, property, this.Converter);
	}
	#endregion

	#region 公共属性
	public readonly bool IsEmpty => _member == null;
	#endregion

	#region 公共方法
	public void Populate(ref object target, IDataRecord record, int ordinal) => _populate.Invoke(ref target, record, ordinal, this.Converter);
	public void SetValue(ref object target, object value) => _setter.Invoke(ref target, value);
	#endregion

	#region 内部方法
	internal void EnsureConvertFrom(DbType dbType) => this.EnsureConvertFrom(dbType.AsType());
	internal void EnsureConvertFrom(Type type)
	{
		var converter = this.Converter;
		if(converter != null && !converter.CanConvertFrom(type))
			throw new DataException($"The '{converter.GetType().Name}' converter for the '{_member.DeclaringType.Name}.{_member.Name}' field does not support conversion from {type.Name} type to '{this.Type.Name}' type.");
	}
	#endregion

	#region 重写方法
	public bool Equals(ModelMemberToken other) => string.Equals(this.Name, other.Name);
	public override bool Equals(object obj) => obj is ModelMemberToken other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Name);
	public override string ToString() => $"{this.Name}:{this.Type.Name}";

	public static bool operator ==(ModelMemberToken left, ModelMemberToken right) => left.Equals(right);
	public static bool operator !=(ModelMemberToken left, ModelMemberToken right) => !(left == right);
	#endregion
}

public readonly struct ModelMemberToken<T> : IEquatable<ModelMemberToken<T>>
{
	#region 委托定义
	private delegate void SetValueDelegate(ref T target, object value);
	#endregion

	#region 私有变量
	private readonly IDataDriver _driver;
	private readonly MemberInfo _member;
	private readonly SetValueDelegate _setter;
	private readonly ModelMemberEmitter.Populator<T> _populate;
	#endregion

	#region 公共字段
	public readonly string Name;
	public readonly Type Type;
	public readonly TypeConverter Converter;
	#endregion

	#region 构造函数
	public ModelMemberToken(IDataDriver driver, FieldInfo field)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
		_member = field ?? throw new ArgumentNullException(nameof(field));

		this.Name = field.Name;
		this.Type = field.FieldType;
		this.Converter = Utility.GetConverter(field);

		_setter = (ref T entity, object value) => Zongsoft.Reflection.Reflector.SetValue(field, ref entity, value);
		_populate = ModelMemberEmitter.GenerateFieldSetter<T>(_driver, field, this.Converter);
	}

	public ModelMemberToken(IDataDriver driver, PropertyInfo property)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
		_member = property ?? throw new ArgumentNullException(nameof(property));

		this.Name = property.Name;
		this.Type = property.PropertyType;
		this.Converter = Utility.GetConverter(property);

		_setter = (ref T entity, object value) => Zongsoft.Reflection.Reflector.SetValue(property, ref entity, value);
		_populate = ModelMemberEmitter.GeneratePropertySetter<T>(_driver, property, this.Converter);
	}
	#endregion

	#region 公共属性
	public readonly bool IsEmpty => _member == null;
	#endregion

	#region 公共方法
	public void Populate(ref T target, IDataRecord record, int ordinal) => _populate.Invoke(ref target, record, ordinal, this.Converter);
	public void SetValue(ref T target, object value) => _setter.Invoke(ref target, value);
	#endregion

	#region 内部方法
	internal void EnsureConvertFrom(DbType dbType) => this.EnsureConvertFrom(dbType.AsType());
	internal void EnsureConvertFrom(Type type)
	{
		var converter = this.Converter;
		if(converter != null && !converter.CanConvertFrom(type))
			throw new DataException($"The '{converter.GetType().Name}' converter for the '{_member.DeclaringType.Name}.{_member.Name}' field does not support conversion from {type.Name} type to '{this.Type.Name}' type.");
	}
	#endregion

	#region 重写方法
	public bool Equals(ModelMemberToken<T> other) => string.Equals(this.Name, other.Name);
	public override bool Equals(object obj) => obj is ModelMemberToken<T> other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Name);
	public override string ToString() => $"{this.Name}:{this.Type.Name}";

	public static bool operator ==(ModelMemberToken<T> left, ModelMemberToken<T> right) => left.Equals(right);
	public static bool operator !=(ModelMemberToken<T> left, ModelMemberToken<T> right) => !(left == right);
	#endregion
}
