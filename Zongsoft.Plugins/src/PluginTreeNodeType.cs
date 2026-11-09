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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Plugins;

/// <summary>表示插件树节点的类型。</summary>
public enum PluginTreeNodeType
{
	/// <summary>空节点(路径节点)，即该节点的 <see cref="Zongsoft.Plugins.PluginTreeNode.Value"/> 属性为空。</summary>
	Empty,

	/// <summary>构件节点，即该节点的 <see cref="Zongsoft.Plugins.PluginTreeNode.Value"/> 属性值的类型为 <seealso cref="Zongsoft.Plugins.Builtin"/>。</summary>
	Builtin,

	/// <summary>自定义节点，即该节点对应的值为内部挂载的自定义对象。</summary>
	Custom,
}
