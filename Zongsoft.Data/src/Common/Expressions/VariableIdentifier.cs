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

namespace Zongsoft.Data.Common.Expressions;

public class VariableIdentifier : Expression, IIdentifier
{
	#region 构造函数
	public VariableIdentifier(string name, bool isGlobal = false)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
		this.IsGlobal = isGlobal;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public bool IsGlobal { get; }
	#endregion

	#region 静态方法
	/// <summary>创建一个全局变量标识。</summary>
	/// <param name="name">指定的要创建的变量名。</param>
	/// <returns>返回新建的全局变量标识。</returns>
	public static VariableIdentifier Global(string name) => new VariableIdentifier(name, true);
	#endregion
}
