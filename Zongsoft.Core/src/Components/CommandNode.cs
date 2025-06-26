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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.Components;

[System.Reflection.DefaultMember(nameof(Children))]
[System.ComponentModel.DefaultProperty(nameof(Children))]
public partial class CommandNode : Zongsoft.Collections.HierarchicalNode<CommandNode>
{
	#region 静态变量
	private static readonly Regex _regex = new(@"^\s*((?<prefix>/|\.{1,2})/?)?(\s*(?<part>[^\.\\/]+|\.{2})?\s*[/.]?\s*)*", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
	#endregion

	#region 私有变量
	private int _childrenLoaded;
	#endregion

	#region 成员字段
	private ICommand _command;
	private CommandNode _parent;
	private readonly HashSet<string> _aliases;
	private readonly CommandNodeCollection _children;
	#endregion

	#region 构造函数
	public CommandNode()
	{
		_children = new CommandNodeCollection(this);
		_aliases = new(StringComparer.OrdinalIgnoreCase);
	}

	public CommandNode(string name) : base(name)
	{
		_children = new CommandNodeCollection(this);
		_aliases = new(StringComparer.OrdinalIgnoreCase);
	}

	public CommandNode(ICommand command) : base(command?.Name)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		_aliases = new(StringComparer.OrdinalIgnoreCase);
		_children = new CommandNodeCollection(this);

		//通过命令属性设置器来初始化别名集
		this.Command = command;
	}
	#endregion

	#region 公共属性
	public ICommand Command
	{
		get => _command;
		set
		{
			//将原命令的别名从节点别名集中移除
			_aliases.ExceptWith(AliasAttribute.GetAliases(_command) ?? []);

			//设置命令对象
			_command = value;

			//将新命令的别名添加到节点别名集中
			_aliases.UnionWith(AliasAttribute.GetAliases(_command) ?? []);
		}
	}

	public ISet<string> Aliases => _aliases;
	public ICommandLoader Loader { get; set; }

	public CommandNodeCollection Children
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
	/// <returns>返回查找的结果，如果为空则表示没有找到指定路径的<see cref="CommandNode"/>命令节点。</returns>
	/// <remarks>
	///		<para>如果路径以斜杠(/)打头则从根节点开始查找；如果以双点(../)打头则表示从上级节点开始查找；否则从当前节点开始查找。</para>
	/// </remarks>
	public CommandNode Find(string path)
	{
		if(string.IsNullOrWhiteSpace(path))
			return this;

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

		return base.FindNode(string.Join('/', parts ?? [path]), token =>
		{
			if(token.Current != null)
				return token.Current;

			if(token.Parent != null)
			{
				foreach(var child in token.Parent.Children)
				{
					if(child.Aliases.Contains(token.Name))
						return child;
				}
			}

			return null;
		});
	}

	public CommandNode Find(ICommand command, bool rooting = false)
	{
		if(command == null)
			return null;

		//向上查找（往根节点方向）
		if(rooting)
			return FindUp(this, node => node.Command == command);

		//确保当前加载器已经被加载过
		this.EnsureChildren();

		return FindDown(this, node => node.Command == command);
	}

	public TCommand Find<TCommand>(bool rooting = false) where TCommand : class, ICommand
	{
		static bool Predicate(CommandNode node)
		{
			var command = node.Command;

			if(command == null)
				return false;

			if(typeof(TCommand).IsInterface || typeof(TCommand).IsAbstract)
				return typeof(TCommand).IsAssignableFrom(command.GetType());
			else
				return typeof(TCommand) == command.GetType();
		}

		//向上查找（往根节点方向）
		if(rooting)
			return (TCommand)FindUp(this, Predicate)?.Command;

		//确保当前加载器已经被加载过
		this.EnsureChildren();

		return (TCommand)FindDown(this, Predicate)?.Command;
	}

	public CommandNode Find(Predicate<CommandNode> predicate, bool rooting = false)
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
	protected void LoadChildren()
	{
		var loader = this.Loader;

		if(loader != null && (!loader.IsLoaded))
			loader.Load(this);
	}

	protected override CommandNode Parent => _parent;
	protected override Collections.IHierarchicalNodeCollection<CommandNode> Nodes => this.Children;
	protected override string GetPath() => _parent == null ? string.Empty : _parent.FullPath;
	protected override void OnFinding(ReadOnlySpan<char> path) => this.EnsureChildren();

	public override string ToString() => _aliases.Count == 0 ? base.ToString() : $"{this.FullPath}({string.Join(',', _aliases)})";
	#endregion

	#region 内部方法
	internal void SetParent(CommandNode parent) => _parent = parent;
	#endregion

	#region 私有方法
	/// <summary>确认子节点集合是否被加载，如果未曾被加载则加载子节点集合。</summary>
	/// <returns>如果子节点集合未曾被加载则加载当前子节点集合并返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	/// <remarks>在<seealso cref="LoadChildren"/>方法中会调用该方法以确保子节点被加载。</remarks>
	protected bool EnsureChildren()
	{
		var childrenLoaded = System.Threading.Interlocked.Exchange(ref _childrenLoaded, 1);

		if(childrenLoaded == 0)
			this.LoadChildren();

		return childrenLoaded == 0;
	}

	private static CommandNode FindUp(CommandNode current, Predicate<CommandNode> predicate)
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

	private static CommandNode FindDown(CommandNode current, Predicate<CommandNode> predicate)
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
