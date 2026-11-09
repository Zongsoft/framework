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

namespace Zongsoft.Web.Security;

/// <summary>表示需要授权控制的特性，适用于类或方法。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class AuthorizationAttribute : Attribute
{
	#region 构造函数
	public AuthorizationAttribute(bool suppressed = false) => this.Suppressed = suppressed;
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，指示是否禁用授权控制。</summary>
	public bool Suppressed { get; }
	#endregion
}
