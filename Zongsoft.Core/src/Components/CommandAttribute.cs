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

namespace Zongsoft.Components;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CommandAttribute : Attribute
{
	#region 构造函数
	public CommandAttribute() { }
	public CommandAttribute(bool ignoreOptions) => this.IgnoreOptions = ignoreOptions;
	#endregion

	#region 公共属性
	/// <summary>获取或设置是否忽略验证传入的命令选项是否合法，默认值为假(<c>False</c>)。</summary>
	public bool IgnoreOptions { get; set; }
	#endregion
}
