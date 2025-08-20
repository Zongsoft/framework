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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Intelligences;

/// <summary>
/// 表示模型信息的接口。
/// </summary>
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
