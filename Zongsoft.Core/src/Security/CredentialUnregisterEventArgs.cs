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

public class CredentialUnregisterEventArgs : EventArgs
{
	#region 构造函数
	public CredentialUnregisterEventArgs(string credentialId, bool renewal = false)
	{
		this.IsRenewal = renewal;
		this.CredentialId = credentialId;
	}
	#endregion

	#region 公共属性
	/// <summary>获取注销的凭证编号。</summary>
	public string CredentialId { get; }

	/// <summary>获取一个值，指示当前注销是否为续约引发。</summary>
	public bool IsRenewal { get; }
	#endregion
}
