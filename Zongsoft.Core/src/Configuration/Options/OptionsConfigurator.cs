/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Options
{
	public class OptionsConfigurator<TOptions> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class
	{
		#region 成员字段
		private readonly string _name;
		private readonly IConfiguration _configuration;
		private readonly Action<ConfigurationBinderOptions> _configureBinder;
		#endregion

		#region 构造函数
		public OptionsConfigurator(string name, IConfiguration configuration) : this(name, configuration, null)
		{
		}

		public OptionsConfigurator(string name, IConfiguration configuration, Action<ConfigurationBinderOptions> configureBinder)
		{
			_name = name;
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_configureBinder = configureBinder;
		}
		#endregion

		#region 公共方法
		public void Configure(TOptions options)
		{
			this.Configure(string.Empty, options);
		}

		public void Configure(string name, TOptions options)
		{
			if(string.IsNullOrEmpty(name))
				name = _name;

			var configuration = string.IsNullOrEmpty(name) ? _configuration :
				_configuration.GetChildren().FirstOrDefault(child => string.Equals(child.Key, name, StringComparison.OrdinalIgnoreCase));

			configuration?.Bind(options, _configureBinder);
		}
		#endregion
	}
}
