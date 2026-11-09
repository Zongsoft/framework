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

namespace Zongsoft.Text.Templating;

/// <summary>表示模板的接口。</summary>
public interface ITemplate
{
	/// <summary>获取模板名称。</summary>
	string Name { get; }
	/// <summary>获取模板格式。</summary>
	TemplateFormat Format { get; }
	/// <summary>获取模板标题。</summary>
	string Title { get; }
	/// <summary>获取模板描述。</summary>
	string Description { get; }
	/// <summary>获取模板的元数据集合。</summary>
	ICollection<KeyValuePair<string, object>> Metadata { get; }
}
