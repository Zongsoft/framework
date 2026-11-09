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

using Zongsoft.Components;

namespace Zongsoft.Externals.Opc;

public partial class OpcServer
{
	#region 公共属性
	public string Urls
	{
		get => string.Join(',', this.Options.Urls);
		set
		{
			this.EnsureStopped();
			var urls = (value ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);

			if(urls.Length == 0 || Array.Exists(urls, url => string.IsNullOrEmpty(url) ||
			   !Uri.TryCreate(url, UriKind.Absolute, out var address) ||
			   !address.Scheme.Equals("opc.tcp", StringComparison.OrdinalIgnoreCase) || address.IsDefaultPort))
				throw new ArgumentException(null, nameof(value));

			this.Options.Configure(urls, this.Options.Discovery);
		}
	}

	public string Discovery
	{
		get => this.Options.Discovery;
		set
		{
			this.EnsureStopped();
			this.Options.Configure(this.Options.Urls, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
		}
	}
	#endregion

	#region 私有方法
	private void EnsureStopped()
	{
		if(this.State != WorkerState.Stopped)
			throw new InvalidOperationException();
	}
	#endregion
}
