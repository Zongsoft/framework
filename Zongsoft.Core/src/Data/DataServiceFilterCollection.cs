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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data
{
	public class DataServiceFilterCollection<TModel> : ICollection<IDataServiceFilter<TModel>>
	{
		#region 成员字段
		private int _count;
		private readonly IDataService<TModel> _service;
		private readonly Dictionary<string, HashSet<IDataServiceFilter<TModel>>> _filters;
		#endregion

		#region 构造函数
		public DataServiceFilterCollection(IDataService<TModel> service)
		{
			_count = 0;
			_service = service ?? throw new ArgumentNullException(nameof(service));
			_filters = new Dictionary<string, HashSet<IDataServiceFilter<TModel>>>();
		}
		#endregion

		#region 公共属性
		public int Count => _count;
		public bool IsReadOnly => false;
		#endregion

		#region 公共方法
		public void Clear()
		{
			_filters.Clear();
			_count = 0;
		}

		void ICollection<IDataServiceFilter<TModel>>.Add(IDataServiceFilter<TModel> filter) => _count += this.Add(filter) ? 1 : 0;
		private bool Add(IDataServiceFilter<TModel> filter)
		{
			if(filter == null)
				return false;

			var attribute = filter.GetType().GetCustomAttribute<DataServiceFilterAttribute>();
			var names = attribute?.Names;

			if(names == null || names.Length == 0)
			{
				if(_filters.TryAdd(string.Empty, new HashSet<IDataServiceFilter<TModel>>(new[] { filter })))
					return true;

				var filters = _filters[string.Empty];

				if(filters != null)
					return filters.Add(filter);
			}
			else
			{
				var added = false;

				foreach(var name in names)
				{
					if(string.IsNullOrWhiteSpace(name))
						continue;

					if(_filters.TryAdd(name, new HashSet<IDataServiceFilter<TModel>>(new[] { filter })))
						return true;

					var filters = _filters[string.Empty];

					if(filters != null)
						added |= filters.Add(filter);
				}

				return added;
			}

			return false;
		}

		public bool Remove(IDataServiceFilter<TModel> filter)
		{
			if(filter == null)
				return false;

			var attribute = filter.GetType().GetCustomAttribute<DataServiceFilterAttribute>();
			var names = attribute?.Names;

			if(names == null || names.Length == 0)
			{
				var succeed = false;

				if(_filters.TryGetValue(string.Empty, out var filters) && filters != null)
					succeed |= filters.Remove(filter);

				if(succeed)
					_count--;

				return succeed;
			}

			foreach(var name in names)
			{
				var succeed = false;

				if(_filters.TryGetValue(name, out var matches) && matches != null)
					succeed |= matches.Remove(filter);

				if(succeed)
					_count--;

				return succeed;
			}

			return false;
		}

		public bool Contains(IDataServiceFilter<TModel> filter)
		{
			if(filter == null)
				return false;

			if(_filters.TryGetValue(string.Empty, out var filters) && filters.Contains(filter))
				return true;

			foreach(var entry in _filters)
			{
				if(entry.Value.Contains(filter))
					return true;
			}

			return false;
		}

		public void CopyTo(IDataServiceFilter<TModel>[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException(nameof(array));
			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			if(_filters == null || _filters.Count == 0)
				return;

			var index = 0;
			var hashset = new HashSet<IDataServiceFilter<TModel>>();

			foreach(var entry in _filters)
			{
				foreach(var filter in entry.Value)
				{
					if(hashset.Add(filter))
						array[arrayIndex + index++] = filter;
				}
			}
		}
		#endregion

		#region 内部方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private DataServiceContext<TModel> CreateContext(DataServiceMethod method, IDataAccessContextBase context, object result, params object[] arguments) => new DataServiceContext<TModel>(_service, method, context, result, arguments);

		internal void OnFiltered(DataServiceMethod method, IDataAccessContextBase accessContext, object result, params object[] arguments)
		{
			DataServiceContext<TModel> context = null;

			if(_filters.TryGetValue(method.Name, out var filters) && filters != null && filters.Count > 0)
			{
				context ??= this.CreateContext(method, accessContext, result, arguments);

				foreach(var filter in filters)
					filter.OnFiltered(context);
			}

			if(_filters.TryGetValue(string.Empty, out filters) && filters != null && filters.Count > 0)
			{
				context ??= this.CreateContext(method, accessContext, result, arguments);

				foreach(var filter in filters)
					filter.OnFiltered(context);
			}
		}

		internal bool OnFiltering(DataServiceMethod method, IDataAccessContextBase accessContext, params object[] arguments)
		{
			DataServiceContext<TModel> context = null;

			if(_filters.TryGetValue(method.Name, out var filters) && filters != null && filters.Count > 0)
			{
				context ??= this.CreateContext(method, accessContext, null, arguments);

				foreach(var filter in filters)
				{
					if(filter.OnFiltering(context))
						return true;
				}
			}

			if(_filters.TryGetValue(string.Empty, out filters) && filters != null && filters.Count > 0)
			{
				context ??= this.CreateContext(method, accessContext, null, arguments);

				foreach(var filter in filters)
				{
					if(filter.OnFiltering(context))
						return true;
				}
			}

			return false;
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<IDataServiceFilter<TModel>> GetEnumerator()
		{
			var hashset = new HashSet<IDataServiceFilter<TModel>>();

			foreach(var entry in _filters)
			{
				foreach(var filter in entry.Value)
				{
					if(filter != null && hashset.Add(filter))
						yield return filter;
				}
			}
		}
		#endregion
	}
}
