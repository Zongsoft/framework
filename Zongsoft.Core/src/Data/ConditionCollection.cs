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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data;

public class ConditionCollection : System.Collections.ObjectModel.Collection<ICondition>, ICondition, IEquatable<ConditionCollection>
{
	#region 成员字段
	private ConditionCombination _combination;
	#endregion

	#region 构造函数
	public ConditionCollection(ConditionCombination combination)
	{
		_combination = combination;
	}

	internal ConditionCollection(ConditionCombination combination, ICondition condition)
	{
		_combination = combination;

		if(condition != null)
			this.Add(condition);
	}

	internal ConditionCollection(ConditionCombination conditionCombination, ICondition a, ICondition b)
	{
		_combination = conditionCombination;

		if(a != null)
			this.Add(a);
		if(b != null)
			this.Add(b);
	}

	private ConditionCollection(ConditionCombination conditionCombination, IEnumerable<ICondition> items)
	{
		_combination = conditionCombination;

		if(items != null)
		{
			foreach(var item in items)
			{
				if(item != null)
					this.Add(item);
			}
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置查询条件的组合方式。</summary>
	public ConditionCombination Combination
	{
		get => _combination;
		set => _combination = value;
	}
	#endregion

	#region 符号重写
	public static ConditionCollection operator +(ICondition condition, ConditionCollection conditions)
	{
		if(condition == null)
			return conditions;

		if(conditions == null)
			return condition as ConditionCollection;

		if(condition is ConditionCollection collection)
			Combine(collection, conditions);
		else
			conditions.Insert(0, condition);

		return conditions;
	}

	public static ConditionCollection operator +(ConditionCollection conditions, ICondition condition)
	{
		if(condition == null)
			return conditions;

		if(conditions == null)
			return condition as ConditionCollection;

		if(condition is ConditionCollection collection)
			Combine(conditions, collection);
		else
			conditions.Add(condition);

		return conditions;
	}

	public static ConditionCollection operator &(ICondition condition, ConditionCollection conditions) => And(condition, conditions);
	public static ConditionCollection operator &(ConditionCollection conditions, Condition condition) => And(conditions, condition);
	public static ConditionCollection operator &(ConditionCollection left, ConditionCollection right)
	{
		if(left == null)
			return right;

		if(right == null)
			return left;

		return And(left, right);
	}

	public static ConditionCollection operator |(ICondition condition, ConditionCollection conditions) => Or(condition, conditions);
	public static ConditionCollection operator |(ConditionCollection conditions, Condition condition) => Or(conditions, condition);
	public static ConditionCollection operator |(ConditionCollection left, ConditionCollection right)
	{
		if(left == null)
			return right;

		if(right == null)
			return left;

		return Or(left, right);
	}
	#endregion

	#region 静态方法
	public static ConditionCollection And(params ICondition[] items) => And((IEnumerable<ICondition>)items);
	public static ConditionCollection And(IEnumerable<ICondition> items)
	{
		var conditions = new ConditionCollection(ConditionCombination.And);

		if(items != null)
			Combine(conditions, items);

		return conditions;
	}

	public static ConditionCollection Or(params ICondition[] items) => Or((IEnumerable<ICondition>)items);
	public static ConditionCollection Or(IEnumerable<ICondition> items)
	{
		var conditions = new ConditionCollection(ConditionCombination.Or);

		if(items != null)
			Combine(conditions, items);

		return conditions;
	}

	private static void Combine(ConditionCollection owner, IEnumerable<ICondition> items)
	{
		if(owner == null || items == null)
			return;

		var combination = owner.Combination;

		foreach(var item in items)
		{
			if(item == null)
				continue;

			if(item is ConditionCollection conditions)
			{
				if(conditions.Count == 0)
					continue;

				if(conditions.Count == 1)
				{
					owner.Add(conditions[0]);
					continue;
				}

				if(conditions.Combination == combination)
				{
					Combine(owner, conditions);
					continue;
				}

				conditions.Flatten();
			}

			owner.Add(item);
		}
	}
	#endregion

	#region 公共方法
	public bool Contains(string name)
	{
		if(string.IsNullOrEmpty(name))
			return false;

		return Matches(this, name, condition => true) > 0;
	}

	public int Remove(params string[] names)
	{
		if(names == null || names.Length == 0)
			return 0;

		var count = 0;

		foreach(var name in names)
		{
			if(!string.IsNullOrEmpty(name))
				count += Remove(this, name);
		}

		return count;
	}

	public Condition Find(string name)
	{
		if(string.IsNullOrEmpty(name))
			return null;

		Condition found = null;
		Matches(this, name, condition => { found = condition; return true; });
		return found;
	}

	public Condition[] FindAll(string name)
	{
		if(string.IsNullOrEmpty(name))
			return null;

		var conditions = new List<Condition>();
		Matches(this, name, condition => { conditions.Add(condition); return false; });
		return conditions.ToArray();
	}

	public bool Match(string name, Action<Condition> matched = null)
	{
		if(string.IsNullOrEmpty(name))
			return false;

		return Matches(this, name, condition => { matched?.Invoke(condition); return true; }) > 0;
	}

	public int Matches(string name, Action<Condition> matched = null)
	{
		if(string.IsNullOrEmpty(name))
			return 0;

		return Matches(this, name, condition => { matched?.Invoke(condition); return false; });
	}

	public int AddRange(IEnumerable<ICondition> items)
	{
		if(items == null)
			return 0;

		var count = 0;

		foreach(var item in items)
		{
			if(item != null)
			{
				this.Add(item);
				count++;
			}
		}

		return count;
	}

	/// <summary>创建一个与当前集合内容相同的新条件集，并将指定的条件项追加到新集中。</summary>
	/// <param name="items">要追加的条件项数组。</param>
	/// <returns>返回新建的条件集合。</returns>
	public ConditionCollection Append(params ICondition[] items)
	{
		if(items == null || items.Length < 1)
			return this;

		return new ConditionCollection(this.Combination, this.Items.Concat(items.Where(item => item != null)));
	}

	/// <summary>创建一个与当前集合内容相同的新条件集，并将指定的条件项置顶到新集中。</summary>
	/// <param name="items">要置顶的条件项数组。</param>
	/// <returns>返回新建的条件集合。</returns>
	public ConditionCollection Prepend(params ICondition[] items)
	{
		if(items == null || items.Length < 1)
			return this;

		return new ConditionCollection(this.Combination, items.Concat(this.Items.Where(item => item != null)));
	}
	#endregion

	#region 重写方法
	protected override void InsertItem(int index, ICondition item)
	{
		if(item == null)
			return;

		if(item is ConditionCollection collection && collection.Combination == this.Combination)
		{
			foreach(var condition in collection)
				this.Add(condition);

			return;
		}

		base.InsertItem(index, item);
	}

	protected override void SetItem(int index, ICondition item)
	{
		if(item == null)
			return;

		base.SetItem(index, item);
	}

	public bool Equals(ConditionCollection other)
	{
		if(other is null)
			return this.Count == 0;

		if(object.ReferenceEquals(this, other))
			return true;

		if(this.Combination == other.Combination && this.Count == other.Count)
		{
			for(int i = 0; i < this.Count; i++)
			{
				var a = this[i];
				var b = other[i];

				if(a is null)
				{
					if(b is null)
						continue;
					else
						return false;
				}
				else
				{
					if(b is null || a.GetType() != b.GetType())
						return false;
					if(!a.Equals(b))
						return false;
				}
			}

			return true;
		}

		return false;
	}

	public override bool Equals(object obj) => obj is ConditionCollection other && this.Equals(other);
	public override int GetHashCode()
	{
		if(this.Count == 0)
			return this.Combination.GetHashCode();

		var code = 0;
		for(int i = 0; i < this.Count; i++)
			code = HashCode.Combine(code, this[i]);

		return HashCode.Combine(this.Combination, code);
	}

	public override string ToString()
	{
		var combiner = _combination.ToString().ToUpperInvariant();

		if(this.Count < 1)
			return combiner;

		var text = new System.Text.StringBuilder();

		foreach(var item in this.Items)
		{
			if(text.Length > 0)
				text.Append(" " + combiner + " ");

			text.Append(item.ToString());
		}

		return "(" + text.ToString() + ")";
	}
	#endregion

	#region 符号重载
	public static bool operator ==(ConditionCollection a, ConditionCollection b) => object.Equals(a, b);
	public static bool operator !=(ConditionCollection a, ConditionCollection b) => !(a == b);
	#endregion

	#region 私有方法
	private static int Remove(ICollection<ICondition> conditions, string name)
	{
		var count = 0;
		List<Condition> matches = null;

		foreach(var condition in conditions)
		{
			if(condition is Condition c)
			{
				if(string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					count++;

					if(matches == null)
						matches = new List<Condition>();

					matches.Add(c);
				}
			}
			else if(condition is ConditionCollection cs)
			{
				count += Remove(cs, name);
			}
		}

		if(matches != null && matches.Count > 0)
		{
			foreach(var match in matches)
			{
				conditions.Remove(match);
			}
		}

		return count;
	}

	private static int Matches(ICollection<ICondition> conditions, string name, Predicate<Condition> matched)
	{
		int count = 0;

		foreach(var condition in conditions)
		{
			if(condition is Condition c)
			{
				if(string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					count++;

					if(matched != null && matched.Invoke(c))
						return count;
				}
			}
			else if(condition is ConditionCollection cs)
			{
				Matches(cs, name, matched);
			}
		}

		return count;
	}
	#endregion
}
