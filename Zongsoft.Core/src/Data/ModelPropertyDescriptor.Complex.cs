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
		protected override void OnPropertyChanged(string propertyName)
		{
			switch(propertyName)
			{
				case nameof(this.Member) when this.Member != null:
					var attribute = this.Member.GetCustomAttribute<ModelPropertyAttribute>(true);

					if(attribute != null)
					{
						this.Port = attribute.Port;
						this.Behaviors = attribute.Behaviors;
						this.Multiplicity = attribute.Multiplicity;
					}

					break;
			}

			//调用基类同名方法
			base.OnPropertyChanged(propertyName);
		}
		#endregion
	}
}
