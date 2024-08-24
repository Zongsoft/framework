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
using System.ComponentModel;

namespace Zongsoft.Configuration
{
	/// <summary>
	/// 表示配置属性的标注类。
	/// </summary>
	/// <remarks>
	/// 如果需要定义类型转换器，请加注 <seealso cref="System.ComponentModel.TypeConverterAttribute"/> 标注标记。
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class ConfigurationPropertyAttribute : Attribute
	{
		#region 构造函数
		public ConfigurationPropertyAttribute(string name)
		{
			this.Name = name == null ? string.Empty : name.Trim();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取对应于配置源中的键名。
		/// </summary>
		public string Name
		{
			get;
		}
		#endregion
	}
}
