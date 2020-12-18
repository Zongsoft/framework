/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Flowing
{
	public class StateContext<TKey, TValue> : IStateContext<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : struct
	{
		#region 构造函数
		public StateContext(IStateMachine machine, IStateDiagram<TKey, TValue> diagram, TKey key, TValue source, TValue destination, string description)
		{
			this.Machine = machine ?? throw new ArgumentNullException(nameof(machine));
			this.Diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
			this.Key = key;
			this.State = new StateVector<TValue>(source, destination);
			this.Description = description;
			this.Parameters = new ParameterCollection(machine);
		}

		public StateContext(IStateMachine machine, IStateDiagram<TKey, TValue> diagram, TKey key, StateVector<TValue> state, string description)
		{
			this.Machine = machine ?? throw new ArgumentNullException(nameof(machine));
			this.Diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
			this.Key = key;
			this.State = state;
			this.Description = description;
			this.Parameters = new ParameterCollection(machine);
		}
		#endregion

		#region 公共属性
		public IStateMachine Machine { get; }
		public IStateDiagram<TKey, TValue> Diagram { get; }
		public IDictionary<object, object> Parameters { get; }

		public TKey Key { get; }
		public StateVector<TValue> State { get; }
		public string Description { get; set; }
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return this.Key + ":" + this.State.ToString();
		}
		#endregion

		#region 公共方法
		public bool SetState(string description = null)
		{
			return this.Diagram.SetState(this.Key, this.State.Destination, description ?? this.Description, this.Parameters);
		}
		#endregion

		private class ParameterCollection : IDictionary<object, object>
		{
			#region 成员字段
			private readonly IStateMachine _machine;
			private readonly IDictionary<object, object> _parameters;
			#endregion

			#region 构造函数
			public ParameterCollection(IStateMachine machine)
			{
				_machine = machine;
				_parameters = new Dictionary<object, object>();
			}
			#endregion

			#region 公共属性
			public object this[object key]
			{
				get => _parameters.TryGetValue(key, out var value) ? value : _machine.Parameters[key];
				set => _parameters[key] = value;
			}

			public ICollection<object> Keys => _machine.HasParameters ? _parameters.Keys.Union(_machine.Parameters.Keys).ToArray() : _parameters.Keys;

			public ICollection<object> Values => _machine.HasParameters ? _parameters.Values.Union(_machine.Parameters.Values).ToArray() : _parameters.Values;

			public int Count => _parameters.Count + (_machine.HasParameters ? _machine.Parameters.Count : 0);

			public bool IsReadOnly => false;
			#endregion

			#region 公共方法
			public void Add(object key, object value)
			{
				_parameters.Add(key, value);
			}

			public void Add(KeyValuePair<object, object> item)
			{
				_parameters.Add(item);
			}

			public void Clear()
			{
				_parameters.Clear();
			}

			public bool Contains(KeyValuePair<object, object> item)
			{
				return _parameters.Contains(item) || (_machine.HasParameters && _machine.Parameters.Contains(item));
			}

			public bool ContainsKey(object key)
			{
				return _parameters.ContainsKey(key) || (_machine.HasParameters && _machine.Parameters.ContainsKey(key));
			}

			public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
			{
				_parameters.CopyTo(array, arrayIndex);

				if(array.Length - arrayIndex > _parameters.Count && _machine.HasParameters)
					_machine.Parameters.CopyTo(array, arrayIndex + _parameters.Count);
			}

			public bool Remove(object key)
			{
				return _parameters.Remove(key) || (_machine.HasParameters && _machine.Parameters.Remove(key));
			}

			public bool Remove(KeyValuePair<object, object> item)
			{
				return _parameters.Remove(item.Key) || (_machine.HasParameters && _machine.Parameters.Remove(item.Key));
			}

			public bool TryGetValue(object key, out object value)
			{
				return _parameters.TryGetValue(key, out value) || (_machine.HasParameters && _machine.Parameters.TryGetValue(key, out value));
			}

			public bool TryGetValue<T>(out T value)
			{
				if(this.TryGetValue(typeof(T), out var parameter) && parameter is T result)
				{
					value = result;
					return true;
				}

				value = default;
				return false;
			}
			#endregion

			#region 遍历迭代
			public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
			{
				foreach(var entry in _parameters)
					yield return entry;

				if(_machine.HasParameters)
				{
					foreach(var entry in _machine.Parameters)
						yield return entry;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			#endregion
		}
	}
}
