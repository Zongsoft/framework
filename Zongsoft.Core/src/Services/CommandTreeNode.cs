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
using System.Text.RegularExpressions;

namespace Zongsoft.Services
{
	[System.Reflection.DefaultMember(nameof(Children))]
	public class CommandTreeNode : Zongsoft.Collections.HierarchicalNode
	{
		#region 私有变量
		private readonly Regex _regex = new Regex(@"^\s*((?<prefix>/|\.{1,2})/?)?(\s*(?<part>[^\.\\/]+|\.{2})?\s*[/.]?\s*)*", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
		#endregion

		#region 成员字段
		private ICommand _command;
		private ICommandLoader _loader;
		private CommandTreeNodeCollection _children;
		#endregion

		#region 构造函数
		public CommandTreeNode()
		{
			_children = new CommandTreeNodeCollection(this);
		}

		public CommandTreeNode(string name) : base(name)
		{
			_children = new CommandTreeNodeCollection(this);
		}

		public CommandTreeNode(string name, CommandTreeNode parent) : base(name, parent)
		{
			_children = new CommandTreeNodeCollection(this);
		}

		public CommandTreeNode(ICommand command, CommandTreeNode parent = null) : base(command.Name, parent)
		{
			_command = command ?? throw new ArgumentNullException(nameof(command));
			_children = new CommandTreeNodeCollection(this);
		}
		#endregion

		#region 公共属性
		public ICommand Command
		{
			get => _command;
			set => _command = value;
		}

		public ICommandLoader Loader
		{
			get => _loader;
			set => _loader = value;
		}

		public CommandTreeNode Parent => (CommandTreeNode)this.InnerParent;
		public CommandTreeNodeCollection Children
		{
			get
			{
				//确保当前加载器已经被加载过
				this.EnsureChildren();

				//返回子节点集
				return _children;
			}
		}
		#endregion

		#region 公共方法
		/// <summary>查找指定的命令路径的命令节点。</summary>
		/// <param name="path">指定的命令路径。</param>
		/// <returns>返回查找的结果，如果为空则表示没有找到指定路径的<see cref="CommandTreeNode"/>命令节点。</returns>
		/// <remarks>
		///		<para>如果路径以斜杠(/)打头则从根节点开始查找；如果以双点(../)打头则表示从上级节点开始查找；否则从当前节点开始查找。</para>
		/// </remarks>
		public CommandTreeNode Find(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return null;

			var match = _regex.Match(path);
			string[] parts = null;

			if(match.Success)
			{
				int offset = string.IsNullOrWhiteSpace(match.Groups["prefix"].Value) ? 0 : 1;

				if(offset == 0)
				{
					parts = new string[match.Groups["part"].Captures.Count];
				}
				else
				{
					parts = new string[match.Groups["part"].Captures.Count + 1];
					parts[0] = match.Groups["prefix"].Value;
				}

				for(int i = 0; i < match.Groups["part"].Captures.Count; i++)
				{
					parts[i + offset] = match.Groups["part"].Captures[i].Value;
				}
			}

			if(parts == null)
				parts = new string[] { path };

			return (CommandTreeNode)base.FindNode(parts, null);
		}

		public CommandTreeNode Find(ICommand command, bool rooting = false)
		{
			if(command == null)
				return null;

			//向上查找（往根节点方向）
			if(rooting)
				return FindUp(this, node => node._command == command);

			//确保当前加载器已经被加载过
			this.EnsureChildren();

			return FindDown(this, node => node._command == command);
		}

		public TCommand Find<TCommand>(bool rooting = false) where TCommand : class, ICommand
		{
			static bool Predicate(CommandTreeNode node)
			{
				var command = node._command;

				if(command == null)
					return false;

				if(typeof(TCommand).IsInterface || typeof(TCommand).IsAbstract)
					return typeof(TCommand).IsAssignableFrom(command.GetType());
				else
					return typeof(TCommand) == command.GetType();
			}

			//向上查找（往根节点方向）
			if(rooting)
				return (TCommand)FindUp(this, Predicate)?._command;

			//确保当前加载器已经被加载过
			this.EnsureChildren();

			return (TCommand)FindDown(this, Predicate)?._command;
		}

		public CommandTreeNode Find(Predicate<CommandTreeNode> predicate, bool rooting = false)
		{
			if(predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			//向上查找（往根节点方向）
			if(rooting)
				return FindUp(this, predicate);

			//确保当前加载器已经被加载过
			this.EnsureChildren();

			return FindDown(this, predicate);
		}
		#endregion

		#region 重写方法
		protected override Collections.HierarchicalNode GetChild(string name)
		{
			return _children != null && _children.TryGet(name, out var child) ? child : null;
		}

		protected override void LoadChildren()
		{
			var loader = _loader;

			if(loader != null && (!loader.IsLoaded))
				loader.Load(this);
		}

		public override string ToString() => this.FullPath;
		#endregion

		#region 私有方法
		private static CommandTreeNode FindUp(CommandTreeNode current, Predicate<CommandTreeNode> predicate)
		{
			if(current == null || predicate == null)
				return null;

			while(current != null)
			{
				if(predicate(current))
					return current;

				current = current.Parent;
			}

			return null;
		}

		private static CommandTreeNode FindDown(CommandTreeNode current, Predicate<CommandTreeNode> predicate)
		{
			if(current == null || predicate == null)
				return null;

			if(predicate(current))
				return current;

			foreach(var child in current._children)
			{
				if(FindDown(child, predicate) != null)
					return child;
			}

			return null;
		}
		#endregion
	}
}
