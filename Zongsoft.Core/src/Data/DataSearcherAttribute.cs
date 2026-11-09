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

namespace Zongsoft.Data;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class DataSearcherAttribute : Attribute
{
	#region 构造函数
	public DataSearcherAttribute(params string[] patterns)
	{
		if(patterns == null || patterns.Length == 0)
			throw new ArgumentNullException(nameof(patterns));

		this.Patterns = patterns;
	}
	#endregion

	#region 公共属性
	public string[] Patterns { get; }
	#endregion
}
