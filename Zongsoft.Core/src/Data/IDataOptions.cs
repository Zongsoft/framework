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

public interface IDataOptions
{
	/// <summary>获取操作选项的自定义参数集。</summary>
	Collections.Parameters Parameters { get; }
}

public interface IDataOptionsBuilder<out TOptions> where TOptions : IDataOptions
{
	/// <summary>构建数据操作选项。</summary>
	/// <returns>返回构建成功的数据操作选项。</returns>
	TOptions Build();
}
