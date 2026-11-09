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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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

	#region 重写属性
	public override int Count => _parameters.Count;
	public override object SyncRoot => ((ICollection)_parameters).SyncRoot;
	#endregion

	#region 重写方法
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
	#endregion

	private sealed class Parameters : KeyedCollection<string, InfluxParameter>
	{
		#region 重写方法
		protected override string GetKeyForItem(InfluxParameter parameter) => parameter.ParameterName;
		#endregion
	}
}
