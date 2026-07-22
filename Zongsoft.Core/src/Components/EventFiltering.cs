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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components;

public class EventFiltering : IPredication<EventContext>, IList<EventFiltering.Entry>
{
	#region 成员字段
	private readonly List<EventFiltering.Entry> _entries = [];
	#endregion

	#region 公共属性
	public int Count => _entries.Count;
	bool ICollection<Entry>.IsReadOnly => false;
	public Entry this[int index] { get => _entries[index]; set => _entries[index] = value; }
	#endregion

	#region 断言方法
	ValueTask<bool> IPredication.PredicateAsync(object argument, CancellationToken cancellation) => this.PredicateAsync(argument as EventContext, cancellation);
	ValueTask<bool> IPredication.PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation) => this.PredicateAsync(argument as EventContext, parameters, cancellation);
	public virtual ValueTask<bool> PredicateAsync(EventContext argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);
	public ValueTask<bool> PredicateAsync(EventContext context, Parameters parameters, CancellationToken cancellation = default)
	{
		if(_entries == null || _entries.Count == 0)
			return ValueTask.FromResult(true);

		foreach(var entry in _entries)
		{
			if(IsMatch(entry.RegistryName, context.Registry.Name) &&
			   IsMatch(entry.EventName, context.Name))
				return ValueTask.FromResult(entry.Kind == EntryKind.Inclusive);
		}

		return ValueTask.FromResult(true);

		static bool IsMatch(string pattern, string name)
		{
			if(string.IsNullOrWhiteSpace(pattern) || pattern == Entry.ALL)
				return true;

			return string.Equals(pattern, name, StringComparison.OrdinalIgnoreCase);
		}
	}
	#endregion

	#region 集合方法
	public void Clear() => _entries.Clear();
	public void Add(Entry entry) => _entries.Add(entry);
	public bool Remove(Entry entry) => _entries.Remove(entry);
	public void RemoveAt(int index) => _entries.RemoveAt(index);
	public bool Contains(Entry entry) => _entries.Contains(entry);
	public int IndexOf(Entry entry) => _entries.IndexOf(entry);
	public void Insert(int index, Entry entry) => _entries.Insert(index, entry);
	public void CopyTo(Entry[] array, int arrayIndex) => _entries.CopyTo(array, arrayIndex);
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<Entry> GetEnumerator() => _entries.GetEnumerator();
	#endregion

	#region 嵌套结构
	public enum EntryKind
	{
		Inclusive,
		Exclusive,
	}

	[TypeConverter(typeof(EntryConverter))]
	public readonly struct Entry : IEquatable<Entry>, IParsable<Entry>
	{
		internal const string ALL = "*";

		public readonly EntryKind Kind;
		public readonly string RegistryName;
		public readonly string EventName;

		public Entry(EntryKind kind, string registry, string name)
		{
			this.Kind = kind;
			this.RegistryName = Normalize(registry);
			this.EventName = Normalize(name);
			static string Normalize(string name) => string.IsNullOrEmpty(name) || name == ALL ? null : name;
		}

		public bool Equals(Entry other) => this.Kind == other.Kind &&
			string.Equals(this.RegistryName, other.RegistryName, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(this.EventName, other.EventName, StringComparison.OrdinalIgnoreCase);

		public override bool Equals(object obj) => obj is Entry other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Kind, this.RegistryName, this.EventName);
		public override string ToString()
		{
			if(IsEmpty(this.RegistryName) && IsEmpty(this.EventName))
				return this.Kind == EntryKind.Exclusive ? "!" : ALL;

			var registry = string.IsNullOrEmpty(this.RegistryName) ? ALL : this.RegistryName;
			var eventName = string.IsNullOrEmpty(this.EventName) ? ALL : this.EventName;

			return this.Kind switch
			{
				EntryKind.Inclusive => $"{registry}.{eventName}",
				EntryKind.Exclusive => $"!{registry}.{eventName}",
				_ => $"{registry}.{eventName}",
			};

			static bool IsEmpty(string name) => string.IsNullOrEmpty(name) || name == ALL;
		}

		public static implicit operator string(Entry entry) => entry.ToString();
		public static bool operator ==(Entry left, Entry right) => left.Equals(right);
		public static bool operator !=(Entry left, Entry right) => !(left == right);

		public static Entry Parse(string text, IFormatProvider provider = null) => TryParse(text, provider, out var result) ? result : throw new InvalidOperationException();
		public static bool TryParse(string text, out Entry result) => TryParse(text, null, out result);
		public static bool TryParse(string text, IFormatProvider provider, out Entry result)
		{
			if(string.IsNullOrEmpty(text))
			{
				result = default;
				return false;
			}

			if(text == ALL)
			{
				result = default;
				return true;
			}

			var kind = text[0] == '!' ? EntryKind.Exclusive : EntryKind.Inclusive;

			if(kind == EntryKind.Exclusive)
				text = text[1..].Trim();

			var index = text.LastIndexOf('.');
			result = index < 0 ?
				new(kind, text, null) :
				new(kind, text[..index], text[(index + 1)..]);

			return true;
		}
	}

	private sealed class EntryConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text)
				return Entry.Parse(text);

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is Entry entry && destinationType == typeof(string))
				return entry.ToString();

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
	#endregion
}
