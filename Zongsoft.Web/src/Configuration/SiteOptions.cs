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
	string IWebSite.Domain => this.Hosts?.GetDefault()?.Domain;
	public HostOptionsCollection Hosts { get; set; }
	IWebHostCollection IWebSite.Hosts => this.Hosts;
}
