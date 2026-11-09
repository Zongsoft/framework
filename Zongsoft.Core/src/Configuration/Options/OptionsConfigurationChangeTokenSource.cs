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

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Options;

public class OptionsConfigurationChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions>
{
	#region 成员字段
	private readonly IConfiguration _configuration;
	#endregion

	#region 构造函数
	public OptionsConfigurationChangeTokenSource(IConfiguration configuration) : this(string.Empty, configuration) { }
	public OptionsConfigurationChangeTokenSource(string name, IConfiguration configuration)
	{
		this.Name = name ?? string.Empty;
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public IChangeToken GetChangeToken() => _configuration.GetReloadToken();
	#endregion
}
