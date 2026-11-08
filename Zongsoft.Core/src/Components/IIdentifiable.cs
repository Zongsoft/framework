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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
	#region 公共属性
	/// <summary>获取或设置对象的标识。</summary>
	/// <value>包含类别、类型化键及可选元数据的标识。</value>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	Identifier Identifier { get; set; }
	#endregion
}

/// <summary>提供具有类型化键的对象标识契约。</summary>
/// <typeparam name="T">完整键的类型，也可以为复合键元组。</typeparam>
/// <remarks>非泛型接口通过默认实现桥接，转换时保留元数据并复用键类型转换规则；具体实现仍负责类别及业务校验。</remarks>
public interface IIdentifiable<T> : IIdentifiable
{
	#region 公共属性
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
	#endregion
}

/// <summary>提供具有 2 个类型化键分量的对象标识契约。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <remarks>通过默认实现桥接完整元组键及非泛型接口，保留类别、键类型和元数据。</remarks>
public interface IIdentifiable<T1, T2> : IIdentifiable<(T1, T2)>
{
	#region 公共属性
	/// <summary>获取或设置对象的类型化复合标识。</summary>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T1, T2> Identifier { get; set; }

	/// <summary>通过完整元组键契约读取或设置标识。</summary>
	Identifier<(T1, T2)> IIdentifiable<(T1, T2)>.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = value;
	}
	#endregion
}

/// <summary>提供具有 3 个类型化键分量的对象标识契约。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
/// <remarks>通过默认实现桥接完整元组键及非泛型接口，保留类别、键类型和元数据。</remarks>
public interface IIdentifiable<T1, T2, T3> : IIdentifiable<(T1, T2, T3)>
{
	#region 公共属性
	/// <summary>获取或设置对象的类型化复合标识。</summary>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T1, T2, T3> Identifier { get; set; }

	/// <summary>通过完整元组键契约读取或设置标识。</summary>
	Identifier<(T1, T2, T3)> IIdentifiable<(T1, T2, T3)>.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = value;
	}
	#endregion
}

/// <summary>提供具有 4 个类型化键分量的对象标识契约。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
/// <remarks>通过默认实现桥接完整元组键及非泛型接口，保留类别、键类型和元数据。</remarks>
public interface IIdentifiable<T1, T2, T3, T4> : IIdentifiable<(T1, T2, T3, T4)>
{
	#region 公共属性
	/// <summary>获取或设置对象的类型化复合标识。</summary>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T1, T2, T3, T4> Identifier { get; set; }

	/// <summary>通过完整元组键契约读取或设置标识。</summary>
	Identifier<(T1, T2, T3, T4)> IIdentifiable<(T1, T2, T3, T4)>.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = value;
	}
	#endregion
}

/// <summary>提供具有 5 个类型化键分量的对象标识契约。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
/// <typeparam name="T5">第 5 个键分量的类型。</typeparam>
/// <remarks>通过默认实现桥接完整元组键及非泛型接口，保留类别、键类型和元数据。</remarks>
public interface IIdentifiable<T1, T2, T3, T4, T5> : IIdentifiable<(T1, T2, T3, T4, T5)>
{
	#region 公共属性
	/// <summary>获取或设置对象的类型化复合标识。</summary>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T1, T2, T3, T4, T5> Identifier { get; set; }

	/// <summary>通过完整元组键契约读取或设置标识。</summary>
	Identifier<(T1, T2, T3, T4, T5)> IIdentifiable<(T1, T2, T3, T4, T5)>.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = value;
	}
	#endregion
}

/// <summary>提供具有 6 个类型化键分量的对象标识契约。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
/// <typeparam name="T5">第 5 个键分量的类型。</typeparam>
/// <typeparam name="T6">第 6 个键分量的类型。</typeparam>
/// <remarks>通过默认实现桥接完整元组键及非泛型接口，保留类别、键类型和元数据。</remarks>
public interface IIdentifiable<T1, T2, T3, T4, T5, T6> : IIdentifiable<(T1, T2, T3, T4, T5, T6)>
{
	#region 公共属性
	/// <summary>获取或设置对象的类型化复合标识。</summary>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T1, T2, T3, T4, T5, T6> Identifier { get; set; }

	/// <summary>通过完整元组键契约读取或设置标识。</summary>
	Identifier<(T1, T2, T3, T4, T5, T6)> IIdentifiable<(T1, T2, T3, T4, T5, T6)>.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = value;
	}
	#endregion
}

/// <summary>提供具有 7 个类型化键分量的对象标识契约。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
/// <typeparam name="T5">第 5 个键分量的类型。</typeparam>
/// <typeparam name="T6">第 6 个键分量的类型。</typeparam>
/// <typeparam name="T7">第 7 个键分量的类型。</typeparam>
/// <remarks>通过默认实现桥接完整元组键及非泛型接口，保留类别、键类型和元数据。</remarks>
public interface IIdentifiable<T1, T2, T3, T4, T5, T6, T7> : IIdentifiable<(T1, T2, T3, T4, T5, T6, T7)>
{
	#region 公共属性
	/// <summary>获取或设置对象的类型化复合标识。</summary>
	/// <exception cref="NotSupportedException">对象仅支持读取标识。</exception>
	new Identifier<T1, T2, T3, T4, T5, T6, T7> Identifier { get; set; }

	/// <summary>通过完整元组键契约读取或设置标识。</summary>
	Identifier<(T1, T2, T3, T4, T5, T6, T7)> IIdentifiable<(T1, T2, T3, T4, T5, T6, T7)>.Identifier
	{
		get => this.Identifier;
		set => this.Identifier = value;
	}
	#endregion
}
