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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Web.Configuration;

public class HostOptionsCollection() : KeyedCollection<string,  HostOptions>(StringComparer.OrdinalIgnoreCase), IWebHostCollection
{
	#region 公共属性
	public string Default { get; set; }
	IWebHost IWebHostCollection.Default => this.GetDefault();
	IWebHost IWebHostCollection.this[string name] => this[name];
	bool ICollection<IWebHost>.IsReadOnly => false;
	#endregion

	#region 公共方法
	public HostOptions GetDefault() => this.TryGetValue(this.Default ?? string.Empty, out var value) ? value : (this.Count > 0 ? this.Items[0] : null);
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(HostOptions host) => host.Name;
	#endregion

	#region 显式实现
	void ICollection<IWebHost>.Add(IWebHost item)
	{
		if(item is HostOptions options)
			this.Add(options);
		else
			this.Add(new HostOptions(item));
	}

	bool ICollection<IWebHost>.Contains(IWebHost item) => item != null && this.Contains(item.Name);
	void ICollection<IWebHost>.CopyTo(IWebHost[] array, int arrayIndex) => throw new NotImplementedException();
	IEnumerator<IWebHost> IEnumerable<IWebHost>.GetEnumerator() => this.GetEnumerator();
	bool ICollection<IWebHost>.Remove(IWebHost item) => item != null && this.Remove(item.Name);
	bool IWebHostCollection.TryGetValue(string name, out IWebHost value)
	{
		if(this.TryGetValue(name, out var host))
		{
			value = host;
			return true;
		}
		else
		{
			value = null;
			return false;
		}
	}
	#endregion
}
