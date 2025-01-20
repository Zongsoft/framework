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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.Configuration;

partial class ConnectionSettingsDriver<TOptions, TDescriptors>
{
	protected abstract class PopulatorBase
	{
		#region 私有变量
		private readonly Dictionary<string, MemberInfo> _members = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 构造函数
		protected PopulatorBase(ConnectionSettingsDriver<TOptions, TDescriptors> driver)
		{
			this.Driver = driver ?? throw new ArgumentNullException(nameof(driver));
			this.Descriptors = (TDescriptors)this.Driver.GetType()
				.GetProperty(nameof(IConnectionSettingsDriver.Descriptors), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.GetValue(null);

			var members = typeof(TOptions).GetMembers(BindingFlags.Instance | BindingFlags.Public);

			for(int i = 0; i < members.Length; i++)
			{
				var usabled = members[i].MemberType switch
				{
					MemberTypes.Field => !((FieldInfo)members[i]).IsInitOnly,
					MemberTypes.Property => ((PropertyInfo)members[i]).CanWrite,
					_ => false,
				};

				if(usabled)
				{
					_members.TryAdd(members[i].Name, members[i]);

					if(this.Descriptors.TryGetValue(members[i].Name, out var descriptor))
					{
						_members.TryAdd(descriptor.Name, members[i]);

						if(!string.IsNullOrEmpty(descriptor.Alias))
							_members.TryAdd(descriptor.Alias, members[i]);
					}
				}
			}
		}
		#endregion

		#region 保护字段
		protected readonly ConnectionSettingsDriver<TOptions, TDescriptors> Driver;
		protected readonly TDescriptors Descriptors;
		#endregion

		#region 公共方法
		public TOptions Populate(IConnectionSettings settings)
		{
			if(settings == null)
				throw new ArgumentNullException(nameof(settings));

			if(!((IConnectionSettingsDriver)this.Driver).IsDriver(settings.Driver?.Name))
				throw new InvalidOperationException($"The specified '{settings}' connection settings is not a {this.Driver.Name} configuration.");

			var options = this.Create(settings);

			foreach(var setting in settings)
			{
				if(this.Descriptors.TryGetValue(setting.Key, out var descriptor))
					this.OnPopulate(ref options, descriptor, setting.Value);
			}

			return options;
		}
		#endregion

		#region 保护方法
		protected virtual TOptions Create(IConnectionSettings settings) => Activator.CreateInstance<TOptions>();
		protected virtual bool OnPopulate(ref TOptions model, ConnectionSettingDescriptor descriptor, object value)
		{
			if(_members.TryGetValue(descriptor.Name, out var member))
				return Common.Convert.TryConvertValue(value ?? descriptor.DefaultValue, descriptor.Type, out value) && Reflection.Reflector.TrySetValue(member, ref model, value);

			if(!string.IsNullOrEmpty(descriptor.Alias) && _members.TryGetValue(descriptor.Alias, out member))
				return Common.Convert.TryConvertValue(value ?? descriptor.DefaultValue, descriptor.Type, out value) && Reflection.Reflector.TrySetValue(member, ref model, value);

			return false;
		}
		#endregion
	}
}
