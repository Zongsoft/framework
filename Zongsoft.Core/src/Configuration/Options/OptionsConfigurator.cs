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
using System.Linq;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Options;

public class OptionsConfigurator<TOptions> : IConfigureNamedOptions<TOptions> where TOptions : class
{
	#region 构造函数
	public OptionsConfigurator(string name, IConfiguration configuration) : this(name, configuration, null) { }
	public OptionsConfigurator(string name, IConfiguration configuration, Action<ConfigurationBinderOptions> configureBinder)
	{
		this.Name = name;
		this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		this.ConfigurationBinder = configureBinder;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public IConfiguration Configuration { get; }
	public Action<ConfigurationBinderOptions> ConfigurationBinder { get; }
	#endregion

	#region 公共方法
	public void Configure(TOptions options) => this.Configure(string.Empty, options);
	public void Configure(string name, TOptions options)
	{
		if(string.IsNullOrEmpty(name))
			name = this.Name;

		var configuration = string.IsNullOrEmpty(name) ? this.Configuration :
			this.Configuration.GetChildren().FirstOrDefault(child => string.Equals(child.Key, name, StringComparison.OrdinalIgnoreCase));

		//调用Zongsoft的配置绑定方法：Zongsoft.Configuration.ConfigurationBinder.Bind(...)
		configuration?.Bind(options, this.ConfigurationBinder);
	}
	#endregion
}
