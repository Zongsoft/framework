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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Services;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

public static class Mapping
{
	#region 成员字段
	private static readonly LoaderCollection _loaders;
	private static readonly DataEntityCollection _entities;
	private static readonly DataCommandCollection _commands;
	#endregion

	#region 静态构造
	static Mapping()
	{
		_entities = new();
		_commands = new();
		_loaders = new LoaderCollection();
	}
	#endregion

	#region 公共属性
	/// <summary>获取元数据加载器集合。</summary>
	public static ICollection<Loader> Loaders => _loaders;

	/// <summary>获取元数据容器中的数据实体定义集。</summary>
	public static DataEntityCollection Entities
	{
		get
		{
			if(!_loaders.IsLoaded)
				_loaders.Load();

			return _entities;
		}
	}

	/// <summary>获取元数据容器中的数据命令定义集。</summary>
	public static DataCommandCollection Commands
	{
		get
		{
			if(!_loaders.IsLoaded)
				_loaders.Load();

			return _commands;
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class LoaderCollection : Collection<Loader>
	{
		#region 私有变量
		private volatile int _loads = 0;
		#endregion

		#region 公共属性
		public bool IsLoaded => this.Count > 0 && _loads >= this.Count;
		#endregion

		#region 公共方法
		public void Load()
		{
			if(this.IsLoaded)
				return;

			//初始化加载器集合
			this.Initialize();

			foreach(var loader in this.Items)
			{
				if(loader.IsUnload)
				{
					loader.Load();
					Interlocked.Increment(ref _loads);
				}
			}
		}
		#endregion

		#region 初始化器
		private bool _initialized = false;
		private void Initialize()
		{
			if(_initialized)
				return;

			lock(this)
			{
				if(_initialized)
					return;

				var loaders = ApplicationContext.Current?.Services.ResolveAll<Loader>();
				if(loaders == null)
					return;

				foreach(var loader in loaders)
				{
					if(!this.Contains(loader))
						this.Add(loader);
				}

				_initialized = true;
			}
		}
		#endregion

		#region 重写方法
		protected override void ClearItems() => Interlocked.Exchange(ref _loads, 0);
		protected override void InsertItem(int index, Loader loader)
		{
			if(loader == null)
				throw new ArgumentNullException(nameof(loader));

			if(loader.IsLoaded)
				Interlocked.Increment(ref _loads);
		}

		protected override void SetItem(int index, Loader loader) => throw new NotSupportedException();
		protected override void RemoveItem(int index)
		{
			if(_loads > 0 && index >= 0 && base[index].IsLoaded)
				Interlocked.Decrement(ref _loads);
		}
		#endregion
	}

	public abstract class Loader
	{
		#region 常量定义
		private const int UNLOAD = 0;
		private const int LOADED = 1;
		#endregion

		#region 私有字段
		private long _status = UNLOAD;
		private readonly AutoResetEvent _semaphore = new(true);
		#endregion

		#region 公共属性
		public bool IsUnload => Interlocked.Read(ref _status) == UNLOAD;
		public bool IsLoaded => Interlocked.Read(ref _status) == LOADED;
		#endregion

		#region 公共方法
		public void Load()
		{
			if(this.IsLoaded)
				return;

			try
			{
				//等待信号量
				_semaphore.WaitOne();

				//如果其他线程已加载完成则返回
				if(this.IsLoaded)
					return;

				//加载元数据
				var results = this.OnLoad();

				foreach(var result in results)
				{
					if(result.Entities != null)
					{
						foreach(var entity in result.Entities)
						{
							if(entity != null)
								SetEntity(entity);
						}
					}

					if(result.Commands != null)
					{
						foreach(var command in result.Commands)
						{
							if(command != null)
								SetCommand(command);
						}
					}
				}
			}
			finally
			{
				//设置状态为已加载
				Interlocked.Exchange(ref _status, LOADED);

				//释放信号量
				_semaphore.Set();
			}
		}
		#endregion

		#region 私有方法
		private static void SetEntity(IDataEntity entity)
		{
			if(entity == null)
				return;

			if(Entities.TryAdd(entity))
				return;

			var existed = Entities[entity.Name, entity.Namespace];

			foreach(var property in entity.Properties)
			{
				existed.Properties.Add(property);
			}

			if(string.IsNullOrEmpty(existed.Alias))
				existed.Alias = entity.Alias;
			if(string.IsNullOrEmpty(existed.BaseName))
				existed.BaseName = entity.BaseName;
			if(string.IsNullOrEmpty(existed.Driver))
				existed.Driver = entity.Driver;
		}

		private static void SetCommand(IDataCommand command)
		{
			if(command == null)
				return;

			if(_commands.TryAdd(command))
				return;

			throw new DataException($"The specified '{command}' data command mapping cannot be defined repeatedly.");
		}
		#endregion

		#region 抽象方法
		protected abstract IEnumerable<Result> OnLoad();
		#endregion

		#region 嵌套结构
		public readonly struct Result
		{
			public Result(IEnumerable<IDataCommand> commands, IEnumerable<IDataEntity> entities)
			{
				this.Commands = commands;
				this.Entities = entities;
			}

			public Result(IEnumerable<IDataEntity> entities, IEnumerable<IDataCommand> commands)
			{
				this.Entities = entities;
				this.Commands = commands;
			}

			public IEnumerable<IDataEntity> Entities { get; }
			public IEnumerable<IDataCommand> Commands { get; }
		}
		#endregion
	}
	#endregion
}
