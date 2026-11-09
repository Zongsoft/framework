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

namespace Zongsoft.Security;

public class CredentialRegisterEventArgs : EventArgs
{
	#region 构造函数
	public CredentialRegisterEventArgs(CredentialPrincipal principal, bool renewal = false)
	{
		this.IsRenewal = renewal;
		this.Principal = principal ?? throw new ArgumentNullException(nameof(principal));
	}
	#endregion

	#region 公共属性
	/// <summary>获取注册的凭证主体对象。</summary>
	public CredentialPrincipal Principal { get; }

	/// <summary>获取一个值，指示当前注册是否为续约引发。</summary>
	public bool IsRenewal { get; }
	#endregion
}
