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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Upgrading;

public abstract class FetcherBase : IFetcher
{
	protected FetcherBase(string name, IDownloader downloader = null)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.Downloader = downloader;
	}

	public string Name { get; }
	public IDownloader Downloader { get; protected set; }

	public async ValueTask<Upgrader.Manifest> FetchAsync(Version version, CancellationToken cancellation = default)
	{
		var baseline = default(Package);
		var deltas = new List<Package>();
		var upgradingVersion = version;
		var currentlyVersion = Utility.ApplicationVersion;

		//获取升级包列表
		var packages = this.OnFetchAsync(version, cancellation);

		await foreach(var package in packages)
		{
			if(!package.Deprecated &&
			   string.Equals(Utility.ApplicationName, package.Name) &&
			   string.Equals(Utility.Platform, package.Platform) &&
			   string.Equals(Utility.Architecture, package.Architecture) &&
			   package.Version > currentlyVersion && (upgradingVersion.IsZero() || package.Version <= upgradingVersion))
			{
				if(package.Kind == PackageKind.Delta)
					deltas.Add(package);
				else if(baseline == null || package.Version > baseline.Version)
					baseline = package;
			}
		}

		return baseline == null ?
			new(deltas.OrderBy(delta => delta.Version).ToArray()) :
			new(baseline, deltas.Where(delta => delta.Version > baseline.Version).OrderBy(delta => delta.Version).ToArray());
	}

	protected abstract IAsyncEnumerable<Package> OnFetchAsync(Version version, CancellationToken cancellation);
	protected virtual IConnectionSettings GetSettings() => ApplicationContext.Current?.Configuration.GetConnectionSettings(nameof(Upgrading), this.Name);
}
