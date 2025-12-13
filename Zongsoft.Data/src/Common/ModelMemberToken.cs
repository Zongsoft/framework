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

namespace Zongsoft.Data.Common;

public sealed class ModelMemberToken<TModel> : IEquatable<ModelMemberToken<TModel>>
{
	#region 委托定义
	private delegate void SetValueDelegate(ref TModel target, object value);
	#endregion

	#region 私有变量
	private readonly MemberInfo _member;
	private readonly SetValueDelegate _setter;
	private readonly IDataRecordGetter _getter;
	private readonly ModelMemberEmitter.PopulatorWithGetter<TModel> _populateWithGetter;
	private readonly Func<object, Type, object> _converter;
	private readonly ModelMemberEmitter.PopulatorWithConverter<TModel> _populateWithConverter;
	#endregion

	#region 公共字段
	public readonly string Name;
	public readonly Type Type;
	#endregion

	#region 构造函数
	public ModelMemberToken(IDataDriver driver, FieldInfo field)
	{
		_member = field ?? throw new ArgumentNullException(nameof(field));

		this.Name = field.Name;
		this.Type = field.FieldType;

		_getter = driver.Getter;
		_setter = (ref TModel entity, object value) => Reflection.Reflector.SetValue(field, ref entity, value);
		_converter = ToThunk(Utility.GetConverter(field));

		if(_converter == null)
			_populateWithGetter = ModelMemberEmitter.GenerateFieldSetter<TModel>(field);
		else
			_populateWithConverter = ModelMemberEmitter.GenerateFieldSetter<TModel>(field, _converter);
	}

	public ModelMemberToken(IDataDriver driver, PropertyInfo property)
	{
		_member = property ?? throw new ArgumentNullException(nameof(property));

		this.Name = property.Name;
		this.Type = property.PropertyType;

		_getter = driver.Getter;
		_setter = (ref TModel entity, object value) => Reflection.Reflector.SetValue(property, ref entity, value);
		_converter = ToThunk(Utility.GetConverter(property));

		if(_converter == null)
			_populateWithGetter = ModelMemberEmitter.GeneratePropertySetter<TModel>(property);
		else
			_populateWithConverter = ModelMemberEmitter.GeneratePropertySetter<TModel>(property, _converter);
	}
	#endregion

	#region 公共属性
	public bool IsEmpty => _member == null;
	#endregion

	#region 公共方法
	public bool Populate(ref TModel target, IDataRecord record, int ordinal)
	{
		if(_converter == null)
			return _populateWithGetter.Invoke(ref target, record, ordinal, _getter);
		else
			return _populateWithConverter.Invoke(ref target, record, ordinal, _converter);
	}

	public void SetValue(ref TModel target, object value) => _setter.Invoke(ref target, value);
	#endregion

	#region 重写方法
	public bool Equals(ModelMemberToken<TModel> other) => other is not null && string.Equals(this.Name, other.Name);
	public override bool Equals(object obj) => this.Equals(obj as ModelMemberToken<TModel>);
	public override int GetHashCode() => HashCode.Combine(this.Name);
	public override string ToString() => $"{this.Name}:{this.Type.Name}";
	#endregion

	#region 符号重写
	public static bool operator !=(ModelMemberToken<TModel> left, ModelMemberToken<TModel> right) => !(left == right);
	public static bool operator ==(ModelMemberToken<TModel> left, ModelMemberToken<TModel> right) => left is null ? right is null : left.Equals(right);
	#endregion

	#region 私有方法
	private static Func<object, Type, object> ToThunk(System.ComponentModel.TypeConverter converter)
	{
		if(converter == null)
			return null;

		return new Func<object, Type, object>(
			(value, type) => Zongsoft.Common.Convert.ConvertValue(value, type, () => converter));
	}
	#endregion
}
