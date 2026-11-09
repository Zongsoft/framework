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

/// <summary>表示数据文件提取选项的接口。</summary>
public interface IDataArchiveExtractorOptions
{
	/// <summary>获取模型元信息。</summary>
	ModelDescriptor Model { get; }

	/// <summary>获取或设置提取来源。</summary>
	object Source { get; set; }

	/// <summary>获取或设置提取的成员集。</summary>
	string[] Members { get; set; }

	/// <summary>获取上下文相关参数集合。</summary>
	Collections.Parameters Parameters { get; }

	/// <summary>获取或设置数据组装器。</summary>
	IDataArchivePopulator Populator { get; set; }
}
