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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Zongsoft.Externals.Aliyun.Storages
{
	[Serializable]
	public class StorageSearchResult : IEnumerable<Zongsoft.IO.PathInfo>, IEnumerator<Zongsoft.IO.PathInfo>
	{
		#region 私有变量
		private int _index;
		private string _marker;
		private IList<Zongsoft.IO.PathInfo> _items;
		#endregion

		#region 成员字段
		private string _name;
		private string _pattern;
		private StorageSearchResultResolver _resolver;
		#endregion

		#region 构造函数
		internal StorageSearchResult(string name, string pattern, string marker, StorageSearchResultResolver resolver)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			_index = -1;
			_name = name.Trim('/', ' ', '\t', '\r', '\n');
			_pattern = pattern;
			_marker = marker;
			_items = new List<Zongsoft.IO.PathInfo>();
			_resolver = resolver;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取结果列表所属的存储容器(Bucket)名称。
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		public string Pattern
		{
			get
			{
				return _pattern;
			}
		}
		#endregion

		#region 内部属性
		internal StorageSearchResultResolver Resolver
		{
			get
			{
				return _resolver;
			}
		}
		#endregion

		#region 内部方法
		internal Zongsoft.IO.PathInfo Append(string path, long size, DateTimeOffset modifiedTime)
		{
			var items = _items;

			if(items == null)
				throw new ObjectDisposedException(this.GetType().FullName);

			var fullPath = this.GetFullPath(path);
			var url = _resolver.UrlThunk == null ? null : _resolver.UrlThunk(fullPath);

			var info = fullPath.EndsWith("/") ?
				(IO.PathInfo)new Zongsoft.IO.DirectoryInfo(fullPath, null, modifiedTime.LocalDateTime, url) :
				(IO.PathInfo)new Zongsoft.IO.FileInfo(fullPath, size, null, modifiedTime.LocalDateTime, url);

			items.Add(info);
			return info;
		}
		#endregion

		#region 私有方法
		private string GetFullPath(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return "/" + _name + "/";

			return string.Format("/{0}/{1}", _name, path.Trim().TrimStart('/')); //注意：不能移除path尾部的斜杠(/)符号
		}
		#endregion

		#region 遍历方法
		public IEnumerator<Zongsoft.IO.PathInfo> GetEnumerator()
		{
			return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}

		Zongsoft.IO.PathInfo IEnumerator<Zongsoft.IO.PathInfo>.Current
		{
			get
			{
				if(_items == null || _index < 0 || _index >= _items.Count)
					return null;

				return _items[_index];
			}
		}

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return ((IEnumerator<Zongsoft.IO.PathInfo>)this).Current;
			}
		}

		bool System.Collections.IEnumerator.MoveNext()
		{
			if(_items == null)
				return false;

			_index++;

			if(_index < _items.Count)
				return true;

			var resolver = this.Resolver;

			if(resolver == null || string.IsNullOrWhiteSpace(_marker))
				return false;

			var url = resolver.ServiceCenter.GetRequestUrl(_name) + string.Format("?list-type=2&prefix={0}&delimiter=%2F&marker={1}&max-keys=21", Uri.EscapeDataString(_pattern), _marker);
			var response = resolver.Client.GetAsync(url).Result;

			if(response == null || (!response.IsSuccessStatusCode))
				return false;

			_items.Clear();
			_marker = resolver.Reload(this, response);

			if(_items == null || _items.Count < 1)
				return false;

			_index = 0;
			return true;
		}

		void System.Collections.IEnumerator.Reset()
		{
			_index = 0;
		}
		#endregion

		#region 释放资源
		void IDisposable.Dispose()
		{
			var items = System.Threading.Interlocked.Exchange(ref _items, null);

			if(items != null)
			{
				_index = -1;
				items.Clear();

				var resolver = System.Threading.Interlocked.Exchange(ref _resolver, null);

				if(resolver != null)
					resolver.Dispose();
			}
		}
		#endregion
	}
}
