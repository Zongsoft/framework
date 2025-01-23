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

namespace Zongsoft.Configuration;

/// <summary>
/// 供 <see cref="ConfigurationBinder"/> 使用的选项类。
/// </summary>
public class ConfigurationBinderOptions
{
	/// <summary>获取或设置一个值，指示是否绑定到非公共属性。默认为假(<c>False</c>)。</summary>
	/// <remarks>如果为假（默认值），则绑定器仅尝试设置公共属性；如果为真(<c>True</c>)，则绑定器将尝试设置所有（包含非公共）属性。</remarks>
	public bool BindNonPublicProperties { get; set; }

	/// <summary>获取或设置一个值，指示当绑定过程遇到无法处理的未识别属性（也未通过<see cref="ConfigurationAttribute.UnrecognizedProperty"/>定义未识别属性容器）是否抛出异常。默认为假(<c>False</c>)。</summary>
	public bool UnrecognizedError { get; set; }
}
