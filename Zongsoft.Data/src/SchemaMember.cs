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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

public class SchemaMember : SchemaMemberBase
{
	#region 成员字段
	private SchemaMember _parent;
	private SchemaMemberCollection<SchemaMember> _children;
	private readonly string _name;
	#endregion

	#region 构造函数
	internal SchemaMember(IDataEntityProperty property, IEnumerable<IDataEntity> ancestors = null)
	{
		this.Token = new DataEntityPropertyToken(property);
		this.Ancestors = ancestors;
		_name = property?.Name ?? throw new ArgumentNullException(nameof(property));
	}

	internal SchemaMember(DataEntityPropertyToken token, IEnumerable<IDataEntity> ancestors = null)
	{
		this.Token = token;
		this.Ancestors = ancestors;
		_name = token.Property?.Name ?? throw new ArgumentException("The mapped schema token requires a property.", nameof(token));
	}

	internal SchemaMember(SchemaMemberDescriptor descriptor, IEnumerable<SchemaMember> dependencies = null)
	{
		this.Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
		this.Token = new DataEntityPropertyToken(null, descriptor.Member);
		this.Dependencies = dependencies == null ? [] : [.. dependencies];
		_name = descriptor.Name;
	}
	#endregion

	#region 公共属性
	public override string Name => _name;
	public override bool Ignored => this.Token.Property == null;
	public SchemaMemberDescriptor Descriptor { get; }
	public IReadOnlyList<SchemaMember> Dependencies { get; } = [];
	public override MemberInfo Member => this.Token.Member;
	public override IDataEntityProperty Property => this.Token.Property;
	public DataEntityPropertyToken Token { get; }
	public new SchemaMember Parent => _parent;
	public IEnumerable<IDataEntity> Ancestors { get; }
	public override bool HasChildren => _children != null && _children.Count > 0;
	public new SchemaMemberCollection<SchemaMember> Children => _children;
	#endregion

	#region 重写方法
	protected override SchemaMemberBase GetParent() => _parent;
	protected override void SetParent(SchemaMemberBase parent) => _parent = (parent as SchemaMember) ?? throw new ArgumentException();
	protected override IEnumerable<SchemaMemberBase> GetChildren() => _children ?? [];

	protected override bool TryGetChild(string name, out SchemaMemberBase child)
	{
		if(_children != null && _children.TryGetValue(name, out var schema))
		{
			child = schema;
			return true;
		}

		child = null;
		return false;
	}

	protected override void AddChild(SchemaMemberBase child)
	{
		if(child is not SchemaMember schema)
			throw new ArgumentException();

		if(_children == null)
			System.Threading.Interlocked.CompareExchange(ref _children, new SchemaMemberCollection<SchemaMember>(), null);

		_children.Add(schema);
		schema._parent = this;
	}

	protected override void RemoveChild(string name) => _children?.Remove(name);
	protected override void ClearChildren() => _children?.Clear();

	public override string ToString()
	{
		var text = this.Name;

		if(this.Paging != null && this.Paging.IsPaged(out var page, out var size))
			text += $":{(page == 1 ? $"{size}" : $"{page}/{size}")}";

		if(this.Sortings != null && this.Sortings.Length > 0)
		{
			var index = 0;
			text += "(";

			foreach(var sorting in this.Sortings)
			{
				if(index++ > 0)
					text += ", ";

				if(sorting.Mode == SortingMode.Ascending)
					text += sorting.Name;
				else
					text += "~" + sorting.Name;
			}

			text += ")";
		}

		if(_children != null && _children.Count > 0)
		{
			var index = 0;
			text += "{";

			foreach(var child in _children)
			{
				if(index++ > 0)
					text += ", ";

				text += child.ToString();
			}

			text += "}";
		}

		return text;
	}
	#endregion

	#region 内部方法
	internal void Append(SchemaMember child) => this.AddChild(child);
	#endregion
}
