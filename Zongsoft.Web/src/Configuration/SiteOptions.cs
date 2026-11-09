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

namespace Zongsoft.Web.Configuration;

public class SiteOptions : IWebSite
{
	public SiteOptions() { }
	public SiteOptions(IWebSite site)
	{
		ArgumentNullException.ThrowIfNull(site);

		this.Name = site.Name;
		if(site.Hosts != null && site.Hosts.Count > 0)
		{
			this.Hosts ??= [];
			foreach(var host in site.Hosts)
				this.Hosts.Add(new HostOptions(host));
		}
	}

	public string Name { get; set; }
	public HostOptionsCollection Hosts { get; set; }
	IWebHostCollection IWebSite.Hosts => this.Hosts;
}
