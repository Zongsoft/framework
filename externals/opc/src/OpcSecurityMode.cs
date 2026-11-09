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

namespace Zongsoft.Externals.Opc;

/// <summary>表示安全模式的枚举。</summary>
public enum OpcSecurityMode
{
	/// <summary>非安全模式</summary>
	None = 0,
	/// <summary>签名</summary>
	Sign = 1,
	/// <summary>签名并加密</summary>
	SignAndEncrypt = 2,
}
