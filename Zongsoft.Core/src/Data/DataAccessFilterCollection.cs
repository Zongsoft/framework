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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data
{
	public class DataAccessFilterCollection : ICollection<object>
	{
		#region 私有变量
		private readonly FilterDescriptor _global;
		private readonly HashSet<object> _items;
		private readonly ConcurrentDictionary<string, FilterDescriptor> _filters;
		#endregion

		#region 构造函数
		public DataAccessFilterCollection()
		{
			_global = new FilterDescriptor();
			_items = new HashSet<object>();
			_filters = new ConcurrentDictionary<string, FilterDescriptor>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count { get => _items.Count; }
		public bool IsReadOnly { get => false; }
		#endregion

		#region 公共方法
		public void InvokeFiltering(IDataAccessContextBase context)
		{
			_global.OnFiltering(context);

			if(_filters != null && _filters.TryGetValue(context.Name, out var descriptor))
				descriptor.OnFiltering(context);
		}

		public void InvokeFiltered(IDataAccessContextBase context)
		{
			_global.OnFiltered(context);

			if(_filters != null && _filters.TryGetValue(context.Name, out var descriptor))
				descriptor.OnFiltered(context);
		}

		public bool Add(object instance)
		{
			if(instance == null)
				return false;

			if(_items.Contains(instance))
				return false;

			var attribute = instance.GetType().GetCustomAttribute<DataAccessFilterAttribute>();

			if(attribute == null || attribute.Names == null || attribute.Names.Length == 0)
				return _global.Add(instance) && _items.Add(instance);

			var count = 0;

			for(int i = 0; i < attribute.Names.Length; i++)
			{
				var name = attribute.Names[i];

				if(string.IsNullOrWhiteSpace(name))
					continue;

				var descriptor = _filters.GetOrAdd(name, _ => new FilterDescriptor());
				count += descriptor.Add(instance) ? 1 : 0;
			}

			return count > 0 && _items.Add(instance);
		}

		public void Clear()
		{
			_items.Clear();
			_global.Clear();
			_filters.Clear();
		}

		public bool Remove(object instance)
		{
			if(instance == null)
				return false;

			if(!_items.Remove(instance))
				return false;

			var attribute = instance.GetType().GetCustomAttribute<DataAccessFilterAttribute>(true);

			if(attribute == null || attribute.Names == null || attribute.Names.Length == 0)
				return _global.Remove(instance);

			var count = 0;

			for(int i = 0; i < attribute.Names.Length; i++)
			{
				var name = attribute.Names[i];

				if(string.IsNullOrWhiteSpace(name))
					continue;

				if(_filters.TryGetValue(name, out var descriptor))
					count += descriptor.Remove(instance) ? 1 : 0;
			}

			return count > 0;
		}

		public bool Contains(object instance) => instance != null && _items.Contains(instance);
		void ICollection<object>.Add(object item) => this.Add(item);
		void ICollection<object>.CopyTo(object[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
		#endregion

		#region 遍历枚举
		public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region 嵌套子类
		private class FilterDescriptor
		{
			#region 成员字段
			private ICollection<IDataAccessFilter<DataExistContextBase>> _exists;
			private ICollection<IDataAccessFilter<DataSelectContextBase>> _selects;
			private ICollection<IDataAccessFilter<DataDeleteContextBase>> _deletes;
			private ICollection<IDataAccessFilter<DataInsertContextBase>> _inserts;
			private ICollection<IDataAccessFilter<DataUpdateContextBase>> _updates;
			private ICollection<IDataAccessFilter<DataUpsertContextBase>> _upserts;
			private ICollection<IDataAccessFilter<DataExecuteContextBase>> _executes;
			private ICollection<IDataAccessFilter<DataAggregateContextBase>> _aggregates;
			#endregion

			#region 构造函数
			public FilterDescriptor() { }
			public FilterDescriptor(object filter) => this.Add(filter);
			#endregion

			#region 公共方法
			public bool Add(object instance)
			{
				if(instance == null)
					return false;

				var count = 0;
				var contracts = instance.GetType().GetInterfaces();

				foreach(var contract in contracts)
				{
					if(contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDataAccessFilter<>))
					{
						var type = contract.GetGenericArguments()[0];

						if(type == typeof(DataExistContextBase))
						{
							EnsureFilters(ref _exists);
							_exists.Add((IDataAccessFilter<DataExistContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataSelectContextBase))
						{
							EnsureFilters(ref _selects);
							_selects.Add((IDataAccessFilter<DataSelectContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataDeleteContextBase))
						{
							EnsureFilters(ref _deletes);
							_deletes.Add((IDataAccessFilter<DataDeleteContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataInsertContextBase))
						{
							EnsureFilters(ref _inserts);
							_inserts.Add((IDataAccessFilter<DataInsertContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataUpsertContextBase))
						{
							EnsureFilters(ref _upserts);
							_upserts.Add((IDataAccessFilter<DataUpsertContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataUpdateContextBase))
						{
							EnsureFilters(ref _updates);
							_updates.Add((IDataAccessFilter<DataUpdateContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataExecuteContextBase))
						{
							EnsureFilters(ref _executes);
							_executes.Add((IDataAccessFilter<DataExecuteContextBase>)instance);
							count++;
						}
						else if(type == typeof(DataAggregateContextBase))
						{
							EnsureFilters(ref _aggregates);
							_aggregates.Add((IDataAccessFilter<DataAggregateContextBase>)instance);
							count++;
						}
					}
				}

				return count > 0;
			}

			public bool Remove(object instance)
			{
				if(instance == null)
					return false;

				var count = 0;
				var contracts = instance.GetType().GetInterfaces();

				for(int i = 0; i < contracts.Length; i++)
				{
					var contract = contracts[i];

					if(contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDataAccessFilter<>))
					{
						var type = contract.GetGenericArguments()[0];

						if(type == typeof(DataExistContextBase))
							count += (_exists?.Remove((IDataAccessFilter<DataExistContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataSelectContextBase))
							count += (_selects?.Remove((IDataAccessFilter<DataSelectContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataDeleteContextBase))
							count += (_deletes?.Remove((IDataAccessFilter<DataDeleteContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataInsertContextBase))
							count += (_inserts?.Remove((IDataAccessFilter<DataInsertContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataUpsertContextBase))
							count += (_upserts?.Remove((IDataAccessFilter<DataUpsertContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataUpdateContextBase))
							count += (_updates?.Remove((IDataAccessFilter<DataUpdateContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataExecuteContextBase))
							count += (_executes?.Remove((IDataAccessFilter<DataExecuteContextBase>)instance) ?? false) ? 1 : 0;
						else if(type == typeof(DataAggregateContextBase))
							count += (_aggregates?.Remove((IDataAccessFilter<DataAggregateContextBase>)instance) ?? false) ? 1 : 0;
					}
				}

				return count > 0;
			}

			public void Clear()
			{
				_exists?.Clear();
				_selects?.Clear();
				_deletes?.Clear();
				_inserts?.Clear();
				_updates?.Clear();
				_upserts?.Clear();
				_executes?.Clear();
				_aggregates?.Clear();
			}

			public void OnFiltering(IDataAccessContextBase context)
			{
				switch(context)
				{
					case DataExistContextBase existing:
						var exists = _exists;
						if(exists != null)
						{
							foreach(var filter in exists)
								filter.OnFiltering(existing);
						}

						break;
					case DataSelectContextBase selection:
						var selects = _selects;
						if(selects != null)
						{
							foreach(var filter in selects)
								filter.OnFiltering(selection);
						}

						break;
					case DataDeleteContextBase deletion:
						var deletes = _deletes;
						if(deletes != null)
						{
							foreach(var filter in deletes)
								filter.OnFiltering(deletion);
						}

						break;
					case DataInsertContextBase insertion:
						var inserts = _inserts;
						if(inserts != null)
						{
							foreach(var filter in inserts)
								filter.OnFiltering(insertion);
						}

						break;
					case DataUpdateContextBase updation:
						var updates = _updates;
						if(updates != null)
						{
							foreach(var filter in updates)
								filter.OnFiltering(updation);
						}

						break;
					case DataUpsertContextBase upsertion:
						var upserts = _upserts;
						if(upserts != null)
						{
							foreach(var filter in upserts)
								filter.OnFiltering(upsertion);
						}

						break;
					case DataExecuteContextBase execution:
						var executes = _executes;
						if(executes != null)
						{
							foreach(var filter in executes)
								filter.OnFiltering(execution);
						}

						break;
					case DataAggregateContextBase aggregation:
						var aggregates = _aggregates;
						if(aggregates != null)
						{
							foreach(var filter in aggregates)
								filter.OnFiltering(aggregation);
						}

						break;
				}
			}

			public void OnFiltered(IDataAccessContextBase context)
			{
				switch(context)
				{
					case DataExistContextBase existing:
						var exists = _exists;
						if(exists != null)
						{
							foreach(var filter in exists)
								filter.OnFiltered(existing);
						}

						break;
					case DataSelectContextBase selection:
						var selects = _selects;
						if(selects != null)
						{
							foreach(var filter in selects)
								filter.OnFiltered(selection);
						}

						break;
					case DataDeleteContextBase deletion:
						var deletes = _deletes;
						if(deletes != null)
						{
							foreach(var filter in deletes)
								filter.OnFiltered(deletion);
						}

						break;
					case DataInsertContextBase insertion:
						var inserts = _inserts;
						if(inserts != null)
						{
							foreach(var filter in inserts)
								filter.OnFiltered(insertion);
						}

						break;
					case DataUpdateContextBase updation:
						var updates = _updates;
						if(updates != null)
						{
							foreach(var filter in updates)
								filter.OnFiltered(updation);
						}

						break;
					case DataUpsertContextBase upsertion:
						var upserts = _upserts;
						if(upserts != null)
						{
							foreach(var filter in upserts)
								filter.OnFiltered(upsertion);
						}

						break;
					case DataExecuteContextBase execution:
						var executes = _executes;
						if(executes != null)
						{
							foreach(var filter in executes)
								filter.OnFiltered(execution);
						}

						break;
					case DataAggregateContextBase aggregation:
						var aggregates = _aggregates;
						if(aggregates != null)
						{
							foreach(var filter in aggregates)
								filter.OnFiltered(aggregation);
						}

						break;
				}
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private ICollection<IDataAccessFilter<TContext>> EnsureFilters<TContext>(ref ICollection<IDataAccessFilter<TContext>> filters) where TContext : IDataAccessContextBase
			{
				if(filters == null)
				{
					lock(this)
					{
						if(filters == null)
							filters = new List<IDataAccessFilter<TContext>>();
					}
				}

				return filters;
			}
			#endregion
		}
		#endregion
	}
}
