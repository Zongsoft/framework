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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;

namespace Zongsoft.Data.Metadata
{
	public class DataMetadataContainer : IDataMetadataContainer
	{
		#region 成员字段
		private readonly DataCommandCollection _commands;
		private readonly DataEntityCollection _entities;

		private volatile int _initialized;
		private ReaderWriterLockSlim _locker;
		#endregion

		#region 构造函数
		public DataMetadataContainer(string name)
		{
			this.Name = name ?? string.Empty;
			_locker = new ReaderWriterLockSlim();
			_entities = new DataEntityCollection(this);
			_commands = new DataCommandCollection(this);
		}
		#endregion

		#region 公共属性
		public string Name { get; }

		public IDataEntityCollection Entities
		{
			get
			{
				this.Initialize();
				_locker.EnterReadLock();
				return _entities;
			}
		}

		public IDataCommandCollection Commands
		{
			get
			{
				this.Initialize();
				_locker.EnterReadLock();
				return _commands;
			}
		}
		#endregion

		#region 加载方法
		public void Reload()
		{
			if(_locker.TryEnterWriteLock(TimeSpan.FromSeconds(10)))
			{
				_entities.Clear();
				_commands.Clear();

				foreach(var loader in DataEnvironment.Loaders)
					loader.Load(this);
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void Initialize()
		{
			if(Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
				return;

			_entities.Clear();
			_commands.Clear();

			foreach(var loader in DataEnvironment.Loaders)
				loader.Load(this);
		}
		#endregion
	}
}
