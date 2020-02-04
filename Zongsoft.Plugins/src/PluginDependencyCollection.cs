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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	public class PluginDependencyCollection : ICollection<PluginDependency>
	{
		#region 成员变量
		private readonly Dictionary<string, PluginDependency> _innerDictionary;
		#endregion

		#region 构造函数
		internal PluginDependencyCollection()
		{
			_innerDictionary = new Dictionary<string, PluginDependency>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count
		{
			get
			{
				return _innerDictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		public string[] Keys
		{
			get
			{
				string[] keys = new string[_innerDictionary.Keys.Count];
				_innerDictionary.Keys.CopyTo(keys, 0);
				return keys;
			}
		}

		public PluginDependency this[string name]
		{
			get
			{
				if(string.IsNullOrWhiteSpace(name))
					throw new ArgumentNullException("name");

				return _innerDictionary[name];
			}
		}
		#endregion

		#region 公共方法
		public bool Contains(string name)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			return _innerDictionary.ContainsKey(name.Trim());
		}

		public bool Contains(PluginDependency depend)
		{
			if(depend == null)
				return false;

			return _innerDictionary.ContainsKey(depend.Name);
		}
		#endregion

		#region 内部方法
		internal bool Any(Func<PluginDependency, bool> func)
		{
			foreach(var dependency in _innerDictionary.Values)
			{
				if(func(dependency))
					return true;
			}

			return false;
		}

		internal void Remove(Plugin item)
		{
			if(item == null)
				return;

			_innerDictionary.Remove(item.Name);
		}

		internal void SetDependency(string pluginName)
		{
			_innerDictionary[pluginName] = new PluginDependency(pluginName);
		}

		internal void SetDependencies(IEnumerable<Plugin> plugins)
		{
			foreach(var name in _innerDictionary.Keys)
			{
				foreach(Plugin plugin in plugins)
				{
					if(string.Equals(name, plugin.Name, StringComparison.OrdinalIgnoreCase))
					{
						_innerDictionary[name].Plugin = plugin;
						break;
					}
				}
			}
		}
		#endregion

		#region 显式实现
		void ICollection<PluginDependency>.Add(PluginDependency item)
		{
			throw new NotSupportedException();
		}

		void ICollection<PluginDependency>.Clear()
		{
			throw new NotSupportedException();
		}

		void ICollection<PluginDependency>.CopyTo(PluginDependency[] array, int arrayIndex)
		{
			_innerDictionary.Values.CopyTo(array, arrayIndex);
		}

		bool ICollection<PluginDependency>.Remove(PluginDependency item)
		{
			throw new NotSupportedException();
		}
		#endregion

		#region 接口实现
		public IEnumerator<PluginDependency> GetEnumerator()
		{
			foreach(PluginDependency value in _innerDictionary.Values)
			{
				yield return value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<PluginDependency>)this).GetEnumerator();
		}
		#endregion
	}
}
