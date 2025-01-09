﻿/*
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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Plugins.Builders
{
	/// <summary>
	/// 提供构建器行为约定的特性类。
	/// </summary>
	/// <remarks>
	///		<para>在特定情况建议使用该类对构建器进行定制。</para>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class)]
	public class BuilderBehaviorAttribute : Attribute
	{
		#region 成员字段
		private Type _valueType;
		#endregion

		#region 构造函数
		public BuilderBehaviorAttribute(Type valueType) => _valueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
		#endregion

		#region 公共属性
		public Type ValueType
		{
			get => _valueType;
			set => _valueType = value ?? throw new ArgumentNullException(nameof(value));
		}
		#endregion
	}
}
