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

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration
{
	public static class ConfigurationBinder
	{
		#region 公共方法
		public static void Bind(this IConfiguration configuration, string path, object instance)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			configuration.GetSection(ConfigurationUtility.GetConfigurationPath(path)).Bind(instance);
		}

		public static void Bind(this IConfiguration configuration, object instance) => configuration.Bind(instance, o => { });
		public static void Bind(this IConfiguration configuration, object instance, Action<ConfigurationBinderOptions> configureOptions)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			if(instance != null)
			{
				var options = new ConfigurationBinderOptions();
				configureOptions?.Invoke(options);
				GetResolver(instance.GetType()).Resolve(instance, configuration, options);
			}
		}

		public static T GetOption<T>(this IConfiguration configuration, string path = null, Action<ConfigurationBinderOptions> configureOptions = null)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var result = configuration.GetOption(typeof(T), path, configureOptions);

			if(result == null)
				return default;

			return (T)result;
		}

		public static object GetOption(this IConfiguration configuration, Type type, string path = null, Action<ConfigurationBinderOptions> configureOptions = null)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var options = new ConfigurationBinderOptions();
			configureOptions?.Invoke(options);

			if(!string.IsNullOrEmpty(path))
				configuration = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(path));

			if(configuration is IConfigurationSection section && section.Value != null)
				return Common.Convert.ConvertValue(section.Value, type);

			return GetResolver(type).Resolve(type, configuration, options);
		}

		public static T GetOptionValue<T>(this IConfiguration configuration, string path) => GetOptionValue(configuration, path, default(T));
		public static T GetOptionValue<T>(this IConfiguration configuration, string path, T defaultValue) => (T)GetOptionValue(configuration, typeof(T), path, defaultValue);
		public static object GetOptionValue(this IConfiguration configuration, Type type, string path) => GetOptionValue(configuration, type, path, defaultValue: null);
		public static object GetOptionValue(this IConfiguration configuration, Type type, string path, object defaultValue)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			var section = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(path));
			var value = section.Value;

			if(value == null)
				return defaultValue;

			if(Common.Convert.TryConvertValue(value, type, out var result))
				return result;

			throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedBinding, path, type));
		}

		public static void SetOption(this IConfiguration configuration, object instance, string path = null, Action<ConfigurationBinderOptions> configureOptions = null)
		{
			if(instance == null || configuration == null)
				return;

			var options = new ConfigurationBinderOptions();
			configureOptions?.Invoke(options);

			if(!string.IsNullOrEmpty(path))
				configuration = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(path));

			GetResolver(instance.GetType()).Attach(instance, configuration, options);
		}
		#endregion

		#region 私有方法
		private static IConfigurationResolver GetResolver(Type type)
		{
			var attribute = type.GetConfigurationAttribute();

			if(attribute != null && attribute.ResolverType != null)
				return Activator.CreateInstance(attribute.ResolverType) as IConfigurationResolver ?? ConfigurationResolver.Default;

			return ConfigurationResolver.Default;
		}
		#endregion
	}
}
