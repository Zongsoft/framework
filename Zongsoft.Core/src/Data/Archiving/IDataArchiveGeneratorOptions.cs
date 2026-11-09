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

namespace Zongsoft.Data.Archiving;

/// <summary>表示数据文件生成选项的接口。</summary>
public interface IDataArchiveGeneratorOptions
{
	/// <summary>获取或设置生成格式化器。</summary>
	IDataArchiveFormatter Formatter { get; set; }

	/// <summary>获取或设置生成的字段数组。</summary>
	DataArchiveField[] Fields { get; set; }
}