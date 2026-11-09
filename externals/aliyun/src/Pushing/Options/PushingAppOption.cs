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
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Pushing.Options;

/// <summary>表示移动应用的配置选项。</summary>
public class PushingAppOption
{
	/// <summary>获取或设置移动应用的名称。</summary>
	public string Name
	{
		get; set;
	}

	/// <summary>获取或设置移动应用的代号(即移动推送的App-Key)。</summary>
	public string Code
	{
		get; set;
	}

	/// <summary>获取或设置移动应用的推送密码。</summary>
	public string Secret
	{
		get; set;
	}

	/// <summary>获取或设置移动应用所属的运营商区域。</summary>
	public ServiceCenterName? Region
	{
		get; set;
	}

	/// <summary>获取或设置移动应用关联的凭证名。</summary>
	public string Certificate
	{
		get; set;
	}
}
