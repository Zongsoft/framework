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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Components;

/// <summary>提供对象的统一标识契约。</summary>
/// <remarks>赋值由实现者验证类别、键类型及业务约束；只读实现应明确拒绝赋值，不得静默忽略。</remarks>
public interface IIdentifiable
{
	/// <summary>获取或设置对象的标识。</summary>
	/// <value>包含类别、类型化键及可选元数据的标识。</value>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	Identifier Identifier { get; set; }
}

/// <summary>提供具有类型化键的对象标识契约。</summary>
/// <typeparam name="T">完整键的类型，也可以为复合键元组。</typeparam>
/// <remarks>非泛型接口通过默认实现桥接，转换时保留元数据并复用键类型转换规则；具体实现仍负责类别及业务校验。</remarks>
public interface IIdentifiable<T> : IIdentifiable
{
	/// <summary>获取或设置对象的类型化标识。</summary>
	/// <value>具有指定键类型及可选元数据的标识。</value>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T> Identifier { get; set; }

	/// <summary>通过非泛型契约读取或设置类型化标识。</summary>
	/// <value>保留类别及元数据的标识；扩展属性字典与类型化标识共享。</value>
	/// <exception cref="InvalidCastException">赋入标识的键值无法转换为目标键类型。</exception>
	Identifier IIdentifiable.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = (Identifier<T>)value;
	}
}
