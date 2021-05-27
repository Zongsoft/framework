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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data
{
	[Service(typeof(IServiceProvider<IDataAccess>))]
	public abstract class DataAccessProviderBase<TDataAccess> : IDataAccessProvider, IServiceProvider<TDataAccess>, ICollection<TDataAccess> where TDataAccess : class, IDataAccess
	{
		#region 成员字段
		private readonly Collections.INamedCollection<TDataAccess> _accesses;
		#endregion

		#region 构造函数
		protected DataAccessProviderBase()
		{
			_accesses = new Collections.NamedCollection<TDataAccess>(p => p.Name, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count
		{
			get => _accesses.Count;
		}
		#endregion

		#region 公共方法
		public TDataAccess GetService(string name)
		{
			if(string.IsNullOrEmpty(name))
				name = GetDefaultName();

			if(_accesses.TryGet(name, out var accessor))
				return accessor;

			lock(_accesses)
			{
				if(_accesses.TryGet(name, out accessor))
					return accessor;

				_accesses.Add(accessor = this.CreateAccessor(name));
			}

			return accessor;
		}

		public IDataAccess GetAccessor(string name)
		{
			if(string.IsNullOrEmpty(name))
				name = GetDefaultName();

			if(_accesses.TryGet(name, out var accessor))
				return accessor;

			lock(_accesses)
			{
				if(_accesses.TryGet(name, out accessor))
					return accessor;

				_accesses.Add(accessor = this.CreateAccessor(name));
			}

			return accessor;
		}

		public bool TryGetAccessor(string name, out IDataAccess accessor)
		{
			if(string.IsNullOrEmpty(name))
				name = GetDefaultName();

			if(_accesses.TryGet(name, out var result))
			{
				accessor = result;
				return true;
			}

			accessor = null;
			return false;
		}
		#endregion

		#region 抽象方法
		protected abstract TDataAccess CreateAccessor(string name);
		#endregion

		#region 私有方法
		private static string GetDefaultName()
		{
			var connectionSettings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingCollection>("/Data/ConnectionSettings");

			if(connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.Default))
				throw new InvalidOperationException("Missing the default connection settings.");

			return connectionSettings.Default;
		}
		#endregion

		#region 集合接口
		bool ICollection<TDataAccess>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		void ICollection<TDataAccess>.Add(TDataAccess item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			_accesses.Add(item);
		}

		void ICollection<TDataAccess>.Clear()
		{
			_accesses.Clear();
		}

		bool ICollection<TDataAccess>.Contains(TDataAccess item)
		{
			if(item == null)
				return false;

			return _accesses.Contains(item);
		}

		void ICollection<TDataAccess>.CopyTo(TDataAccess[] array, int arrayIndex)
		{
			_accesses.CopyTo(array, arrayIndex);
		}

		bool ICollection<TDataAccess>.Remove(TDataAccess item)
		{
			return _accesses.Remove(item);
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<TDataAccess> GetEnumerator()
		{
			return _accesses.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _accesses.GetEnumerator();
		}
		#endregion
	}
}
