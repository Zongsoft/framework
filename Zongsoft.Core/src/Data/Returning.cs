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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 表示写操作的返回数据的类。
/// </summary>
public class Returning
{
	#region 构造函数
	internal Returning(ReturningKind kind, params string[] columns)
	{
		this.Columns = new(kind, columns
			.Where(column => !string.IsNullOrEmpty(column))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(name => new Column(name, kind)));
		this.Rows = new(this.Columns);
	}

	internal Returning(ReturningKind kind, params IEnumerable<string> columns)
	{
		this.Columns = new(kind, columns
			.Where(column => !string.IsNullOrEmpty(column))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(name => new Column(name, kind)));
		this.Rows = new(this.Columns);
	}

	internal Returning(params Column[] columns)
	{
		this.Columns = new(null, columns);
		this.Rows = new(this.Columns);
	}

	internal Returning(params IEnumerable<Column> columns)
	{
		this.Columns = new(null, columns);
		this.Rows= new(this.Columns);
	}
	#endregion

	#region 公共属性
	/// <summary>获取返回数据的列信息。</summary>
	public ColumnCollection Columns { get; }
	/// <summary>获取返回数据的内容行。</summary>
	public RowCollection Rows { get; }
	#endregion

	#region 嵌套结构
	public readonly struct Column(string name, ReturningKind kind) : IEquatable<Column>
	{
		public readonly string Name = name;
		public readonly ReturningKind Kind = kind;

		public bool Equals(Column other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && this.Kind == other.Kind;
		public override bool Equals(object obj) => obj is Column other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant(), this.Kind);
		public override string ToString() => $"{this.Name}({this.Kind})";

		public static bool operator ==(Column left, Column right) => left.Equals(right);
		public static bool operator !=(Column left, Column right) => !(left == right);
	}

	public sealed class ColumnCollection : ICollection<Column>
	{
		private readonly ReturningKind? _kind;
		private Column[] _columns;

		public ColumnCollection(ReturningKind? kind, params Column[] columns)
		{
			_kind = kind;
			_columns = columns ?? [];
		}
		public ColumnCollection(ReturningKind? kind, params IEnumerable<Column> columns)
		{
			_kind = kind;
			_columns = [.. columns];
		}

		public int Count => _columns.Length;
		public bool IsEmpty => _columns == null || _columns.Length == 0;
		bool ICollection<Column>.IsReadOnly => false;

		public int GetOrdinal(Column column) => this.GetOrdinal(column.Name, column.Kind);
		public int GetOrdinal(string name, ReturningKind? kind = default)
		{
			if(string.IsNullOrEmpty(name))
				return -1;

			if(kind.HasValue && _kind.HasValue && kind.Value != _kind.Value)
				throw new ArgumentOutOfRangeException(nameof(kind));

			kind ??= _kind ?? ReturningKind.Older;
			return Array.IndexOf(_columns, new Column(name, kind.Value));
		}

		public bool Append(string name, ReturningKind? kind = default)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(kind.HasValue && _kind.HasValue && kind.Value != _kind.Value)
				throw new ArgumentOutOfRangeException(nameof(kind));

			kind ??= _kind ?? ReturningKind.Older;
			if(_columns != null && Array.Exists(_columns, column => column == new Column(name, kind.Value)))
				return false;

			_columns = [.. _columns, new Column(name, kind.Value)];
			return true;
		}

		public bool Prepend(string name, ReturningKind? kind = default)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(kind.HasValue && _kind.HasValue && kind.Value != _kind.Value)
				throw new ArgumentOutOfRangeException(nameof(kind));

			kind ??= _kind ?? ReturningKind.Older;
			if(_columns != null && Array.Exists(_columns, column => column == new Column(name, kind.Value)))
				return false;

			_columns = [new Column(name, kind.Value), .. _columns];
			return true;
		}

		public bool Contains(string name, ReturningKind? kind = default)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(kind.HasValue && _kind.HasValue && kind.Value != _kind.Value)
				return false;

			kind ??= _kind ?? ReturningKind.Older;
			return _columns != null && Array.Exists(_columns, column => column == new Column(name, kind.Value));
		}

		public void Add(Column column) => this.Append(column.Name, column.Kind);
		public void Clear() => Array.Clear(_columns);
		public bool Contains(Column column) => Array.Exists(_columns, element => element == column);
		public void CopyTo(Column[] array, int arrayIndex) => _columns.CopyTo(array, arrayIndex);
		public bool Remove(Column column)
		{
			var index = Array.IndexOf(_columns, column);
			if(index < 0)
				return false;

			_columns = [.. _columns.AsSpan(0, index), .. _columns.AsSpan(index + 1)];
			return true;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Column> GetEnumerator()
		{
			for(int i = 0; i < _columns.Length; i++)
				yield return _columns[i];
		}
	}

	public readonly struct Row(ColumnCollection columns, params object[] values)
	{
		private readonly ColumnCollection _columns = columns;
		public readonly object[] Values = values;
		public object this[int index] => Values[index];

		public bool TryGetValue(string name, out object value) => this.TryGetValue(name, null, out value);
		public bool TryGetValue(string name, ReturningKind? kind, out object value)
		{
			if(string.IsNullOrEmpty(name))
			{
				value = null;
				return false;
			}

			var ordinal = _columns.GetOrdinal(name, kind);
			if(ordinal >= 0 && ordinal < this.Values.Length)
			{
				value = this.Values[ordinal];
				return true;
			}

			value = null;
			return false;
		}
	}

	public sealed class RowCollection(ColumnCollection columns) : IReadOnlyList<Row>
	{
		private readonly List<Row> _rows = [];
		private readonly ColumnCollection _columns = columns;

		public int Count => _rows.Count;
		public bool IsEmpty => _rows.Count == 0;
		public Row this[int index] => _rows[index];

		public void Populate(IDataRecord record)
		{
			if(record == null)
				throw new ArgumentNullException(nameof(record));

			var values = new object[_columns.Count];
			var row = new Row(_columns, record.GetValues(values));
			_rows.Add(row);
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Row> GetEnumerator() => _rows.GetEnumerator();
	}
	#endregion
}
