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
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Diagnostics;
using Zongsoft.Configuration;

using Velopack;

namespace Zongsoft.Externals.Velopack;

public sealed class Upgrader : WorkerBase
{
	private UpdateManager _manager;
	private readonly Common.Timer _timer;

	public Upgrader()
	{
		_timer = new(TimeSpan.FromMinutes(1), this.OnTick);
	}

	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var settings = ApplicationContext.Current.Configuration.GetConnectionSettings(
			Configuration.VelopackConnectionSettingsDriver.PATH,
			ApplicationContext.Current.Name,
			Configuration.VelopackConnectionSettingsDriver.NAME);

		if(settings.TryGetValue<TimeSpan>("period", out var period) && period.TotalSeconds > 30)
			_timer.Period = period;

		_manager = new("", new() { }, null);
		_timer.Start(args, cancellation);
		return default;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		_timer.Stop();
		_manager = null;
		return default;
	}

	private async ValueTask OnTick(object state, CancellationToken cancellation)
	{
		var manager = _manager;
		if(manager == null)
			return;

		try
		{
			var info = await manager.CheckForUpdatesAsync();
			if(info == null)
				return;

			await manager.DownloadUpdatesAsync(info, this.OnDownload, cancellation);
			manager.ApplyUpdatesAndRestart(info);
		}
		catch(Exception ex)
		{
			await Logging.GetLogging<Upgrader>().ErrorAsync(ex, cancellation);
		}
	}

	private void OnDownload(int progress)
	{
	}
}
