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

namespace Zongsoft.Messaging;

/// <summary>表示消息可靠性的枚举。</summary>
public enum MessageReliability
{
	/// <summary>最多一次</summary>
	MostOnce = 0,

	/// <summary>至少一次</summary>
	LeastOnce = 1,

	/// <summary>精确一次</summary>
	ExactlyOnce = 2
}
