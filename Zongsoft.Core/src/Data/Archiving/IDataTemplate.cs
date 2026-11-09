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

namespace Zongsoft.Data.Archiving;

/// <summary>表示数据模板的接口。</summary>
public interface IDataTemplate
{
	/// <summary>获取模板名称。</summary>
	string Name { get; }
	/// <summary>获取模板格式。</summary>
	DataArchiveFormat Format { get; }
	/// <summary>获取或设置模板标题。</summary>
	string Title { get; set; }
	/// <summary>获取或设置模板描述文本。</summary>
	string Description { get; set; }

	/// <summary>打开模板(获取模板内容)。</summary>
	/// <returns>返回模板内容流。</returns>
	Stream Open();
}