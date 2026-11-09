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

/// <summary>提供模板格式化功能的接口。</summary>
public interface ITemplateFormatter
{
	/// <summary>获取格式化器名称。</summary>
	string Name { get; }

	/// <summary>格式化模板参数。</summary>
	/// <param name="name">指定的模板名称。</param>
	/// <param name="data">待格式化的模板数据。</param>
	/// <param name="arguments">附加参数集。</param>
	/// <returns>返回格式化后的模板数据。</returns>
	object Format(string name, object data, params IEnumerable<KeyValuePair<string, object>> arguments);
}
