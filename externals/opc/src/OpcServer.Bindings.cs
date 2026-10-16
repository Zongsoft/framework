/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  ____  ____ / __/ /_
 *    / /  / __ \/ __ \/ __ \/ __ \/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/ \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
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
