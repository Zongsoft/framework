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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration
{
	public class ConnectionSettingDescriptorCollection() : KeyedCollection<string, ConnectionSettingDescriptor>(StringComparer.OrdinalIgnoreCase)
	{
		private Dictionary<string, ConnectionSettingDescriptor> _aliases;

		protected override string GetKeyForItem(ConnectionSettingDescriptor descriptor) => descriptor.Name;

		public ConnectionSettingDescriptor Add<T>(string name, string defaultValue = null, string label = null, string description = null) => this.Add(name, typeof(T), false, defaultValue, label, description);
		public ConnectionSettingDescriptor Add<T>(string name, bool required, string defaultValue = null, string label = null, string description = null) => this.Add(name, typeof(T), required, defaultValue, label, description);
		public ConnectionSettingDescriptor Add(string name, Type type, string defaultValue = null, string label = null, string description = null) => this.Add(name, type, false, defaultValue, label, description);
		public ConnectionSettingDescriptor Add(string name, Type type, bool required, string defaultValue = null, string label = null, string description = null)
		{
			var descriptor = new ConnectionSettingDescriptor(name, type, required, defaultValue, label, description);
			this.Add(descriptor);
			return descriptor;
		}
	}
}