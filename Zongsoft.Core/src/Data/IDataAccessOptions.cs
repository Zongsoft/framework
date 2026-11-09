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

using Zongsoft.Configuration;

namespace Zongsoft.Data;

/// <summary>表示构建数据访问器的选项设置接口。</summary>
public interface IDataAccessOptions
{
	/// <summary>获取或设置数据访问的过滤器集合。</summary>
	IEnumerable<object> Filters { get; set; }

	/// <summary>获取或设置数据访问的连接设置集合。</summary>
	IEnumerable<IConnectionSettings> Settings { get; set; }
}
