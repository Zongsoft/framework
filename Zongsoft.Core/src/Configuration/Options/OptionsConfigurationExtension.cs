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

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Zongsoft.Configuration.Options
{
	public static class OptionsConfigurationExtension
	{
		public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration configuration) where TOptions : class
		{
			return services.Configure<TOptions>(string.Empty, configuration);
		}

        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, IConfiguration configuration) where TOptions : class
        {
            return services.Configure<TOptions>(name, configuration, _ => { });
        }

        public static IServiceCollection Configure<TOptions>(this IServiceCollection services,
            IConfiguration configuration,
            Action<ConfigurationBinderOptions> configureBinder) where TOptions : class
        {
            return services.Configure<TOptions>(string.Empty, configuration, configureBinder);
        }

		public static IServiceCollection Configure<TOptions>(this IServiceCollection services,
            string name,
            IConfiguration configuration,
            Action<ConfigurationBinderOptions> configureBinder) where TOptions : class
		{
			if(services == null)
				throw new ArgumentNullException(nameof(services));

			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			return services.AddOptions()
                .Replace(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(Zongsoft.Configuration.Options.OptionsFactory<>)))
                .AddSingleton<IOptionsChangeTokenSource<TOptions>>(new OptionsConfigurationChangeTokenSource<TOptions>(name, configuration))
                .AddSingleton<IConfigureOptions<TOptions>>(new OptionsConfigurator<TOptions>(name, configuration, configureBinder));
		}
	}
}
