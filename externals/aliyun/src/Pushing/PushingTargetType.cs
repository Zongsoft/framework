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
using System.ComponentModel;

namespace Zongsoft.Externals.Aliyun.Pushing;

/// <summary>关于移动推送目标种类(推送方式)的枚举。</summary>
public enum PushingTargetType
{
	/// <summary>所有</summary>
	All,

	/// <summary>设备</summary>
	Device,

	/// <summary>账号</summary>
	Account,

	/// <summary>别名</summary>
	Alias,

	/// <summary>标签</summary>
	Tag,
}
