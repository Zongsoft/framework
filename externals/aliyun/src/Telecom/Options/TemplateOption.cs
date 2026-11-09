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

namespace Zongsoft.Externals.Aliyun.Telecom.Options;

/// <summary>表示短信模板的配置项接口。</summary>
public class TemplateOption
{
	#region 构造函数
	public TemplateOption() => this.Parameters = new();
	#endregion

	#region 公共属性
	/// <summary>获取或设置模板的名称。</summary>
	public string Name { get; set; }

	/// <summary>获取或设置模板的代号。</summary>
	public string Code { get; set; }

	/// <summary>获取或设置模板的方案（即阿里云的短信签名）。</summary>
	public string Scheme { get; set; }

	/// <summary>获取或设置模板格式化器的名称。</summary>
	public string Formatter { get; set; }

	/// <summary>获取或设置模板参数集。</summary>
	public TemplateParameterOptionCollection Parameters { get; set; }
	#endregion
}
