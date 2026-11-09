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

/// <summary>表示命名转换的枚举。</summary>
public enum SerializationNamingConvention
{
	/// <summary>无转换</summary>
	None,
	/// <summary>驼峰(小驼峰)命名</summary>
	Camel,
	/// <summary>烤串风格(使用短横线分隔)</summary>
	Kebab,
	/// <summary>蛇形风格(使用下划线分隔)</summary>
	Snake,
	/// <summary>帕斯卡(大驼峰)命名</summary>
	Pascal,
}
