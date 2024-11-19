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
using System.Linq;

namespace Zongsoft.Data.Metadata
{
	public class DataMetadataContainer : IDataMetadataContainer
	{
		#region 成员字段
		private readonly DataCommandCollection _commands;
		private readonly DataEntityCollection _entities;

		private bool _initialized;
		private readonly object _locker;
		#endregion

		#region 构造函数
		public DataMetadataContainer(string name)
		{
			this.Name = name ?? string.Empty;
			_locker = new object();
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
				return _entities;
			}
		}

		public IDataCommandCollection Commands
		{
			get
			{
				this.Initialize();
				return _commands;
			}
		}
		#endregion

		#region 加载方法
		public void Reload()
		{
			lock(_locker)
			{
				_entities.Clear();
				_commands.Clear();

				this.Load();
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void Initialize()
		{
			if(_initialized)
				return;

			lock(_locker)
			{
				if(_initialized)
					return;

				_entities.Clear();
				_commands.Clear();

				this.Load();

				_initialized = true;
			}
		}
		#endregion

		#region 私有方法
		private void Load()
		{
			foreach(var loader in DataEnvironment.Loaders)
			{
				var results = loader.Load(this.Name);

				foreach(var result in results)
				{
					foreach(var command in result.Commands)
						SetCommand(command);
					foreach(var entity in result.Entities)
						SetEntity(entity);
				}
			}
		}

		private void SetEntity(IDataEntity entity)
		{
			if(entity == null)
				return;

			if(_entities.TryAdd(entity))
			{
				entity.Container ??= this;
				return;
			}

			var existed = _entities[entity.Name, entity.Namespace];

			foreach(var property in entity.Properties)
			{
				existed.Properties.Add(property);
			}

			if(!existed.HasKey && entity.HasKey)
			{
				if(existed is Profiles.MetadataEntity metadataEntity)
					metadataEntity.SetKey(entity.Key.Select(key => key.Name).ToArray());

				existed.Immutable = entity.Immutable;
			}

			if(string.IsNullOrEmpty(existed.Alias))
				existed.Alias = entity.Alias;
			if(string.IsNullOrEmpty(existed.BaseName))
				existed.BaseName = entity.BaseName;
			if(string.IsNullOrEmpty(existed.Driver))
				existed.Driver = entity.Driver;
		}

		private void SetCommand(IDataCommand command)
		{
			if(command == null)
				return;

			if(_commands.TryAdd(command))
			{
				command.Container ??= this;
				return;
			}

			throw new DataException($"The specified '{command}' data command mapping cannot be defined repeatedly.");
		}
		#endregion
	}
}
