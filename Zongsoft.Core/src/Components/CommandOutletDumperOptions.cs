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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Components;

public class CommandOutletDumperOptions
{
	#region 构造函数
	public CommandOutletDumperOptions(Func<object, ICommandOutletDumper> selector = null) : this(null, 3, selector) { }
	public CommandOutletDumperOptions(int maximumDepth, Func<object, ICommandOutletDumper> selector = null) : this(null, maximumDepth, selector) { }
	public CommandOutletDumperOptions(string indentString, int maximumDepth, Func<object, ICommandOutletDumper> selector = null)
	{
		this.IndentString = indentString ?? "\t";
		this.MaximumDepth = Math.Max(maximumDepth, 0);
		this.Selector = selector;
		this.Binding = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
	}
	#endregion

	#region 公共属性
	public string IndentString { get; set; }
	public int MaximumDepth { get; set; }
	public BindingFlags Binding { get; set; }
	public Func<object, ICommandOutletDumper> Selector { get; set; }
	#endregion

	#region 内部属性
	internal ReferenceTracker Tracker = new();
	#endregion

	#region 内部方法
	internal string Indent(int value) => string.IsNullOrEmpty(this.IndentString) || value < 1 ?
		string.Empty : string.Concat(System.Linq.Enumerable.Repeat(this.IndentString, value));

	internal MemberInfo[] GetMembers(object target) => target == null ? [] : target.GetType().GetMembers(this.Binding);
	#endregion

	#region 嵌套子类
	internal sealed class ReferenceTracker
	{
		private readonly Stack<object> _stack = new();
		private readonly HashSet<object> _hashset = new(ReferenceComparer.Instance);

		public bool CanTrack(object value) => value != null && value is not string && value.GetType().IsClass;
		public bool Track(object value)
		{
			if(!CanTrack(value))
				throw new InvalidOperationException();

			if(_hashset.Add(value))
			{
				_stack.Push(value);
				return true;
			}

			return false;
		}

		public object Untrack()
		{
			if(_stack.TryPop(out var value))
			{
				_hashset.Remove(value);
				return value;
			}

			return null;
		}

		private sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceComparer Instance = new();
			bool IEqualityComparer<object>.Equals(object x, object y) => object.ReferenceEquals(x, y);
			public int GetHashCode(object obj) => HashCode.Combine(obj);
		}
	}
	#endregion
}
