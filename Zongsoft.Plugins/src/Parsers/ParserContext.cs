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

namespace Zongsoft.Plugins.Parsers
{
	public class ParserContext
	{
		#region 构造函数
		internal ParserContext(string scheme, string text, PluginTreeNode node, string memberName, Type memberType, object parameter = null)
		{
			if(string.IsNullOrEmpty(scheme))
				throw new ArgumentNullException(nameof(scheme));

			this.Scheme = scheme;
			this.Text = text ?? string.Empty;
			this.Parameter = parameter;
			this.MemberName = memberName;
			this.MemberType = memberType;
			this.Node = node ?? throw new ArgumentNullException(nameof(node));
		}

		internal ParserContext(string scheme, string text, Builtin builtin, string memberName, Type memberType, object parameter = null)
		{
			if(string.IsNullOrEmpty(scheme))
				throw new ArgumentNullException(nameof(scheme));
			if(builtin == null)
				throw new ArgumentNullException(nameof(builtin));

			this.Scheme = scheme;
			this.Text = text ?? string.Empty;
			this.Parameter = parameter;
			this.MemberName = memberName;
			this.MemberType = memberType;
			this.Node = builtin.Node;
		}
		#endregion

		#region 公共属性
		/// <summary>获取解析文本的方案(即解析器名称)。</summary>
		public string Scheme { get; }

		/// <summary>获取待解析的不包含解析器名的文本。</summary>
		public string Text { get; }

		/// <summary>获取解析器的上下文的输入参数。</summary>
		public object Parameter { get; }

		/// <summary>获取待解析文本所在目标对象的成员名称。</summary>
		public string MemberName { get; }

		/// <summary>获取待解析文本所在目标对象的成员类型。</summary>
		public Type MemberType { get; }

		/// <summary>获取待解析文本所在的构件(<see cref="Builtin"/>)，注意：该属性可能返回空值(null)。</summary>
		public Builtin Builtin => this.Node.NodeType == PluginTreeNodeType.Builtin ? (Builtin)this.Node.Value : null;

		/// <summary>获取待解析文本所在的插件树节点(<see cref="PluginTreeNode"/>)。</summary>
		public PluginTreeNode Node { get; }

		/// <summary>获取待解析文本所在构件或插件树节点所隶属的插件对象，注意：该属性可能返回空值(null)。</summary>
		public Plugin Plugin => this.Node.Plugin;

		public PluginTree PluginTree => this.Node.Tree;
		#endregion
	}
}
