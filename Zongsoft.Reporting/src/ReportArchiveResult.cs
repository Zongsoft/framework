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
using System.IO;

namespace Zongsoft.Reporting;

/// <summary>表示归档查询的元数据及已打开的数据流。</summary>
/// <remarks>默认值表示未找到归档。找到归档但没有文件时，Stream 为空，Archive 仍有效。调用方负责异步释放非空 Stream。</remarks>
public readonly struct ReportArchiveResult
{
	#region 构造函数
	public ReportArchiveResult(ReportArchive archive, Stream stream = null)
	{
		this.Archive = archive;
		this.Stream = stream;
	}
	#endregion

	#region 公共属性
	public ReportArchive Archive { get; }
	public Stream Stream { get; }
	#endregion
}
