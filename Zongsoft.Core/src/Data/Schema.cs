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
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示由指定成员类型构成的数据模式。</summary>
/// <typeparam name="TMember">数据模式成员类型。</typeparam>
public abstract class Schema<TMember> : ISchema<TMember> where TMember : SchemaMemberBase
{
	#region 构造函数
	protected Schema(string name, string text, Type modelType, IEnumerable<TMember> members = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Text = text ?? throw new ArgumentNullException(nameof(text));
		this.ModelType = modelType;
		this.Members = new SchemaMemberCollection<TMember>(members);
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Text { get; }
	public Type ModelType { get; }
	public bool IsReadOnly { get; set; }
	public bool IsEmpty => this.Members == null || this.Members.Count == 0;
	public SchemaMemberCollection<TMember> Members { get; }
	#endregion

	#region 公共方法
	public void Clear()
	{
		if(!this.IsReadOnly)
			this.Members?.Clear();
	}

	public bool Contains(string path) => this.Find(path) != null;

	public TMember Find(string path)
	{
		if(!TryGetParts(path, out var parts) || this.IsEmpty)
			return null;

		TMember current = null;

		for(int i = 0; i < parts.Length; i++)
		{
			if(i == 0)
			{
				if(!this.Members.TryGetValue(parts[i], out current))
					return null;
			}
			else if(!current.TryGetChild(parts[i], out var child) || child is not TMember member)
				return null;
			else
				current = member;
		}

		return current;
	}

	public ISchema<TMember> Include(string expression)
	{
		if(this.IsReadOnly || string.IsNullOrWhiteSpace(expression))
			return this;

		var count = 0;
		var characters = expression.ToCharArray();

		for(int i = 0; i < characters.Length; i++)
		{
			if(characters[i] == '.' || characters[i] == '/')
			{
				characters[i] = '{';
				count++;
			}
		}

		var members = this.OnInclude(count == 0 ? expression : new string(characters) + new string('}', count));

		if(members != null)
		{
			foreach(var member in members)
			{
				if(member != null && !this.Members.Contains(member.Name))
					this.Members.Add(member);
			}
		}

		return this;
	}

	public ISchema<TMember> Exclude(string path)
	{
		this.Exclude(path, out _);
		return this;
	}

	public bool Exclude(string path, out TMember member)
	{
		member = null;

		if(this.IsReadOnly || !TryGetParts(path, out var parts) || this.IsEmpty)
			return false;

		TMember parent = null;
		TMember current = null;

		for(int i = 0; i < parts.Length; i++)
		{
			if(i == 0)
			{
				if(!this.Members.TryGetValue(parts[i], out current))
					return false;
			}
			else if(!current.TryGetChild(parts[i], out var child) || child is not TMember nested)
				return false;
			else
			{
				parent = current;
				current = nested;
			}
		}

		if(parent == null)
		{
			if(!this.Members.Remove(current.Name))
				return false;
		}
		else
		{
			parent.RemoveChild(current.Name);
			this.Prune(parent);
		}

		member = current;
		return true;
	}
	#endregion

	#region 保护方法
	protected abstract IEnumerable<TMember> OnInclude(string expression);
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(this.IsEmpty)
			return string.Empty;

		var text = new StringBuilder();

		foreach(var member in this.Members)
		{
			if(text.Length > 0)
				text.Append(',');

			WriteMember(text, member);
		}

		return text.ToString();
	}
	#endregion

	#region 私有方法
	private void Prune(TMember member)
	{
		while(member != null && !member.HasChildren)
		{
			if(member.Parent is not TMember parent)
			{
				this.Members.Remove(member.Name);
				return;
			}

			parent.RemoveChild(member.Name);
			member = parent;
		}
	}

	private static bool TryGetParts(string path, out string[] parts)
	{
		parts = null;

		if(string.IsNullOrWhiteSpace(path))
			return false;

		parts = path.Split(['.', '/'], StringSplitOptions.None);

		for(int i = 0; i < parts.Length; i++)
		{
			if(string.IsNullOrWhiteSpace(parts[i]))
				return false;
		}

		return parts.Length > 0;
	}

	private static void WriteMember(StringBuilder text, SchemaMemberBase member)
	{
		text.Append(member.Name);

		if(member.Limit > 0)
		{
			text.Append(':');
			text.Append(member.Limit);
		}

		if(member.Sortings != null && member.Sortings.Length > 0)
		{
			text.Append('(');

			for(int i = 0; i < member.Sortings.Length; i++)
			{
				if(i > 0)
					text.Append(',');

				if(member.Sortings[i].Mode == SortingMode.Descending)
					text.Append('~');

				text.Append(member.Sortings[i].Name);
			}

			text.Append(')');
		}

		if(member.HasChildren)
		{
			var index = 0;
			text.Append('{');

			foreach(var child in member.Children)
			{
				if(index++ > 0)
					text.Append(',');

				WriteMember(text, (SchemaMemberBase)child);
			}

			text.Append('}');
		}
	}
	#endregion

	#region 显式实现
	SchemaMemberBase ISchema.Find(string path) => this.Find(path);
	ISchema ISchema.Include(string path) => this.Include(path);
	ISchema ISchema.Exclude(string path) => this.Exclude(path);
	bool ISchema.Exclude(string path, out SchemaMemberBase member)
	{
		if(this.Exclude(path, out var result))
		{
			member = result;
			return true;
		}

		member = null;
		return false;
	}
	#endregion
}
