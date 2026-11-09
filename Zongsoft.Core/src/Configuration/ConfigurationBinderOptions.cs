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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Configuration;

/// <summary>供 <see cref="ConfigurationBinder"/> 使用的选项类。</summary>
public class ConfigurationBinderOptions
{
	/// <summary>获取或设置一个值，指示是否绑定到非公共属性。默认为假(<c>False</c>)。</summary>
	/// <remarks>如果为假（默认值），则绑定器仅尝试设置公共属性；如果为真(<c>True</c>)，则绑定器将尝试设置所有（包含非公共）属性。</remarks>
	public bool BindNonPublicProperties { get; set; }

	/// <summary>获取或设置一个值，指示当绑定过程遇到无法处理的未识别属性（也未通过<see cref="ConfigurationAttribute.UnrecognizedProperty"/>定义未识别属性容器）是否抛出异常。默认为假(<c>False</c>)。</summary>
	public bool UnrecognizedError { get; set; }
}
