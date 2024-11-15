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
 * Copyright (C) 2020-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;

namespace Zongsoft.Collections
{
	/// <summary>
	/// 表示层次结构的节点类。
	/// </summary>
	public abstract class HierarchicalNode : IHierarchicalNode, IEquatable<HierarchicalNode>
	{
		#region 常量定义
		public const char PathSeparator = '/';
		#endregion

		#region 静态常量
		public static readonly char[] IllegalCharacters = ['/', '\\', '*', '?', '!', '@', '#', '$', '%', '^', '&'];
		#endregion

		#region 私有变量
		private int _childrenLoaded;
		#endregion

		#region 成员变量
		private readonly string _name;
		private readonly string _path;
		private int? _hashCode;
		#endregion

		#region 构造函数
		protected HierarchicalNode() => _name = "/";
		protected HierarchicalNode(string name)
		{
			if(string.IsNullOrEmpty(name) || name == $"{PathSeparator}")
				_name = name;
			else
			{
				if(name.IndexOfAny(IllegalCharacters) >= 0)
					throw new ArgumentException("The name contains illegal character(s).");

				_name = name.Trim();
			}
		}
		#endregion

		#region 公共属性
		/// <summary>获取层次结构节点的名称，名称不可为空或空字符串，根节点的名称固定为斜杠(即“/”)。</summary>
		public virtual string Name => _name;

		/// <summary>获取层次结构节点的路径。</summary>
		/// <remarks>如果为根节点则返回空字符串(<c>String.Empty</c>)，否则即为父节点的全路径。</remarks>
		public string Path => _path ?? this.GetPath();

		/// <summary>获取层次结构节点的完整路径，即节点路径与名称的组合。</summary>
		public string FullPath
		{
			get
			{
				var path = this.Path;

				if(string.IsNullOrEmpty(path))
					return _name;

				if(path.Length == 1 && path[0] == PathSeparator)
					return path + _name;
				else
					return path + PathSeparator + _name;
			}
		}
		#endregion

		#region 重写方法
		public bool Equals(HierarchicalNode other) => other is not null && string.Equals(this.FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is HierarchicalNode other && this.Equals(other);
		public override int GetHashCode() => _hashCode ??= this.FullPath.ToUpperInvariant().GetHashCode();
		public override string ToString() => this.FullPath;
		#endregion

		#region 抽象方法
		protected abstract string GetPath();
		#endregion

		#region 保护方法
		internal protected HierarchicalNode FindNode(string path, Func<HierarchicalNodeToken, HierarchicalNode> onStep = null) => this.FindNode(path, 0, 0, onStep);
		internal protected HierarchicalNode FindNode(string path, int startIndex, int length = 0, Func<HierarchicalNodeToken, HierarchicalNode> onStep = null)
		{
			//注意：一定要确保空字符串路径是返回自身
			if(path == null || path.Length == 0)
				return this;

			if(startIndex < 0 || startIndex >= path.Length)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if(length > 0 && length > path.Length - startIndex)
				throw new ArgumentOutOfRangeException(nameof(length));

			//确保当前子节点集合已经被加载过
			this.EnsureChildren();

			//当前节点默认为本节点
			var current = this;

			int last = startIndex;
			int spaces = 0;
			int index = 0;

			for(int i = startIndex; i < (length > 0 ? length : path.Length - startIndex); i++)
			{
				if(path[i] == PathSeparator)
				{
					if(index++ == 0)
					{
						if(last == i)
							current = this.FindRoot();
					}

					if(i - last > spaces)
					{
						current = FindStep(current, index - 1, path, i, last, spaces, onStep);

						if(current == null)
							return null;
					}

					spaces = -1;
					last = i + 1;
				}
				else if(char.IsWhiteSpace(path, i))
				{
					if(i == last)
						last = i + 1;
					else
						spaces++;
				}
				else
				{
					spaces = 0;
				}
			}

			if(last < path.Length - spaces - 1)
				current = FindStep(current, index, path, path.Length, last, spaces, onStep);

			return current;
		}

		internal protected HierarchicalNode FindNode(string[] parts, Func<HierarchicalNodeToken, HierarchicalNode> onStep = null)
		{
			if(parts == null || parts.Length == 0)
				return null;

			return this.FindNode(string.Join(PathSeparator.ToString(), parts), onStep);
		}

		internal protected HierarchicalNode FindNode(string[] parts, int startIndex, int count = 0, Func<HierarchicalNodeToken, HierarchicalNode> onStep = null)
		{
			if(parts == null || parts.Length == 0)
				return null;

			if(startIndex < 0 || startIndex >= parts.Length)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if(count > 0 && count > parts.Length - startIndex)
				throw new ArgumentOutOfRangeException(nameof(count));

			return this.FindNode(string.Join(PathSeparator.ToString(), parts, startIndex, (count > 0 ? count : parts.Length - startIndex)), onStep);
		}
		#endregion

		#region 虚拟方法
		/// <summary>确认子节点集合是否被加载，如果未曾被加载则加载子节点集合。</summary>
		/// <returns>如果子节点集合未曾被加载则加载当前子节点集合并返回真(true)，否则返回假(false)。</returns>
		/// <remarks>在<seealso cref="LoadChildren"/>方法中会调用该方法以确保子节点被加载。</remarks>
		protected bool EnsureChildren()
		{
			var childrenLoaded = System.Threading.Interlocked.Exchange(ref _childrenLoaded, 1);

			if(childrenLoaded == 0)
				this.LoadChildren();

			return childrenLoaded == 0;
		}

		/// <summary>加载当前节点的子节点集合。</summary>
		protected virtual void LoadChildren() { }

		/// <summary>获取指定名称的子节点对象。</summary>
		/// <param name="name">指定要查找的子节点名称。</param>
		/// <returns>如果找到指定名称的子节点则返回它，否则返回空(null)。</returns>
		protected abstract HierarchicalNode GetChild(string name);
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static HierarchicalNode FindStep(HierarchicalNode current, int index, string path, int position, int last, int spaces, Func<HierarchicalNodeToken, HierarchicalNode> onStep)
		{
			var part = path.Substring(last, position - last - spaces);
			HierarchicalNode parent = null;

			switch(part)
			{
				case "":
				case ".":
					return current;
				case "..":
					if(current._parent != null)
						current = current._parent;

					break;
				default:
					parent = current;
					current = parent.GetChild(part);
					break;
			}

			if(onStep != null)
				current = onStep(new HierarchicalNodeToken(index, part, current, parent));

			return current;
		}
		#endregion

		#region 嵌套子类
		internal protected readonly struct HierarchicalNodeToken
		{
			public readonly string Name;
			public readonly int Index;
			public readonly HierarchicalNode Parent;
			public readonly HierarchicalNode Current;

			internal HierarchicalNodeToken(int index, string name, HierarchicalNode current, HierarchicalNode parent = null)
			{
				this.Index = index;
				this.Name = name;
				this.Current = current;
				this.Parent = parent ?? (current?.InnerParent);
			}

			public override string ToString() => $"{this.Name}#{this.Index}";
		}
		#endregion
	}
}
