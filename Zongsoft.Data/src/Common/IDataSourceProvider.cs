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

/// <summary>表示数据源提供程序的接口。</summary>
public interface IDataSourceProvider
{
	/// <summary>获取指定应用名的可用数据源集合。</summary>
	/// <param name="name">指定要获取数据源集的应用名。</param>
	/// <returns>返回指定应用的可用数据源集。</returns>
	IEnumerable<IDataSource> GetSources(string name);
}
