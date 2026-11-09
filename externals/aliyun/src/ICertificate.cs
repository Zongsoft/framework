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

namespace Zongsoft.Externals.Aliyun;

/// <summary>表示阿里云访问凭证的配置项类。</summary>
public interface ICertificate
{
	string Name
	{
		get; set;
	}

	/// <summary>获取凭证的标识（即：AccessKey）。</summary>
	string Code
	{
		get; set;
	}

	/// <summary>获取凭证的密钥（即：AccessKeySecret）。</summary>
	string Secret
	{
		get; set;
	}
}
