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

/// <summary>表示数据访问的上下文的基本接口。</summary>
public interface IDataAccessContextBase
{
	/// <summary>获取数据访问的名称。</summary>
	string Name { get; }

	/// <summary>获取数据访问的方法。</summary>
	DataAccessMethod Method { get; }

	/// <summary>获取当前上下文关联的数据访问器。</summary>
	IDataAccess DataAccess { get; }

	/// <summary>获取当前安全主体。</summary>
	System.Security.Claims.ClaimsPrincipal Principal { get; }

	/// <summary>获取指定类型的特性。</summary>
	/// <typeparam name="TFeature">指定的特性类型。</typeparam>
	/// <returns>返回的指定类型的特性，如果没有找到则返回空(<c>null</c>)。</returns>
	TFeature GetFeature<TFeature>();
}

/// <summary>表示数据访问的上下文的基本接口。</summary>
/// <typeparam name="TOptions">当前数据操作的选项类型。</typeparam>
public interface IDataAccessContextBase<TOptions> : IDataAccessContextBase where TOptions : IDataOptions
{
	/// <summary>获取当前数据访问操作的选项对象。</summary>
	TOptions Options { get; }
}
