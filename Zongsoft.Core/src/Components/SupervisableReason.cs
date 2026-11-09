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

namespace Zongsoft.Components;

/// <summary>表示被监测对象取消原因的枚举。</summary>
public enum SupervisableReason
{
	/// <summary>手动，被手动取消监测。</summary>
	Manual,
	/// <summary>失败，超过允许报错次数。</summary>
	Failed,
	/// <summary>失活，超时未报导致失活。</summary>
	Inactived,
}
