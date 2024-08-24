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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Common;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis
{
	public class RedisHashset : ISet<string>, ICollection<string>
	{
		private readonly IDatabase _database;
		private readonly string _name;

		internal RedisHashset(IDatabase database, string name)
		{
			_database = database ?? throw new ArgumentNullException(nameof(database));
			_name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public int Count => (int)_database.SetLength(_name);
		public bool IsReadOnly => false;

		public TimeSpan? GetExpiry() => _database.KeyTimeToLive(_name);

		public bool Add(string item) => item != null && _database.SetAdd(_name, item);
		public long AddRange(IEnumerable<string> items) => items == null ? 0 : _database.SetAdd(_name, items.Where(item => item != null).Select(item => (RedisValue)item).ToArray());
		void ICollection<string>.Add(string item) => this.Add(item);

		public bool Move(string destination, string item) => destination != null && item != null && _database.SetMove(_name, destination, item);
		public bool Remove(string item) => _database.SetRemove(_name, item);
		public long RemoveRange(IEnumerable<string> items) => items == null ? 0 : _database.SetRemove(_name, items.Select(item => (RedisValue)item).ToArray());

		public void Clear() => _database.KeyDelete(_name);
		public bool Contains(string item) => item != null && _database.SetContains(_name, item);

		public void ExceptWith(IEnumerable<string> items)
		{
			var temp = _name + ":" + Randomizer.GenerateString();
			var transaction = _database.CreateTransaction();
			transaction.SetAddAsync(temp, items.Where(item => item != null).Select(item => (RedisValue)item).ToArray()).RunSynchronously();
			transaction.SetCombineAndStoreAsync(SetOperation.Difference, _name, _name, temp).RunSynchronously();
			transaction.Execute();
		}

		public void SymmetricExceptWith(IEnumerable<string> items)
		{
			var temp = _name + ":" + Randomizer.GenerateString();
			var transaction = _database.CreateTransaction();
			transaction.SetAddAsync(temp, items.Where(item => item != null).Select(item => (RedisValue)item).ToArray()).RunSynchronously();
			transaction.SetCombineAndStoreAsync(SetOperation.Difference, _name, _name, temp).RunSynchronously();
			transaction.Execute();
		}

		public void IntersectWith(IEnumerable<string> items)
		{
			var temp = _name + ":" + Randomizer.GenerateString();
			var transaction = _database.CreateTransaction();
			transaction.SetAddAsync(temp, items.Where(item => item != null).Select(item => (RedisValue)item).ToArray()).RunSynchronously();
			transaction.SetCombineAndStoreAsync(SetOperation.Intersect, _name, _name, temp).RunSynchronously();
			transaction.Execute();
		}

		public void UnionWith(IEnumerable<string> items) => this.AddRange(items);
		public bool IsProperSubsetOf(IEnumerable<string> other) => throw new NotImplementedException();
		public bool IsProperSupersetOf(IEnumerable<string> other) => throw new NotImplementedException();
		public bool IsSubsetOf(IEnumerable<string> other) => throw new NotImplementedException();
		public bool IsSupersetOf(IEnumerable<string> other) => throw new NotImplementedException();
		public bool Overlaps(IEnumerable<string> other) => throw new NotImplementedException();
		public bool SetEquals(IEnumerable<string> other) => throw new NotImplementedException();

		void ICollection<string>.CopyTo(string[] array, int arrayIndex)
		{
			if(array == null || array.Length == 0)
				return;

			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			var items = _database.SetMembers(_name).AsSpan();

			for(int i = 0; i < items.Length; i++)
			{
				var destinationIndex = arrayIndex + i;
				if(destinationIndex >= array.Length)
					break;

				array[destinationIndex] = (string)items[i];
			}
		}

		public IEnumerator<string> GetEnumerator() => _database.SetScan(_name).Select(p => p.ToString()).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}
}
