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

using Zongsoft.Plugins;

namespace Zongsoft.Services
{
	public class PluginPredicationContext
	{
		#region 构造函数
		public PluginPredicationContext(string parameter, Builtin builtin)
		{
			this.Parameter = parameter;
			this.Builtin = builtin;
			this.Node = builtin.Node;
			this.Plugin = builtin.Plugin;
		}

		public PluginPredicationContext(string parameter, PluginTreeNode node, Plugin plugin)
		{
			this.Parameter = parameter;
			this.Node = node;
			this.Plugin = plugin ?? node.Plugin;
		}

		public PluginPredicationContext(string parameter, Builtin builtin, PluginTreeNode node, Plugin plugin)
		{
			this.Parameter = parameter;
			this.Builtin = builtin;

			if(builtin != null)
			{
				this.Node = builtin.Node;
				this.Plugin = builtin.Plugin;
			}

			if(node != null)
			{
				this.Node = node;
				this.Plugin = plugin ?? node.Plugin;
			}

			if(plugin != null)
				this.Plugin = plugin;
		}
		#endregion

		#region 公共属性
		/// <summary>获传入的参数文本。</summary>
		public string Parameter { get; }

		/// <summary>获取待解析文本所在的构件(<see cref="Builtin"/>)，注意：该属性可能返回空值(null)。</summary>
		public Builtin Builtin { get; }

		/// <summary>获取待解析文本所在的插件树节点(<see cref="PluginTreeNode"/>)，注意：该属性可能返回空值(null)。</summary>
		public PluginTreeNode Node { get; }

		/// <summary>获取待解析文本所在构件或插件树节点所隶属的插件对象，注意：该属性可能返回空值(null)。</summary>
		public Plugin Plugin { get; }
		#endregion
	}
}
