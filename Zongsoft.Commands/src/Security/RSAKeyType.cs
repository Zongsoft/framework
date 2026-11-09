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

/// <summary>表示RSA密钥类型的枚举。</summary>
public enum RSAKeyType
{
	/// <summary>全部(公钥和私钥)</summary>
	All,

	/// <summary>公钥</summary>
	Public,

	/// <summary>私钥</summary>
	Private,

	/// <summary>主体与公钥</summary>
	Subject,
}
