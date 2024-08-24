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
using System.ComponentModel;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 表示插件树节点的类型。
	/// </summary>
	public enum PluginTreeNodeType
	{
		/// <summary>空节点(路径节点)，即该节点的<see cref="Zongsoft.Plugins.PluginTreeNode.Value"/>属性为空。</summary>
		[Description("空节点")]
		Empty,

		/// <summary>构件节点，即该节点的<see cref="Zongsoft.Plugins.PluginTreeNode.Value"/>属性值的类型为<seealso cref="Zongsoft.Plugins.Builtin"/>。</summary>
		[Description("构件节点")]
		Builtin,

		/// <summary>自定义节点，即该节点对应的值为内部挂载的自定义对象。</summary>
		[Description("对象节点")]
		Custom,
	}
}
