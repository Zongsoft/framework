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
 * This file is part of Zongsoft.Data.Influx library.
 *
 * The Zongsoft.Data.Influx is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.Influx is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.Influx library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using InfluxDB3.Client;
using InfluxDB3.Client.Write;

using Zongsoft.Collections;

namespace Zongsoft.Data.Influx.Common;

public class InfluxDataReader(IAsyncEnumerable<PointDataValues> data) : DbDataReader
{
	#region 成员字段
	private Entry[] _entries;
	private IAsyncEnumerable<PointDataValues> _data = data;
	private IAsyncEnumerator<PointDataValues> _iterator = data.GetAsyncEnumerator();
	#endregion

	#region 公共属性
	public override object this[int ordinal] => this.GetValue(ordinal);
	public override object this[string name] => this.GetValue(name);

	public override int Depth => 0;
	public override int FieldCount => _entries == null ? 0 : _entries.Length;
	public override bool HasRows => true;
	public override bool IsClosed => false;
	public override int RecordsAffected => -1;
	#endregion

	#region 取值方法
	public object GetValue(string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		return _iterator.Current.GetTag(name) ?? _iterator.Current.GetField(name);
	}

	public override bool GetBoolean(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			Zongsoft.Common.Convert.ConvertValue<bool>(_iterator.Current.GetTag(entry.Name)) :
			_iterator.Current.GetBooleanField(entry.Name) ?? throw new InvalidOperationException();
	}

	public override DateTime GetDateTime(int ordinal) => Zongsoft.Common.Convert.ConvertValue<DateTime>(this.GetString(ordinal));

	public override decimal GetDecimal(int ordinal) => (decimal)this.GetDouble(ordinal);
	public override float GetFloat(int ordinal) => (float)this.GetDouble(ordinal);
	public override double GetDouble(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			Zongsoft.Common.Convert.ConvertValue<double>(_iterator.Current.GetTag(entry.Name)) :
			_iterator.Current.GetDoubleField(entry.Name) ?? throw new InvalidOperationException();
	}

	public override byte GetByte(int ordinal) => (byte)this.GetInt64(ordinal);
	public override short GetInt16(int ordinal) => (short)this.GetInt64(ordinal);
	public override int GetInt32(int ordinal) => (int)this.GetInt64(ordinal);
	public override long GetInt64(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			Zongsoft.Common.Convert.ConvertValue<int>(_iterator.Current.GetTag(entry.Name)) :
			_iterator.Current.GetIntegerField(entry.Name) ?? throw new InvalidOperationException();
	}

	public override char GetChar(int ordinal)
	{
		var text = this.GetString(ordinal);
		return string.IsNullOrEmpty(text) ? '\0' : text[0];
	}

	public override string GetString(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			_iterator.Current.GetTag(entry.Name) :
			_iterator.Current.GetStringField(entry.Name) ?? throw new InvalidOperationException();
	}

	public override Guid GetGuid(int ordinal) => Zongsoft.Common.Convert.ConvertValue<Guid>(this.GetValue(ordinal));
	public override object GetValue(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			_iterator.Current.GetTag(entry.Name) :
			_iterator.Current.GetField(entry.Name) ?? throw new InvalidOperationException();
	}
	#endregion

	#region 其他方法
	public override string GetName(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		return _entries[ordinal].Name;
	}

	public override int GetOrdinal(string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(_entries == null)
			throw new InvalidOperationException();

		for(int i = 0; i < _entries.Length; i++)
		{
			if(_entries[i].Name == name)
				return i;
		}

		return -1;
	}

	public override bool IsDBNull(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			_iterator.Current.GetTag(entry.Name) == null :
			_iterator.Current.GetField(entry.Name) == null;
	}

	public override string GetDataTypeName(int ordinal) => this.GetFieldType(ordinal)?.Name;
	public override Type GetFieldType(int ordinal)
	{
		if(ordinal < 0 || ordinal >= _entries.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal));

		if(_entries == null)
			throw new InvalidOperationException();

		var entry = _entries[ordinal];

		return entry.IsTag ?
			typeof(string) :
			_iterator.Current.GetFieldType(entry.Name);
	}
	#endregion

	#region 不支持的
	public override int GetValues(object[] values) => throw new NotSupportedException();
	public override long GetBytes(int ordinal, long offset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
	public override long GetChars(int ordinal, long offset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
	public override bool NextResult() => throw new NotSupportedException();
	#endregion

	#region 枚举遍历
	public override IEnumerator GetEnumerator() => _data.Synchronize().GetEnumerator();
	#endregion

	#region 读取方法
	public override bool Read()
	{
		if(_iterator.MoveNextAsync().AsTask().Wait(1000))
		{
			if(_entries == null)
				this.Fill();

			return true;
		}

		return false;
	}

	public override async Task<bool> ReadAsync(CancellationToken cancellation)
	{
		if(await _iterator.MoveNextAsync())
		{
			if(_entries == null)
				this.Fill();

			return true;
		}

		return false;
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		Dispose(_iterator);

		_data = null;
		_entries = null;

		async void Dispose(IAsyncEnumerator<PointDataValues> iterator)
		{
			await iterator.DisposeAsync();
		}
	}

	public override ValueTask DisposeAsync()
	{
		_data = null;
		_entries = null;
		return _iterator?.DisposeAsync() ?? default;
	}
	#endregion

	#region 私有方法
	private void Fill()
	{
		if(_entries == null)
			return;

		var tags = _iterator.Current.GetTagNames();
		var fields = _iterator.Current.GetFieldNames();
		_entries ??= new Entry[tags.Length + fields.Length];

		for(int i = 0; i < tags.Length; i++)
			_entries[i] = new(tags[i], EntryKind.Tag);
		for(int i = 0; i < fields.Length; i++)
			_entries[tags.Length + i] = new(fields[i], EntryKind.Field);
	}
	#endregion

	#region 嵌套结构
	private readonly struct Entry(string name, EntryKind kind) : IEquatable<Entry>
	{
		public readonly string Name = name;
		public readonly EntryKind Kind = kind;

		public bool IsTag => this.Kind == EntryKind.Tag;
		public bool IsField => this.Kind == EntryKind.Field;

		public bool Equals(Entry other) => string.Equals(this.Name, other.Name) && this.Kind == other.Kind;
		public override bool Equals(object obj) => obj is Entry other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Name, this.Kind);
		public override string ToString() => $"[{this.Kind}]{this.Name}";
	}

	private enum EntryKind
	{
		Tag,
		Field,
	}
	#endregion
}
