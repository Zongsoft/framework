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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Configuration
{
	public class RedisConfigurationProvider : ConfigurationProvider
	{
		#region 私有字段
		private readonly RedisConfigurationSource _source;
		#endregion

		#region 构造函数
		public RedisConfigurationProvider(RedisConfigurationSource source)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
		}
		#endregion

		#region 加载方法
		public override void Load()
		{
			var redis = RedisServiceProvider.GetRedis(_source.Name);
			var entry = redis.GetEntry(_source.Namespace, out RedisEntryType entryType);

			if(entry == null || entryType == RedisEntryType.None)
			{
				this.Data = redis.CreateDictionary(_source.Namespace);
				return;
			}

			if(entry is IDictionary<string, string> dictionary)
				this.Data = dictionary;
			else
				this.Data = new RedisConfigurationDictionary(redis.Server, redis.Database, _source.Namespace);
		}
		#endregion

		#region 嵌套子类
		private class RedisConfigurationDictionary : IDictionary<string, string>
		{
			private readonly IServer _server;
			private readonly IDatabase _database;
			private readonly string _namespace;

			internal RedisConfigurationDictionary(IServer server, IDatabase database, string @namespace)
			{
				_server  = server ?? throw new ArgumentNullException(nameof(server));
				_database = database ?? throw new ArgumentNullException(nameof(database));

				if(string.IsNullOrEmpty(@namespace))
					throw new ArgumentNullException(nameof(@namespace));

				_namespace = @namespace;
			}

			public int Count => _server.Scan(_database.Database, GetPattern(_namespace)).Count();
			public bool IsReadOnly => false;

			public string this[string key]
			{
				get => _database.StringGet(GetKey(key));
				set => _database.StringSet(GetKey(key), value);
			}

			public ICollection<string> Keys => _server.Scan(_database.Database, GetPattern(_namespace))
				.Select(key => ((string)key)[(_namespace.Length + 1)..])
				.ToArray();

			public ICollection<string> Values
			{
				get
				{
					var result = new List<string>();

					foreach(var key in _server.Scan(_database.Database, GetPattern(_namespace)))
					{
						var value = _database.StringGet(key);
						result.Add(value);
					}

					return result;
				}
			}

			public void Add(string key, string value)
			{
				if(!_database.StringSet(key, value, when: When.NotExists))
					throw new ArgumentException($"The specified '{key}' key already exists in the '{_namespace}' dictionary.");
			}

			void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> field) => this.Add(field.Key, field.Value);

			public bool Remove(string key) => _database.KeyDelete(GetKey(key));
			bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> field) => this.Remove(field.Key);

			public void Clear()
			{
				foreach(var key in _server.Scan(_database.Database, GetPattern(_namespace)))
					_database.KeyDelete(key);
			}

			public bool Contains(string key) => _database.KeyExists(GetKey(key));
			bool IDictionary<string, string>.ContainsKey(string key) => this.Contains(key);
			bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> field) => this.Contains(field.Key);

			public bool TryGetValue(string key, out string value)
			{
				var result = _database.StringGet(GetKey(key));
				value = result;
				return result.HasValue;
			}

			public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
			{
				if(array == null)
					throw new ArgumentNullException(nameof(array));
				if(arrayIndex < 0 || arrayIndex >= array.Length - 1)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				foreach(var key in _server.Scan(_database.Database, GetPattern(_namespace)))
				{
					if(arrayIndex < array.Length)
						array[arrayIndex++] = new KeyValuePair<string, string>(key, _database.StringGet(key));
					else
						break;
				}
			}

			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
			{
				foreach(var key in _server.Scan(_database.Database, GetPattern(_namespace)))
				{
					yield return new(((string)key)[(_namespace.Length + 1)..], _database.StringGet(key));
				}
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetKey(string key) => $"{_namespace}:{key}";
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private static string GetPattern(string @namespace) => $"{@namespace}:*";
		}
		#endregion
	}
}