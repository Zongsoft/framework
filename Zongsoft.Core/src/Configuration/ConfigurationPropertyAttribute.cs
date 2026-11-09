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

/// <summary>表示配置属性的标注类。</summary>
/// <remarks>如果需要定义类型转换器，请加注 <seealso cref="System.ComponentModel.TypeConverterAttribute"/> 标注标记。</remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public class ConfigurationPropertyAttribute : Attribute
{
	#region 构造函数
	public ConfigurationPropertyAttribute(string name = null)
	{
		this.Name = name == null ? string.Empty : name.Trim();
	}
	#endregion

	#region 公共属性
	/// <summary>获取对应于配置源中的键名。</summary>
	public string Name { get; }
	#endregion
}
