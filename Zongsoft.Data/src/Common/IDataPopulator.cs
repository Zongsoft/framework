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
using System.Data;

namespace Zongsoft.Data.Common;

/// <summary>提供数据实体装配的接口。</summary>
public interface IDataPopulator
{
	/// <summary>数据实体装配方法。</summary>
	/// <typeparam name="T">指定要装配的实体类型。</typeparam>
	/// <param name="record">指定要装配的数据记录。</param>
	/// <returns>返回装配完成的数据对象。</returns>
	T Populate<T>(IDataRecord record);
}

/// <summary>提供数据实体装配的泛型接口。</summary>
/// <typeparam name="T">装配的实体类型。</typeparam>
public interface IDataPopulator<out T> : IDataPopulator
{
	/// <summary>数据实体装配方法。</summary>
	/// <param name="record">指定要装配的数据记录。</param>
	/// <returns>返回装配完成的数据对象。</returns>
	T Populate(IDataRecord record);
}
