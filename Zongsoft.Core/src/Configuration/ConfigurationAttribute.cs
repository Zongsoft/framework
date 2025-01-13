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

namespace Zongsoft.Configuration
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
	public class ConfigurationAttribute : Attribute
	{
		#region 构造函数
		public ConfigurationAttribute() { }
		public ConfigurationAttribute(string unrecognizedProperty)
		{
			this.UnrecognizedProperty = unrecognizedProperty;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置配置解析器类型。</summary>
		public Type ResolverType { get; set; }
		/// <summary>获取或设置配置识别器类型。</summary>
		public Type RecognizerType { get; set; }
		/// <summary>获取或设置用来承载配置中所有未识别特性的属性名，该属性类型通常为 <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> 或 <see cref="System.Collections.ObjectModel.KeyedCollection{TKey, TItem}" /> 类型。</summary>
		public string UnrecognizedProperty { get; set; }
		#endregion
	}
}
