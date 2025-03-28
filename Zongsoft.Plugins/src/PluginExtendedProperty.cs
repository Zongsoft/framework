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

namespace Zongsoft.Plugins
{
	public class PluginExtendedProperty : PluginElementProperty
	{
		#region 构造函数
		internal PluginExtendedProperty(PluginElement owner, string name, string rawValue, Plugin plugin) : base(owner, name, rawValue)
		{
			this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
		}

		internal PluginExtendedProperty(PluginElement owner, string name, PluginTreeNode valueNode, Plugin plugin) : base(owner, name, valueNode)
		{
			this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前扩展属性的定义插件。</summary>
		/// <remarks>
		///		<para>注意：该属性值表示本扩展属性是由哪个插件扩展的。</para>
		///		<para>因此它未必等同于 <see cref="PluginElementProperty.Owner"/> 属性对应的 <seealso cref="PluginElement"/> 类型中的 <see cref="PluginElement.Plugin"/> 属性值。</para>
		/// </remarks>
		public Plugin Plugin { get; }
		#endregion
	}
}
