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

/// <summary>定义用于数据提供程序的访问方式。</summary>
[Flags]
public enum DataAccessMode
{
	/// <summary>读取数据。</summary>
	ReadOnly = 1,

	/// <summary>写入数据(新增、删除、修改)。</summary>
	WriteOnly = 2,

	/// <summary>读写数据。</summary>
	All = 3,
}
