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
using System.Collections.ObjectModel;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 表示插件集合。
	/// </summary>
	public class PluginCollection : KeyedCollection<string, Plugin>
	{
		#region 构造函数
		internal PluginCollection(Plugin owner = null) : base(StringComparer.OrdinalIgnoreCase) => this.Owner = owner;
		#endregion

		#region 公共属性
		public Plugin Owner { get; }
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Plugin plugin) => plugin.Name;
		#endregion

		#region 公共方法
		public Plugin Find(string name)
		{
			if(string.IsNullOrEmpty(name))
				return null;

			if(this.TryGetValue(name, out var plugin))
				return plugin;

			foreach(var item in this.Items)
			{
				if(item.HasChildren)
				{
					var found = item.Children.Find(name);
					if(found != null)
						return found;
				}
			}

			return null;
		}
		#endregion

		#region 内部方法
		/// <summary>将指定的插件对象加入当前的集合中。</summary>
		/// <param name="plugin">带加入的插件对象。</param>
		/// <returns>添加成功则返回真(True)，否则返回假(False)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="plugin"/>参数为空(null)。</exception>
		/// <exception cref="System.InvalidOperationException">当<paramref name="plugin"/>参数的<see cref="Zongsoft.Plugins.Plugin.Parent"/>父插件属性不为空，并且与当前集合的所有者不是同一个引用对象。</exception>
		internal bool TryAdd(Plugin plugin)
		{
			if(plugin == null)
				throw new ArgumentNullException(nameof(plugin));

			if(plugin.Parent != null && (!object.ReferenceEquals(plugin.Parent, this.Owner)))
				throw new InvalidOperationException();

			if(this.Contains(plugin.Name))
				return false;

			base.Add(plugin);

			//返回添加成功
			return true;
		}
		#endregion
	}
}
