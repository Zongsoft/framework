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

namespace Zongsoft.Externals.Wechat.Options;

public class ChannelOptions
{
	/// <summary>获取或设置公众号标识。</summary>
	public string Name { get; set; }

	/// <summary>获取或设置公众号密钥。</summary>
	public string Secret { get; set; }

	/// <summary>获取或设置公众号的对称加解密的密钥，即微信开放平台的<c>symmetric_key</c>或<c>AESEncodingKey</c>。</summary>
	public string Password { get; set; }
}
