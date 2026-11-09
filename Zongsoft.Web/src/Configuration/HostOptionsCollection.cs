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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Web.Configuration;

public class HostOptionsCollection() : KeyedCollection<string, HostOptions>(StringComparer.OrdinalIgnoreCase), IWebHostCollection
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
