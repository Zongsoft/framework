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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.ComponentModel;

using Zongsoft.Common;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据模型属性元信息的类。
/// </summary>
[System.Text.Json.Serialization.JsonDerivedType(typeof(SimplexPropertyDescriptor), "simplex")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ComplexPropertyDescriptor), "complex")]
public abstract partial class ModelPropertyDescriptor : INotifyPropertyChanged, INotifyPropertyChanging
{
	#region 事件定义
	public event PropertyChangedEventHandler PropertyChanged;
	public event PropertyChangingEventHandler PropertyChanging;
	#endregion

	#region 构造函数
	protected ModelPropertyDescriptor() { }
	#endregion

	#region 公共属性
	/// <summary>获取所属的模型定义。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public ModelDescriptor Model { get; internal set; }

	/// <summary>获取或设置属性成员信息。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public MemberInfo Member
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Member));
			field = value;
			this.OnPropertyChanged(nameof(this.Member));
		}
	}

	/// <summary>获取或设置属性名称。</summary>
	public string Name
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Name));
			field = value;
			this.OnPropertyChanged(nameof(this.Name));
		}
	}

	/// <summary>获取或设置属性类型。</summary>
	public Type Type
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Type));
			field = value;
			this.OnPropertyChanged(nameof(this.Type));
		}
	}

	/// <summary>获取或设置属性的提示。</summary>
	public string Hint
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Hint));
			field = value;
			this.OnPropertyChanged(nameof(this.Hint));
		}
	}

	/// <summary>获取或设置一个值，指示属性是否不可变更。</summary>
	public bool Immutable
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Immutable));
			field = value;
			this.OnPropertyChanged(nameof(this.Immutable));
		}
	}

	/// <summary>获取或设置属性的语义角色。</summary>
	public string Role
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Role));
			field = value;
			this.OnPropertyChanged(nameof(this.Role));
		}
	}

	/// <summary>获取或设置属性的标题。</summary>
	public string Label
	{
		get => string.IsNullOrEmpty(field) ? this.GetLabel() : field;
		set
		{
			this.OnPropertyChanging(nameof(this.Label));
			field = value;
			this.OnPropertyChanged(nameof(this.Label));
		}
	}

	/// <summary>获取或设置属性的描述。</summary>
	public string Description
	{
		get => string.IsNullOrEmpty(field) ? this.GetDescription() : field;
		set
		{
			this.OnPropertyChanging(nameof(this.Description));
			field = value;
			this.OnPropertyChanged(nameof(this.Description));
		}
	}
	#endregion

	#region 公共方法
	public bool IsSimplex() => this.IsSimplex(out _);
	public bool IsSimplex(out SimplexPropertyDescriptor simplex)
	{
		simplex = this as SimplexPropertyDescriptor;
		return simplex != null;
	}

	public bool IsComplex() => this.IsComplex(out _);
	public bool IsComplex(out ComplexPropertyDescriptor complex)
	{
		complex = this as ComplexPropertyDescriptor;
		return complex != null;
	}
	#endregion

	#region 虚拟方法
	internal protected virtual void Populate(MemberInfo member)
	{
		if(member == null)
			return;

		this.Member = member;
		this.Type = GetMemberType(member);

		if(string.IsNullOrEmpty(this.Name))
		{
			this.Name = member.Name;

			//设置默认的语义角色
			if(string.IsNullOrEmpty(this.Role))
				this.Role = ModelPropertyRole.Determine(member.Name);
		}

		var attribute = member.GetCustomAttribute<ModelPropertyAttribute>(true);
		if(attribute != null && !attribute.Ignored)
		{
			if(attribute.Role != null)
				this.Role = attribute.Role;

			this.Hint = attribute.Hint;
			this.Immutable = attribute.Immutable;
		}
	}
	#endregion

	#region 事件触发
	protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new(propertyName));
	protected virtual void OnPropertyChanging(string propertyName) => this.PropertyChanging?.Invoke(this, new(propertyName));
	#endregion

	#region 私有方法
	private string GetLabel() => this.Model == null ?
		this.GetResourceString($"{this.Name}.{nameof(this.Label)}", this.Name):
		this.GetResourceString(
			$"{this.Model.QualifiedName}.{this.Name}.{nameof(this.Label)}",
			$"{this.Model.QualifiedName}.{this.Name}",
			$"{this.Name}.{nameof(this.Label)}",
			this.Name);

	private string GetDescription() => this.Model == null ?
		this.GetResourceString($"{this.Name}.{nameof(this.Description)}"):
		this.GetResourceString(
			$"{this.Model.QualifiedName}.{this.Name}.{nameof(this.Description)}",
			$"{this.Name}.{nameof(this.Description)}");

	private string GetResourceString(params ReadOnlySpan<string> names) => this.Model != null ?
		this.Model.GetResourceString(names) :
		Resources.ResourceUtility.GetResourceString(this.Member, names);
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Role) ?
		$"{this.Name}:{TypeAlias.GetAlias(this.Type)}" :
		$"{this.Name}:{TypeAlias.GetAlias(this.Type)}({this.Role})";
	#endregion
}

partial class ModelPropertyDescriptor
{
	public static ModelPropertyDescriptor Create(MemberInfo info)
	{
		var attribute = info.GetCustomAttribute<ModelPropertyAttribute>(true);

		if(attribute == null)
			return IsSimplexType(GetMemberType(info)) ?
				new SimplexPropertyDescriptor(info):
				new ComplexPropertyDescriptor(info);

		if(attribute.Ignored)
			return null;

		return string.IsNullOrEmpty(attribute.Port) ?
			new SimplexPropertyDescriptor(info) :
			new ComplexPropertyDescriptor(info);
	}

	private static bool IsSimplexType(Type type) => TypeExtension.IsScalarType(type);
	private static Type GetMemberType(MemberInfo member) => member switch
	{
		FieldInfo info => info.FieldType,
		PropertyInfo info => info.PropertyType,
		_ => throw new ArgumentException($"The specified '{member.Name}' member is not a valid model property member."),
	};
}
