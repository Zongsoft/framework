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

public class HostOptions : IWebHost
{
	public HostOptions() { }
	public HostOptions(IWebHost host)
	{
		ArgumentNullException.ThrowIfNull(host);
		this.Name = host.Name;
		this.Domain = host.Domain;
	}

	public string Name { get; set; }
	public string Domain { get; set; }
	public ushort Port { get; set; }
	public string RootDirectory { get; set; }
}
