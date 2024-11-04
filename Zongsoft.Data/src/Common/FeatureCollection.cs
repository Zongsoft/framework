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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	public class FeatureCollection : ICollection<Feature>
	{
		#region 静态常量
		private static readonly Version ZERO_VERSION = new();
		#endregion

		#region 成员字段
		private readonly FeatureCollection _parent;
		private readonly IDictionary<string, Version[]> _features;
		#endregion

		#region 构造函数
		public FeatureCollection()
		{
			_features = new Dictionary<string, Version[]>(StringComparer.OrdinalIgnoreCase);
		}

		public FeatureCollection(FeatureCollection parent)
		{
			_parent = parent;
			_features = new Dictionary<string, Version[]>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count => _features.Count;
		#endregion

		#region 公共方法
		public void Add(Feature feature)
		{
			if(feature == null)
				throw new ArgumentNullException(nameof(feature));

			this.Add(feature.Name, feature.Version);
		}

		public void Add(string name, Version version = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(version == null)
				version = ZERO_VERSION;

			if(_features.TryGetValue(name, out var versions))
			{
				var index = -1;

				for(int i = 0; i < versions.Length; i++)
				{
					if(versions[i] == version)
						return;

					if(versions[i] < version)
					{
						index = i;
						break;
					}
				}

				var vers = _features[name] = new Version[versions.Length + 1];

				if(index < 0)
				{
					Array.Copy(versions, vers, versions.Length);
					vers[versions.Length] = version;
				}
				else if(index == 0)
				{
					Array.Copy(versions, 0, vers, 1, versions.Length);
					vers[0] = version;
				}
				else
				{
					Array.Copy(versions, 0, vers, 0, index);
					Array.Copy(versions, index, vers, index + 1, versions.Length - index);

					vers[index] = version;
				}
			}
			else
			{
				_features.Add(name, new Version[] { version });
			}
		}

		public void Clear() => _features.Clear();
		public bool Remove(Feature feature) => feature != null && this.Remove(feature.Name, feature.Version);
		public bool Remove(string name) => name != null && _features.Remove(name);
		public bool Remove(string name, Version version)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(version == null)
				return _features.Remove(name);

			if(_features.TryGetValue(name, out var versions))
			{
				var index = Array.IndexOf(versions, version);

				if(index >= 0)
				{
					if(versions.Length <= 1)
						_features.Remove(name);
					else
					{
						var vers = new Version[versions.Length - 1];

						Array.Copy(versions, 0, vers, 0, index);

						if(index < versions.Length - 1)
							Array.Copy(versions, index + 1, vers, index, versions.Length - index);

						_features[name] = vers;
					}
				}
			}

			return false;
		}

		public bool Support(Feature feature) => feature != null && this.Support(feature.Name, feature.Version);
		public bool Support(string name, Version version = null)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(_parent != null)
			{
				if(_parent.Support(name, version))
					return true;
			}

			return this.OnSupport(name, version);
		}
		#endregion

		#region 虚拟方法
		protected virtual bool OnSupport(string name, Version version)
		{
			if(version == null || version == ZERO_VERSION)
				return _features.ContainsKey(name);

			if(_features.TryGetValue(name, out var versions))
			{
				if(versions == null || versions.Length == 0)
					return true;

				foreach(var ver in versions)
				{
					if(version <= ver)
						return true;
				}
			}

			return false;
		}
		#endregion

		#region 显式实现
		bool ICollection<Feature>.IsReadOnly => false;
		bool ICollection<Feature>.Contains(Feature feature)
		{
			if(feature == null)
				return false;

			if(_features.TryGetValue(feature.Name, out var versions))
				return feature.Version == null ||
					   feature.Version == ZERO_VERSION ||
					   Array.Exists(versions, ver => ver == feature.Version);

			return false;
		}

		void ICollection<Feature>.CopyTo(Feature[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException(nameof(array));

			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			foreach(var feature in _features)
			{
				var versions = feature.Value;

				foreach(var version in versions)
				{
					if(arrayIndex < array.Length)
						array[arrayIndex++] = new Feature(feature.Key, version);
					else
						return;
				}
			}
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Feature> GetEnumerator()
		{
			foreach(var feature in _features)
			{
				foreach(var version in feature.Value)
				{
					yield return new Feature(feature.Key, version);
				}
			}
		}
		#endregion
	}
}
