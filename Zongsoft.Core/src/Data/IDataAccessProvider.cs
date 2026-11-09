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

/// <summary>表示数据访问提供程序的接口。</summary>
public interface IDataAccessProvider
{
	/// <summary>获取或创建指定应用的数据访问器。</summary>
	/// <param name="name">指定的应用名。</param>
	/// <returns>返回指定应用名的数据访问器。</returns>
	IDataAccess GetAccessor(string name = null);

	/// <summary>获取或创建指定应用的数据访问器。</summary>
	/// <param name="name">指定的应用名。</param>
	/// <param name="options">指定的数据访问器选项设置。</param>
	/// <returns>返回指定应用名的数据访问器。</returns>
	IDataAccess GetAccessor(string name, IDataAccessOptions options = null);
}
