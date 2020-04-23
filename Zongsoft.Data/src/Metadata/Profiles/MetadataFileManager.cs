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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata.Profiles
{
	public class MetadataFileManager : IDataMetadataManager
	{
		#region 成员字段
		private readonly string _name;
		private readonly object _syncRoot;
		private readonly MetadataCollection _metadatas;
		private readonly CommandCollection _commands;
		private readonly EntityCollection _entities;

		private bool _isLoaded;
		private IDataMetadataLoader _loader;
		#endregion

		#region 构造函数
		public MetadataFileManager(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_syncRoot = new object();
			_isLoaded = false;
			_loader = new MetadataFileLoader();
			_metadatas = new MetadataCollection(this);
			_commands = new CommandCollection(_metadatas);
			_entities = new EntityCollection(_metadatas);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取元数据管理器所属的应用名。
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		/// <summary>
		/// 获取或设置当前应用的元数据文件加载器。
		/// </summary>
		public IDataMetadataLoader Loader
		{
			get
			{
				return _loader;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				if(object.ReferenceEquals(_loader, value))
					return;

				_loader = value;

				//将加载标记重置为未加载，以待后续重启加载
				_isLoaded = false;
			}
		}

		/// <summary>
		/// 获取元数据提供程序集（即数据映射文件集合）。
		/// </summary>
		public ICollection<IDataMetadataProvider> Providers
		{
			get
			{
				//如果未加载过元数据提供程序，则尝试加载它
				if(!_isLoaded)
					this.EnsureLoad();

				return _metadatas;
			}
		}

		/// <summary>
		/// 获取全局的实体元数据集。
		/// </summary>
		public Collections.IReadOnlyNamedCollection<IDataEntity> Entities
		{
			get
			{
				//如果未加载过元数据提供程序，则尝试加载它
				if(!_isLoaded)
					this.EnsureLoad();

				return _entities;
			}
		}

		/// <summary>
		/// 获取全局的命令元数据集。
		/// </summary>
		public Collections.IReadOnlyNamedCollection<IDataCommand> Commands
		{
			get
			{
				//如果未加载过元数据提供程序，则尝试加载它
				if(!_isLoaded)
					this.EnsureLoad();

				return _commands;
			}
		}
		#endregion

		#region 加载方法
		public void Reload()
		{
			var loader = _loader;

			//如果加载器为空则退出
			if(loader == null)
				return;

			//加载当前应用的所有元数据提供程序
			var items = loader.Load(_name);

			_metadatas.Clear();
			_metadatas.AddRange(items);

			//设置加载标记为完成
			_isLoaded = true;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureLoad()
		{
			if(!_isLoaded)
			{
				lock(_syncRoot)
				{
					if(!_isLoaded)
						this.Reload();
				}
			}
		}
		#endregion

		#region 嵌套子类
		private class MetadataCollection : ICollection<IDataMetadataProvider>
		{
			#region 成员字段
			private readonly IDataMetadataManager _manager;
			private readonly List<IDataMetadataProvider> _items;
			#endregion

			#region 构造函数
			public MetadataCollection(IDataMetadataManager manager)
			{
				_manager = manager;
				_items = new List<IDataMetadataProvider>();
			}
			#endregion

			#region 公共属性
			public int Count => _items.Count;

			public bool IsReadOnly => false;
			#endregion

			#region 公共方法
			public void Add(IDataMetadataProvider item)
			{
				if(item == null)
					throw new ArgumentNullException(nameof(item));

				if(string.IsNullOrEmpty(item.Name) || string.Equals(_manager.Name, item.Name, StringComparison.OrdinalIgnoreCase))
				{
					item.Manager = _manager;
					_items.Add(item);
				}
				else
					throw new ArgumentException($"The '{item.Name}' metadata provider is invalid.");
			}

			public void AddRange(IEnumerable<IDataMetadataProvider> items)
			{
				if(items == null)
					return;

				foreach(var item in items)
				{
					if(!string.IsNullOrEmpty(item.Name) &&
					   !string.Equals(_manager.Name, item.Name, StringComparison.OrdinalIgnoreCase))
						throw new ArgumentException($"The '{item.Name}' metadata provider is invalid.");

					item.Manager = _manager;

					//将当前遍历项加入到集合中
					_items.Add(item);
				}
			}

			public void Clear()
			{
				_items.Clear();
			}

			public bool Contains(IDataMetadataProvider item)
			{
				if(item == null)
					return false;

				return _items.Contains(item);
			}

			public void CopyTo(IDataMetadataProvider[] array, int arrayIndex)
			{
				_items.CopyTo(array, arrayIndex);
			}

			public bool Remove(IDataMetadataProvider item)
			{
				if(item == null)
					return false;

				return _items.Remove(item);
			}
			#endregion

			#region 枚举遍历
			public IEnumerator<IDataMetadataProvider> GetEnumerator()
			{
				return _items.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _items.GetEnumerator();
			}
			#endregion
		}

		private class EntityCollection : Collections.IReadOnlyNamedCollection<IDataEntity>
		{
			#region 成员字段
			private readonly ICollection<IDataMetadataProvider> _metadatas;
			#endregion

			#region 构造函数
			public EntityCollection(ICollection<IDataMetadataProvider> metadatas)
			{
				_metadatas = metadatas ?? throw new ArgumentNullException(nameof(metadatas));
			}
			#endregion

			#region 公共成员
			public int Count
			{
				get
				{
					return _metadatas.Sum(p => p.Entities.Count);
				}
			}

			public IEnumerable<string> Keys
			{
				get => _metadatas.Select(p => p.Name);
			}

			public IDataEntity this[string name]
			{
				get
				{
					return this.Get(name);
				}
			}

			public bool Contains(string name)
			{
				return _metadatas.Any(p => p.Entities.Contains(name));
			}

			public IDataEntity Get(string name)
			{
				IDataEntity entity;

				foreach(var metadata in _metadatas)
				{
					if(metadata.Entities.TryGet(name, out entity))
						return entity;
				}

				throw new KeyNotFoundException($"The specified '{name}' entity does not exist.");
			}

			public bool TryGet(string name, out IDataEntity value)
			{
				value = null;

				foreach(var metadata in _metadatas)
				{
					if(metadata.Entities.TryGet(name, out value))
						return true;
				}

				return false;
			}

			public IEnumerator<IDataEntity> GetEnumerator()
			{
				foreach(var metadata in _metadatas)
				{
					foreach(var entity in metadata.Entities)
						yield return entity;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			#endregion
		}

		private class CommandCollection : Collections.IReadOnlyNamedCollection<IDataCommand>
		{
			#region 成员字段
			private readonly ICollection<IDataMetadataProvider> _metadatas;
			#endregion

			#region 构造函数
			public CommandCollection(ICollection<IDataMetadataProvider> metadatas)
			{
				_metadatas = metadatas ?? throw new ArgumentNullException(nameof(metadatas));
			}
			#endregion

			#region 公共成员
			public int Count
			{
				get
				{
					return _metadatas.Sum(p => p.Commands.Count);
				}
			}

			public IEnumerable<string> Keys
			{
				get => _metadatas.Select(p => p.Name);
			}

			public IDataCommand this[string name]
			{
				get
				{
					return this.Get(name);
				}
			}

			public bool Contains(string name)
			{
				return _metadatas.Any(p => p.Commands.Contains(name));
			}

			public IDataCommand Get(string name)
			{
				IDataCommand command;

				foreach(var metadata in _metadatas)
				{
					if(metadata.Commands.TryGet(name, out command))
						return command;
				}

				throw new KeyNotFoundException($"The specified '{name}' command does not exist.");
			}

			public bool TryGet(string name, out IDataCommand value)
			{
				value = null;

				foreach(var metadata in _metadatas)
				{
					if(metadata.Commands.TryGet(name, out value))
						return true;
				}

				return false;
			}

			public IEnumerator<IDataCommand> GetEnumerator()
			{
				foreach(var metadata in _metadatas)
				{
					foreach(var command in metadata.Commands)
						yield return command;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			#endregion
		}
		#endregion
	}
}
