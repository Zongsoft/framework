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

namespace Zongsoft.Security.Configuration;

/// <summary>表示密码强度的枚举。</summary>
public enum PasswordStrength
{
	/// <summary>无强度约束。</summary>
	None,

	/// <summary>全数字（只能是数字），适用于只有数字输入设备的场景。</summary>
	Digits,

	/// <summary>最低强度。</summary>
	Lowest,

	/// <summary>中等强度。</summary>
	Normal,

	/// <summary>最高强度。</summary>
	Highest
}
