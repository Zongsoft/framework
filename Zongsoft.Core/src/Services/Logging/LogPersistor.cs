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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Caching;

namespace Zongsoft.Services.Logging;

public class LogPersistor<TLog> : ILogPersistor<TLog> where TLog : ILog
{
	#region 常量定义
	private const int LIMIT = 100;
	private const int PERIOD = 1; //Unit: Seconds
	#endregion

	#region 成员字段
	private readonly Spooler<TLog> _spooler;
	#endregion

	#region 构造函数
	public LogPersistor() : this(TimeSpan.FromSeconds(PERIOD)) { }
	public LogPersistor(TimeSpan period, int limit = LIMIT)
	{
		_spooler = new(this.OnFlushAsync, period, limit);
		this.Persistors = new LogPersistorCollection();
	}
	#endregion

	#region 公共属性
	public ICollection<ILogPersistor<TLog>> Persistors { get; }
	#endregion

	#region 公共方法
	public ValueTask FlushAsync() => _spooler.FlushAsync();

	public ValueTask PersistAsync(TLog log, CancellationToken cancellation = default)
	{
		if(log is not null)
			return _spooler.PutAsync(log, cancellation);

		return ValueTask.CompletedTask;
	}

	public async ValueTask<int> PersistAsync(IEnumerable<TLog> logs, CancellationToken cancellation = default)
	{
		if(logs == null)
			return 0;

		int count = 0;

		foreach(var log in logs)
		{
			if(log is null)
				continue;

			await _spooler.PutAsync(log, cancellation);
			count++;
		}

		return count;
	}
	#endregion

	#region 私有方法
	private async ValueTask OnFlushAsync(IEnumerable<TLog> logs)
	{
		foreach(var persistor in this.Persistors)
			await PersistAsync(persistor, logs);

		static async ValueTask PersistAsync(ILogPersistor<TLog> persistor, IEnumerable<TLog> logs)
		{
			if(persistor == null || logs == null)
				return;

			await persistor.PersistAsync(logs);
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class LogPersistorCollection : ICollection<ILogPersistor<TLog>>
	{
		#region 成员字段
		#if NET9_0_OR_GREATER
		private readonly Lock _lock = new();
		#else
		private readonly object _lock = new();
		#endif
		private readonly List<ILogPersistor<TLog>> _persistors = [];
		private bool _initialized;
		#endregion

		#region 初始化器
		private void Initialize()
		{
			if(_initialized)
				return;

			lock(_lock)
			{
				if(_initialized)
					return;

				var persistors = ApplicationContext.Current?.Services.ResolveAll<ILogPersistor<TLog>>();
				foreach(var persistor in persistors)
					_persistors.Add(persistor);

				_initialized = true;
			}
		}
		#endregion

		#region 属性定义
		bool ICollection<ILogPersistor<TLog>>.IsReadOnly => false;
		public int Count
		{
			get
			{
				this.Initialize();
				return _persistors.Count;
			}
		}
		#endregion

		#region 公共方法
		public void Add(ILogPersistor<TLog> item)
		{
			this.Initialize();
			_persistors.Add(item);
		}

		public void Clear()
		{
			this.Initialize();
			_persistors.Clear();
		}

		public bool Contains(ILogPersistor<TLog> item)
		{
			this.Initialize();
			return _persistors.Contains(item);
		}

		public void CopyTo(ILogPersistor<TLog>[] array, int arrayIndex)
		{
			this.Initialize();
			_persistors.CopyTo(array, arrayIndex);
		}

		public bool Remove(ILogPersistor<TLog> item)
		{
			this.Initialize();
			return _persistors.Remove(item);
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<ILogPersistor<TLog>> GetEnumerator()
		{
			this.Initialize();
			return _persistors.GetEnumerator();
		}
		#endregion
	}
	#endregion
}
