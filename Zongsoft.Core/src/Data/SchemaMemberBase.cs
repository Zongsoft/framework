﻿/*
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

namespace Zongsoft.Data;

public abstract class SchemaMemberBase : IEquatable<SchemaMemberBase>
{
	#region 单例字段
	internal static readonly SchemaMemberBase Ignores = new EmptyMember();
	#endregion

	#region 成员字段
	private Sorting[] _sortingArray;
	private HashSet<Sorting> _sortings;
	#endregion

	#region 构造函数
	protected SchemaMemberBase() { }
	protected SchemaMemberBase(string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
	}
	#endregion

	#region 公共属性
	public virtual string Name { get; }
	public string Path
	{
		get
		{
			var parent = this.GetParent();

			if(parent == null)
				return string.Empty;
			else
			{
				if(string.IsNullOrEmpty(parent.Path))
					return parent.Name;
				else
					return parent.Path + "." + parent.Name;
			}
		}
	}

	public string FullPath
	{
		get
		{
			var path = this.Path;

			if(string.IsNullOrEmpty(path))
				return this.Name;
			else
				return path + "." + this.Name;
		}
	}

	public Paging Paging { get; internal set; }
	public Sorting[] Sortings => _sortingArray;
	public abstract bool HasChildren { get; }
	#endregion

	#region 抽象方法
	protected abstract SchemaMemberBase GetParent();
	protected abstract void SetParent(SchemaMemberBase parent);

	internal protected abstract bool TryGetChild(string name, out SchemaMemberBase child);
	internal protected abstract void AddChild(SchemaMemberBase child);
	internal protected abstract void RemoveChild(string name);
	internal protected abstract void ClearChildren();
	#endregion

	#region 内部方法
	internal void AddSorting(Sorting sorting)
	{
		if(_sortings == null)
			System.Threading.Interlocked.CompareExchange(ref _sortings, new HashSet<Sorting>(SortingComparer.Instance), null);

		if(_sortings.Add(sorting))
		{
			var array = new Sorting[_sortings.Count];
			_sortings.CopyTo(array);
			_sortingArray = array;
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(SchemaMemberBase other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is SchemaMemberBase other && this.Equals(other);
	public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
	public override string ToString() => this.FullPath;
	#endregion

	#region 嵌套子类
	private class EmptyMember : SchemaMemberBase
	{
		public override string Name => "?";
		public override bool HasChildren => false;

		protected override SchemaMemberBase GetParent() => null;
		protected override void SetParent(SchemaMemberBase parent) { }
		protected internal override void AddChild(SchemaMemberBase child) { }
		protected internal override void ClearChildren() { }
		protected internal override void RemoveChild(string name) { }
		protected internal override bool TryGetChild(string name, out SchemaMemberBase child)
		{
			child = null;
			return false;
		}
	}

	private sealed class SortingComparer : IEqualityComparer<Sorting>
	{
		public static readonly SortingComparer Instance = new();
		private SortingComparer() { }
		public bool Equals(Sorting x, Sorting y) => string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		public int GetHashCode(Sorting sorting) => sorting.Name.ToLowerInvariant().GetHashCode();
	}
	#endregion
}
