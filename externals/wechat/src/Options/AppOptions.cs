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

/// <summary>表示微信开放平台的第三方应用设置选项类。</summary>
public class AppOptions
{
	#region 公共属性
	/// <summary>获取或设置微信应用标识。</summary>
	public string Name { get; set; }

	/// <summary>获取或设置微信应用的类型。</summary>
	public AccountType Type { get; set; }
	#endregion
}
