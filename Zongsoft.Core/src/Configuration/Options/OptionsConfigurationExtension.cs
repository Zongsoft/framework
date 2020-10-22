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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Zongsoft.Services
{
	public static class OptionsConfigurationExtension
	{
		public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration configuration) where TOptions : class
		{
			return services.Configure<TOptions>(string.Empty, configuration);
		}

		public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, IConfiguration configuration) where TOptions : class
		{
			return services.Configure<TOptions>(name, configuration, null);
		}

		public static IServiceCollection Configure<TOptions>(this IServiceCollection services,
			IConfiguration configuration,
			Action<Zongsoft.Configuration.ConfigurationBinderOptions> configureBinder) where TOptions : class
		{
			return services.Configure<TOptions>(string.Empty, configuration, configureBinder);
		}

		public static IServiceCollection Configure<TOptions>(this IServiceCollection services,
			string name,
			IConfiguration configuration,
			Action<Zongsoft.Configuration.ConfigurationBinderOptions> configureBinder) where TOptions : class
		{
			if(services == null)
				throw new ArgumentNullException(nameof(services));

			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			//确保选项类型没有重复注册，以免导致选项的重复解析（重复解析对字典/集合可能是有害的）
			if(services.Exists<TOptions>(name, configuration, configureBinder))
				return services;

			return services.AddOptions()
				.Replace(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(Zongsoft.Configuration.Options.OptionsFactory<>)))
				.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new Zongsoft.Configuration.Options.OptionsConfigurationChangeTokenSource<TOptions>(name, configuration))
				.AddSingleton<IConfigureOptions<TOptions>>(new Zongsoft.Configuration.Options.OptionsConfigurator<TOptions>(name, configuration, configureBinder));
		}

		private static bool Exists<TOptions>(this IServiceCollection services, string name, IConfiguration configuration, Action<Configuration.ConfigurationBinderOptions> binder) where TOptions : class
		{
			static bool CompareConfiguration(IConfiguration a, IConfiguration b)
			{
				if(a == null)
					return b == null;

				if(b == null)
					return a == null;

				if(a is IConfigurationSection x && b is IConfigurationSection y)
					return string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);

				return object.Equals(a, b);
			}

			return services.Any(descriptor =>
			{
				var instance = descriptor.ImplementationInstance;

				if(instance != null && instance is Configuration.Options.OptionsConfigurator<TOptions> configurator)
				{
					return CompareConfiguration(configuration, configurator.Configuration) &&
							string.Equals(name, configurator.Name, StringComparison.OrdinalIgnoreCase) &&
							object.Equals(binder, configurator.ConfigurationBinder);
				}

				return false;
			});
		}
	}
}
