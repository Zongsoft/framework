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

namespace Zongsoft.Externals.Wechat;

/// <summary>表示应用账户类型的枚举。</summary>
public enum AccountType
{
	/// <summary>小程序</summary>
	Applet,

	/// <summary>公众号</summary>
	Channel,

	/// <summary>开放平台移动应用，仅作为支付应用身份。</summary>
	Application,
}
