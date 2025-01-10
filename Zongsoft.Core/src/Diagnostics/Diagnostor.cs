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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Diagnostics;

[DefaultMember(nameof(Configurators))]
[DefaultProperty(nameof(Configurators))]
public partial class Diagnostor
{
	#region 构造函数
	public Diagnostor(string name, Configurator configurator = null)
	{
		this.Name = name ?? string.Empty;
		configurator?.Configure(this);
	}

	public Diagnostor(string name, Filtering meters, Filtering traces)
	{
		this.Name = name ?? string.Empty;
		this.Meters = meters;
		this.Traces = traces;
	}
	#endregion

	#region 静态属性
	private static volatile Dictionary<string, Type> _configurators;
	public static IDictionary<string, Type> Configurators
	{
		get
		{
			if(_configurators == null)
			{
				if(Interlocked.CompareExchange(ref _configurators, new(StringComparer.OrdinalIgnoreCase), null) == null)
				{
					var assemblies = AppDomain.CurrentDomain.GetAssemblies();

					for(int i = 0; i < assemblies.Length; i++)
					{
						if(assemblies[i].IsDynamic)
							continue;

						foreach(var type in assemblies[i].DefinedTypes)
						{
							if(type.IsPublic && type.IsClass && !type.IsAbstract && (typeof(Configurator).IsAssignableFrom(type) || typeof(ConfiguratorFactory).IsAssignableFrom(type)))
							{
								var attribute = type.GetCustomAttribute<ConfiguratorAttribute>(true);

								if(attribute != null && attribute.Name != null)
									_configurators.TryAdd(attribute.Name, type);
								else
									_configurators.TryAdd(type.Name.EndsWith(nameof(Configurator)) ? type.Name[..^nameof(Configurator).Length] : type.Name, type);
							}
						}
					}
				}
			}

			return _configurators;
		}
	}
	#endregion

	#region 实例属性
	public string Name { get; }
	public Filtering Meters { get; set; }
	public Filtering Traces { get; set; }
	#endregion

	#region 嵌套子类
	public class Filtering
	{
		public Filtering(IEnumerable<string> filters, IEnumerable<KeyValuePair<string, string>> exporters = null)
		{
			this.Filters = new HashSet<string>(filters ?? []);
			this.Exporters = new Dictionary<string, string>(exporters ?? [], StringComparer.OrdinalIgnoreCase);
		}

		public ICollection<string> Filters { get; }
		public IDictionary<string, string> Exporters { get; }
	}

	[TypeConverter(typeof(ConfiguratorConverter))]
	public abstract class Configurator
	{
		public abstract void Configure(Diagnostor diagnostor);
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ConfiguratorAttribute(string name = null) : Attribute
	{
		public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
	}

	public abstract class ConfiguratorFactory
	{
		public abstract Configurator Create(string argument);
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ConfiguratorFactoryAttribute(Type factory) : Attribute
	{
		public Type Factory { get; } = factory ?? throw new ArgumentNullException(nameof(factory));

		public Configurator Create(string argument)
		{
			if(this.Factory == null || !typeof(ConfiguratorFactory).IsAssignableFrom(this.Factory))
				return null;

			var factory = Activator.CreateInstance(this.Factory) as ConfiguratorFactory;
			return factory?.Create(argument);
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ConfiguratorFactoryAttribute<T>() : ConfiguratorFactoryAttribute(typeof(T)) { }

	private sealed class ConfiguratorConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text)
			{
				var index = text.IndexOf(':');
				var name = index > 0 && index < text.Length ? text[..index] : string.Empty;

				if(Configurators.TryGetValue(name, out var type) && type != null)
				{
					if(typeof(Configurator).IsAssignableFrom(type))
					{
						var attribute = type.GetCustomAttribute<ConfiguratorFactoryAttribute>(true);

						if(attribute != null && attribute.Factory != null)
							return attribute.Create(text[(index + 1)..]);

						return Activator.CreateInstance(type, text[(index + 1)..]);
					}
					else if(typeof(ConfiguratorFactory).IsAssignableFrom(type))
					{
						var factory = (ConfiguratorFactory)Activator.CreateInstance(type);
						return factory.Create(text[(index + 1)..]);
					}
				}
			}

			return null;
		}
	}
	#endregion
}
