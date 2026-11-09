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

public class SiteOptionsCollection() : KeyedCollection<string, SiteOptions>(StringComparer.OrdinalIgnoreCase), IWebSiteCollection
{
	#region 公共属性
	public string Default { get; set; }
	IWebSite IWebSiteCollection.this[string name] => this[name];
	IWebSite IWebSiteCollection.Default => this.GetDefault();
	bool ICollection<IWebSite>.IsReadOnly => false;
	#endregion

	#region 公共方法
	public SiteOptions GetDefault() => this.TryGetValue(this.Default ?? string.Empty, out var value) ? value : (this.Count > 0 ? this.Items[0] : null);
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(SiteOptions site) => site.Name;
	#endregion

	#region 显式实现
	void ICollection<IWebSite>.Add(IWebSite item)
	{
		if(item is SiteOptions options)
			this.Add(options);
		else
			this.Add(new SiteOptions(item));
	}

	bool ICollection<IWebSite>.Contains(IWebSite item) => item != null && this.Contains(item.Name);
	void ICollection<IWebSite>.CopyTo(IWebSite[] array, int arrayIndex) => throw new NotImplementedException();
	IEnumerator<IWebSite> IEnumerable<IWebSite>.GetEnumerator() => this.GetEnumerator();
	bool ICollection<IWebSite>.Remove(IWebSite item) => item != null && this.Remove(item.Name);
	bool IWebSiteCollection.TryGetValue(string name, out IWebSite value)
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
