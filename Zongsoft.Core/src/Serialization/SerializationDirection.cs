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

namespace Zongsoft.Serialization;

/// <summary>表示序列化方向的枚举。</summary>
public enum SerializationDirection
{
	/// <summary>未定义。</summary>
	None,

	/// <summary>输入，即反序列化调用。</summary>
	Input,

	/// <summary>输出，即序列化调用。</summary>
	Output,
}
