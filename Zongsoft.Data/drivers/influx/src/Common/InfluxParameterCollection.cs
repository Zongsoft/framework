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
using System.Collections;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Influx.Common;

public class InfluxParameterCollection : DbParameterCollection
{
	private readonly Parameters _parameters = new();

	public override int Count => _parameters.Count;
	public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

	public override int Add(object value)
	{
		if(value is InfluxParameter parameter)
		{
			_parameters.Add(parameter);
			return _parameters.Count - 1;
		}

		return -1;
	}

	public override void AddRange(Array values)
	{
		if(values == null || values.Length == 0)
			return;

		for(int i = 0; i < values.Length; i++)
		{
			if(values.GetValue(i) is InfluxParameter parameter)
				_parameters.Add(parameter);
		}
	}

	public override void Clear() => _parameters.Clear();
	public override bool Contains(object value) => value is InfluxParameter parameter && _parameters.Contains(parameter);
	public override bool Contains(string name) => name is not null && _parameters.Contains(name);
	public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
	public override int IndexOf(object value) => value is InfluxParameter parameter ? _parameters.IndexOf(parameter) : -1;
	public override int IndexOf(string name) => name != null && _parameters.TryGetValue(name, out var parameter) ? _parameters.IndexOf(parameter) : -1;
	public override void Insert(int index, object value) => _parameters.Insert(index, value as InfluxParameter ?? throw new ArgumentException());
	public override void Remove(object value) => _parameters.Remove(value as InfluxParameter);
	public override void RemoveAt(int index) => _parameters.RemoveAt(index);
	public override void RemoveAt(string name) => _parameters.Remove(name);
	protected override DbParameter GetParameter(int index) => _parameters[index];
	protected override DbParameter GetParameter(string name) => _parameters[name];
	protected override void SetParameter(int index, DbParameter value) => _parameters.Insert(index, value as InfluxParameter);
	protected override void SetParameter(string name, DbParameter value) => _parameters.Add(value as InfluxParameter);
	public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

	private sealed class Parameters : KeyedCollection<string, InfluxParameter>
	{
		protected override string GetKeyForItem(InfluxParameter parameter) => parameter.ParameterName;
	}
}
