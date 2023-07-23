﻿/*
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
using System.Reflection;
using System.ComponentModel;

using Zongsoft.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据模型属性元信息的类。
	/// </summary>
	public class ModelPropertyDescriptor
	{
		#region 成员字段
		private ModelDescriptor _model;
		private IDataEntityProperty _field;
		private readonly MemberInfo _member;
		private readonly Type _type;
		private int _length;
		private bool _nullable;
		private bool _immutable;
		private object _defaultValue;
		private string _label;
		private string _description;
		#endregion

		#region 构造函数
		internal ModelPropertyDescriptor(MemberInfo member)
		{
			_member = member ?? throw new ArgumentNullException(nameof(member));

			_type = member switch
			{
				FieldInfo field => field.FieldType,
				PropertyInfo property => property.PropertyType,
				_ => throw new ArgumentException($"The specified '{member.Name}' member is not a valid model property member."),
			};

			_nullable = _type.IsInterface || _type.IsClass || _type.IsNullable();

			var attribute = member.GetCustomAttribute<DefaultValueAttribute>(true);

			if(attribute != null)
				_defaultValue = attribute.Value;
			else
				_defaultValue = TypeExtension.GetDefaultValue(_type);
		}
		#endregion

		#region 公共属性
		/// <summary>获取所属的模型定义。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public ModelDescriptor Model => _model;

		/// <summary>获取对应的数据实体字段定义。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public IDataEntityProperty Field => _field;

		/// <summary>获取属性成员信息。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public MemberInfo Member => _member;

		/// <summary>获取属性名称。</summary>
		public string Name => _member.Name;

		/// <summary>获取属性类型。</summary>
		public Type Type => _type;

		/// <summary>获取一个值，指示当属性为文本类型时，其允许的最大长度。</summary>
		public int Length => _length;

		/// <summary>获取一个值，指示属性是否为空。</summary>
		public bool Nullable => _nullable;

		/// <summary>获取一个值，指示属性是否不可变更。</summary>
		public bool Immutable => _immutable;

		/// <summary>获取属性的默认值。</summary>
		public object DefaultValue => _defaultValue;

		/// <summary>获取或设置属性的语义角色。</summary>
		public ModelPropertyRole Role { get; set; }

		/// <summary>获取或设置属性的标记。</summary>
		public ModelPropertyFlags Flags { get; set; }

		/// <summary>获取或设置属性的标题。</summary>
		public string Label
		{
			get => string.IsNullOrEmpty(_label) ? GetLabel() : _label;
			set => _label = value;
		}

		/// <summary>获取或设置属性的描述文本。</summary>
		public string Description
		{
			get => string.IsNullOrEmpty(_description) ? GetDescription() : _description;
			set => _description = value;
		}
		#endregion

		#region 内部方法
		internal void SetModel(ModelDescriptor model)
		{
			_model = model;

			if(model == null)
				return;

			_field = _model.Entity.Properties.TryGet(this.Name, out var field) ? field : null;

			if(field != null)
			{
				_immutable = field.Immutable;

				if(field.IsSimplex(out var simplex))
				{
					_length = simplex.Length;
					_nullable = simplex.Nullable;
					_defaultValue = simplex.DefaultValue;
				}
			}
		}
		#endregion

		#region 私有方法
		private string GetLabel() => _model == null ? this.Name :
			Resources.ResourceUtility.GetResourceString(_model.Type.Assembly, $"{_model.Name}.{this.Name}.{nameof(this.Label)}") ??
			Resources.ResourceUtility.GetResourceString(_model.Type.Assembly, $"{this.Name}.{nameof(this.Label)}");

		private string GetDescription() => _model == null ? null :
			Resources.ResourceUtility.GetResourceString(_model.Type.Assembly, $"{_model.Name}.{this.Name}.{nameof(this.Description)}") ??
			Resources.ResourceUtility.GetResourceString(_model.Type.Assembly, $"{this.Name}.{nameof(this.Description)}");
		#endregion

		#region 重写方法
		public override string ToString() => this.Role == ModelPropertyRole.None ?
			$"{this.Name}@{TypeExtension.GetTypeAlias(this.Type)}" :
			$"{this.Name}({this.Role})@{TypeExtension.GetTypeAlias(this.Type)}";
		#endregion
	}
}