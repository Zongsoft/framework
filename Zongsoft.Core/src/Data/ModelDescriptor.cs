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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据模型元信息的类。
/// </summary>
public sealed class ModelDescriptor : IEquatable<ModelDescriptor>
{
	#region 成员字段
	private string _name;
	private string _namespace;
	private string _title;
	private string _description;
	private IDataEntity _entity;
	private readonly Type _type;
	private ModelPropertyDescriptorCollection _properties;
	#endregion

	#region 构造函数
	internal ModelDescriptor(Type type, IDataEntity entity = null)
	{
		_type = type ?? throw new ArgumentNullException(nameof(type));
		_entity = entity;

		if(entity == null)
		{
			_name = type.Name;
			_namespace = ApplicationModuleAttribute.Find(type)?.Name;
		}
		else
		{
			_name = entity.Name;
			_namespace = entity.Namespace;
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据实体定义。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public IDataEntity Entity => _entity ??= Mapping.Entities.TryGetValue(this.QualifiedName, out var entity) ? entity : null;

	/// <summary>获取所属命名空间。</summary>
	public string Namespace => _namespace;

	/// <summary>获取模型名称。</summary>
	public string Name => _name;

	/// <summary>获取模型限定名称。</summary>
	public string QualifiedName => string.IsNullOrEmpty(this.Namespace) ? _name : $"{_namespace}.{_name}";

	/// <summary>获取模型别名。</summary>
	public string Alias => this.Entity?.Alias;

	/// <summary>获取模型类型。</summary>
	public Type Type => _type;

	/// <summary>获取模型属性元信息集。</summary>
	public ModelPropertyDescriptorCollection Properties
	{
		get
		{
			if(_properties == null)
			{
				lock(_type)
				{
					_properties ??= this.GetProperties();
				}
			}

			return _properties;
		}
	}

	/// <summary>获取或设置模型标题。</summary>
	public string Title
	{
		get => string.IsNullOrEmpty(_title) ? this.GetTitle() : _title;
		set => _title = value;
	}

	/// <summary>获取或设置模型描述文本。</summary>
	public string Description
	{
		get => string.IsNullOrEmpty(_description) ? this.GetDescription() : _description;
		set => _description = value;
	}
	#endregion

	#region 私有方法
	private string GetTitle() => Resources.ResourceUtility.GetResourceString(this.Type, [$"{this.Name}.{nameof(this.Title)}", this.Name]) ?? this.Name;
	private string GetDescription() => Resources.ResourceUtility.GetResourceString(this.Type, $"{this.Name}.{nameof(this.Description)}");

	private ModelPropertyDescriptorCollection GetProperties()
	{
		var properties = new ModelPropertyDescriptorCollection(this);

		//添加模型的属性定义
		properties.AddRange(
			_type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Select(property => new ModelPropertyDescriptor(property))
		);

		//添加模型的字段定义
		properties.AddRange(
			_type.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Select(field => new ModelPropertyDescriptor(field))
		);

		return properties;
	}
	#endregion

	#region 重写方法
	public bool Equals(ModelDescriptor other) => other is not null && this.Type == other.Type;
	public override bool Equals(object obj) => obj is ModelDescriptor other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(_type);
	public override string ToString() => string.IsNullOrEmpty(this.Namespace) ?
		$"{this.Name}@{TypeAlias.GetAlias(this.Type)}" :
		$"{this.Namespace}:{this.Name}@{TypeAlias.GetAlias(this.Type)}";
	#endregion
}
