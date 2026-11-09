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
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

/// <summary>提供数据源的多路复用功能的接口。</summary>
public interface IDataMultiplexer
{
	/// <summary>获取数据源提供程序。</summary>
	IDataSourceProvider Provider { get; }

	/// <summary>获取数据源选择程序。</summary>
	IDataSourceSelector Selector { get; }

	/// <summary>根据当前数据访问上下文选取合适的数据源。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <returns>返回对应的数据源。</returns>
	IDataSource GetSource(IDataAccessContextBase context);
}
