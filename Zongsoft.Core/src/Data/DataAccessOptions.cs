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

/// <summary>表示构建数据访问器的选项设置类。</summary>
public class DataAccessOptions : IDataAccessOptions
{
	#region 构造函数
	public DataAccessOptions(IEnumerable<IConnectionSettings> settings, IEnumerable<object> filters = null)
	{
		this.Settings = settings;
		this.Filters = filters;
	}
	#endregion

	#region 公共属性
	public IEnumerable<object> Filters { get; set; }
	public IEnumerable<IConnectionSettings> Settings { get; set; }
	#endregion
}
