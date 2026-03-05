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

namespace Zongsoft.Data;

partial class ModelPropertyDescriptor
{
	public class SimplexPropertyDescriptor : ModelPropertyDescriptor
	{
		#region 构造函数
		public SimplexPropertyDescriptor() { }
		public SimplexPropertyDescriptor(MemberInfo member) => this.Populate(member);
		#endregion

		#region 公共属性
		/// <summary>获取或设置属性别名。</summary>
		public string Alias
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Alias));
				field = value;
				this.OnPropertyChanged(nameof(this.Alias));
			}
		}

		/// <summary>获取或设置数据实体属性的数据类型。</summary>
		public DataType DataType
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.DataType));
				field = value;
				this.OnPropertyChanged(nameof(this.DataType));
			}
		}

		/// <summary>获取或设置一个值，指示当前属性是否为主键。</summary>
		public bool IsPrimaryKey
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.IsPrimaryKey));
				field = value;
				this.OnPropertyChanged(nameof(this.IsPrimaryKey));
			}
		}

		/// <summary>获取或设置文本或数组属性的最大长度。</summary>
		public int Length
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Length));
				field = value;
				this.OnPropertyChanged(nameof(this.Length));
			}
		}

		/// <summary>获取或设置数值属性的精度。</summary>
		public byte Precision
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Precision));
				field = value;
				this.OnPropertyChanged(nameof(this.Precision));
			}
		}

		/// <summary>获取或设置数值属性的小数点位数。</summary>
		public byte Scale
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Scale));
				field = value;
				this.OnPropertyChanged(nameof(this.Scale));
			}
		}

		/// <summary>获取或设置默认值。</summary>
		public object DefaultValue
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.DefaultValue));
				field = value;
				this.OnPropertyChanged(nameof(this.DefaultValue));
			}
		}

		/// <summary>获取或设置属性是否允许为空。</summary>
		public bool Nullable
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Nullable));
				field = value;
				this.OnPropertyChanged(nameof(this.Nullable));
			}
		}

		/// <summary>获取或设置属性是否可以参与排序。</summary>
		public bool Sortable
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Sortable));
				field = value;
				this.OnPropertyChanged(nameof(this.Sortable));
			}
		}

		/// <summary>获取或设置数据序号器元数据。</summary>
		public string Sequence
		{
			get; set
			{
				this.OnPropertyChanging(nameof(this.Sequence));
				field = value;
				this.OnPropertyChanged(nameof(this.Sequence));
			}
		}
		#endregion

		#region 重写方法
		internal protected override void Populate(MemberInfo member)
		{
			//调用基类同名方法
			base.Populate(member);

			var type = this.Type;
			if(type != null)
				this.Nullable = type.IsInterface || type.IsClass || Common.TypeExtension.IsNullable(type);

			if(member != null && this.DefaultValue == null)
			{
				var attribute = member.GetCustomAttribute<DefaultValueAttribute>(true);

				if(attribute != null)
					this.DefaultValue = Common.Convert.ConvertValue(attribute.Value, this.Type);
			}

			//设置属性特性映射
			this.Map(member);
		}
		#endregion

		#region 私有方法
		private void Map(MemberInfo member)
		{
			if(member == null)
				return;

			var attribute = member.GetCustomAttribute<ModelPropertyAttribute>(true);
			if(attribute == null)
				return;

			this.Alias = attribute.Alias;
			this.DataType = attribute.Type;
			this.Length = attribute.Length;
			this.Precision = attribute.Precision;
			this.Scale = attribute.Scale;
			this.Nullable = attribute.Nullable;
			this.Sortable = attribute.Sortable;
			this.Sequence = attribute.Sequence;

			if(this.IsPrimaryKey = attribute.IsPrimaryKey)
			{
				this.Sortable = true;
				this.Nullable = false;
			}

			if(attribute.DefaultValue != null)
				this.DefaultValue = Common.Convert.ConvertValue(attribute.DefaultValue, this.Type);
		}
		#endregion
	}
}
