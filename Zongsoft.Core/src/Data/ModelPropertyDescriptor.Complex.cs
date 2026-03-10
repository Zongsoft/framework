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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

partial class ModelPropertyDescriptor
{
	public class ComplexPropertyDescriptor : ModelPropertyDescriptor
	{
		#region 构造函数
		public ComplexPropertyDescriptor() { }
		public ComplexPropertyDescriptor(MemberInfo member) => this.Populate(member);
		#endregion

		#region 公共属性
		/// <summary>获取或设置关联目标，通常它是目标实体名，也支持跳跃关联(即关联到一个复合属性)。</summary>
		/// <remarks>跳跃关联是指关联目标为实体的导航属性，实体与导航属性之间以冒号(<c>:</c>)区隔。</remarks>
		public string Port
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Port));
				field = value;
				this.OnPropertyChanged(nameof(this.Port));
			}
		}

		/// <summary>获取或设置关联的连接数组。</summary>
		public string[] Links
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Links));
				field = value;
				this.OnPropertyChanged(nameof(this.Links));
			}
		}

		/// <summary>获取或设置关联的约束数组。</summary>
		public string[] Constraints
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Constraints));
				field = value;
				this.OnPropertyChanged(nameof(this.Constraints));
			}
		}

		/// <summary>获取关联目标的模型描述器。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public ModelDescriptor Target
		{
			get
			{
				if(field != null)
					return field;

				var type = Common.TypeExtension.GetElementType(this.Type) ?? this.Type;
				return field = Zongsoft.Data.Model.GetDescriptor(type);
			}
		}

		/// <summary>获取或设置属性的特性。</summary>
		public DataEntityComplexPropertyBehaviors Behaviors
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Behaviors));
				field = value;
				this.OnPropertyChanged(nameof(this.Behaviors));
			}
		}

		/// <summary>获取或设置一个值，指示关联的重复性关系。</summary>
		public DataAssociationMultiplicity Multiplicity
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Multiplicity));
				field = value;
				this.OnPropertyChanged(nameof(this.Multiplicity));
			}
		}
		#endregion

		#region 重写方法
		internal protected override void Populate(MemberInfo member)
		{
			if(member == null)
				return;

			//调用基类同名方法
			base.Populate(member);

			//设置默认的关联端口
			(var @namespace, var name) = ModelDescriptor.GetQualifiedName(this.Type, out _);
			this.Port = string.IsNullOrEmpty(@namespace) || string.Equals(this.Model?.Namespace, @namespace) ? name : $"{@namespace}.{name}";

			//设置默认的关联重复性
			this.Multiplicity = Common.TypeExtension.IsEnumerable(this.Type) ?
				DataAssociationMultiplicity.Many :
				DataAssociationMultiplicity.ZeroOrOne;

			var attribute = member.GetCustomAttribute<ModelPropertyAttribute>(true);
			if(attribute != null)
			{
				this.Port = attribute.Port;
				this.Behaviors = attribute.Behaviors;
				this.Multiplicity = attribute.Multiplicity;
				this.Links = attribute.Links;
				this.Constraints = attribute.Constraints;
			}
		}
		#endregion
	}
}
