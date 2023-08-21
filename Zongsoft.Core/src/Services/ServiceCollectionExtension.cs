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
using System.Reflection;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	public static class ServiceCollectionExtension
	{
		#region 私有变量
		private static readonly TypeInfo ObjectType = typeof(object).GetTypeInfo();
		private static readonly MethodInfo ConfigureMethod = typeof(OptionsConfigurationExtension)
			.GetMethod(nameof(OptionsConfigurationExtension.Configure), 1,
				BindingFlags.Public | BindingFlags.Static,
				null,
				new[] { typeof(IServiceCollection), typeof(string), typeof(IConfiguration) },
				null);
		#endregion

		#region 公共方法
		public static void Register(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
		{
			if(assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			foreach(var exportedType in assembly.ExportedTypes)
			{
				var type = exportedType.GetTypeInfo();

				//如果是非公共类（含抽象类）则忽略
				if(type.IsNotPublic || !type.IsClass || (type.IsAbstract && !type.IsSealed))
					continue;

				//使用 IServiceRegistration 服务注册器注册服务
				if(RegisterServices(services, type, configuration))
					continue;

				//获取服务注册注解
				var attribute = type.GetCustomAttribute<ServiceAttribute>(true);

				//尝试注册服务
				if(attribute != null)
					RegisterServices(services, type, attribute);

				//尝试注入配置
				if(configuration != null)
					RegisterOptions(services, type, configuration);
			}
		}
		#endregion

		#region 私有方法
		private static void RegisterOptions(IServiceCollection services, TypeInfo type, IConfiguration configuration)
		{
			static Type GetOptionType(Type type)
			{
				return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOptions<>) ?
				       type.GenericTypeArguments[0] : type;
			}

			do
			{
				var properties = type.DeclaredProperties.Where(p => p.CanRead && p.CanWrite && p.IsDefined(typeof(Configuration.Options.OptionsAttribute), true));

				foreach(var property in properties)
				{
					var attribute = property.GetCustomAttribute<Configuration.Options.OptionsAttribute>(true);

					if(attribute != null)
					{
						var method = ConfigureMethod.MakeGenericMethod(GetOptionType(property.PropertyType));
						method.Invoke(null, new object[] { services, attribute.Name, configuration.GetSection(Configuration.ConfigurationUtility.GetConfigurationPath(attribute.Path)) });
					}
				}

				var fields = type.DeclaredFields.Where(f => f.IsPublic && !f.IsInitOnly && f.IsDefined(typeof(Configuration.Options.OptionsAttribute), true));

				foreach(var field in fields)
				{
					var attribute = field.GetCustomAttribute<Configuration.Options.OptionsAttribute>(true);

					if(attribute != null)
					{
						var method = ConfigureMethod.MakeGenericMethod(GetOptionType(field.FieldType));
						method.Invoke(null, new object[] { services, attribute.Name, configuration.GetSection(Configuration.ConfigurationUtility.GetConfigurationPath(attribute.Path)) });
					}
				}

				type = type.BaseType?.GetTypeInfo();
			} while(type != null && type.GetTypeInfo() != ObjectType);
		}

		private static bool RegisterServices(IServiceCollection services, TypeInfo type, IConfiguration configuration)
		{
			if(typeof(IServiceRegistration).IsAssignableFrom(type))
			{
				var registration = (IServiceRegistration)Activator.CreateInstance(type);
				registration.Register(services, configuration);
				return true;
			}

			return false;
		}

		private static void RegisterServices(IServiceCollection services, TypeInfo type, ServiceAttribute attribute)
		{
			if(type.IsAbstract)
			{
				if(type.IsSealed)
					RegisterStaticMember(services, type, attribute.Members);

				return;
			}

			services.AddSingleton((Type)type);

			if(!string.IsNullOrEmpty(attribute.Name))
				ServiceProviderExtension.Register(attribute.Name, type);

			if(attribute.Contracts != null)
			{
				var contracts = attribute.Contracts;
				var moduleName = ServiceUtility.Modular.GetModuleName(type);

				if(!string.IsNullOrEmpty(moduleName))
				{
					for(var i = 0; i < contracts.Length; i++)
					{
						var modular = ModularServiceUtility.GetModularService(moduleName, contracts[i], type);
						services.AddSingleton(modular.GetType(), modular);
					}
				}

				for(var i = 0; i < contracts.Length; i++)
				{
					services.AddSingleton(contracts[i], services => services.GetService(type));
				}
			}
		}

		private static void RegisterStaticMember(IServiceCollection services, TypeInfo type, string members)
		{
			if(string.IsNullOrEmpty(members))
				return;

			var moduleName = ServiceUtility.Modular.GetModuleName(type);

			foreach(var member in Zongsoft.Common.StringExtension.Slice(members, ','))
			{
				var property = type.GetProperty(member, BindingFlags.Static | BindingFlags.Public);

				if(property != null)
				{
					var value = property.GetValue(null);

					if(!string.IsNullOrEmpty(moduleName))
					{
						var modular = ModularServiceUtility.GetModularService(moduleName, property.PropertyType, value);
						services.AddSingleton(modular.GetType(), modular);
					}

					services.AddSingleton(property.PropertyType, value);
					continue;
				}

				var field = type.GetField(member, BindingFlags.Static | BindingFlags.Public);

				if(field != null)
				{
					var value = field.GetValue(null);

					if(!string.IsNullOrEmpty(moduleName))
					{
						var modular = ModularServiceUtility.GetModularService(moduleName, field.FieldType, value);
						services.AddSingleton(modular.GetType(), modular);
					}

					services.AddSingleton(field.FieldType, value);
					continue;
				}
			}
		}
		#endregion
	}
}
