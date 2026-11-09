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

namespace Zongsoft.Intelligences;

/// <summary>表示模型信息的接口。</summary>
public interface IModel
{
	/// <summary>获取模型的唯一标识。</summary>
	string Identifier { get; }
	/// <summary>获取模型的名称。</summary>
	string Name { get; }
	/// <summary>获取模型的大小，单位：字节。</summary>
	long Size { get; }
	/// <summary>获取模型的版本。</summary>
	string Version { get; }
	/// <summary>获取模型的创建时间。</summary>
	DateTimeOffset Creation { get; }
	/// <summary>获取模型的描述信息。</summary>
	string Description { get; }
}
