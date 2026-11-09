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

namespace Zongsoft.Data.Common;

/// <summary>表示数据源选择器的接口。</summary>
public interface IDataSourceSelector
{
	/// <summary>根据当前数据访问上下文从指定的数据源集合中选取一个数据源。</summary>
	/// <param name="context">指定的当前数据访问上下文。</param>
	/// <returns>返回适合于指定数据访问上下文的数据源。</returns>
	IDataSource GetSource(IDataAccessContextBase context);
}
