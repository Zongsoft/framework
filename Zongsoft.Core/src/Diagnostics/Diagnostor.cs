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
	public static IDictionary<string, Type> Configurators { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
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
		protected Configurator(string name) => this.Name = name ?? string.Empty;
		public string Name { get; }
		public abstract void Configure(Diagnostor diagnostor);
	}

	public abstract class ConfiguratorFactory
	{
		public abstract Configurator Create(string argument);
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ConfiguratorFactoryAttribute : Attribute
	{
		public ConfiguratorFactoryAttribute(Type factory) => this.Factory = factory;
		public Type Factory { get; set; }

		public Configurator Create(string argument)
		{
			if(this.Factory == null || !typeof(Configurator).IsAssignableFrom(this.Factory))
				return null;

			var constructor = this.Factory.GetConstructor([typeof(string)]);
			return (Configurator)(constructor == null ? Activator.CreateInstance(this.Factory) : Activator.CreateInstance(this.Factory, argument));
		}
	}

	private sealed class ConfiguratorConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text)
			{
				var index = text.IndexOf(':');

				if(index > 0 && index < text.Length)
				{
					if(Configurators.TryGetValue(text[..index], out var type) && type != null)
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
			}

			return null;
		}
	}
	#endregion
}
