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

namespace Zongsoft.Data;

/// <summary>表示数据序列字段行为的枚举。</summary>
public enum DataSequenceBehavior
{
	/// <summary>自动，表示序列字段值为空或等于零则由序列器生成，否则为字段指定值。</summary>
	Auto,
	/// <summary>始终由序列器生成。</summary>
	Alway,
	/// <summary>禁用序列器的生成。</summary>
	Never,
}
