/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
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
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Upgrading.Tests;

public class UpgraderLifecycleTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task StopAllowsRestartWithNewTimer()
	{
		var upgrader = CreateUpgrader();
		var states = new List<WorkerState>();
		upgrader.StateChanged += (_, args) => states.Add(args.State);

		try
		{
			for(int i = 0; i < 2; i++)
			{
				await upgrader.StartAsync().WaitAsync(_timeout);

				Assert.Equal(WorkerState.Running, upgrader.State);

				await upgrader.StopAsync().WaitAsync(_timeout);

				Assert.Equal(WorkerState.Stopped, upgrader.State);
				Assert.False(upgrader.IsDisposed);
			}

			Assert.Equal([WorkerState.Running, WorkerState.Stopped, WorkerState.Running, WorkerState.Stopped], states);
		}
		finally { await upgrader.DisposeAsync().AsTask().WaitAsync(_timeout); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposalStopsAndPermanentlyClosesWorker(bool started)
	{
		var upgrader = CreateUpgrader();

		try
		{
			if(started)
				await upgrader.StartAsync().WaitAsync(_timeout);

			await upgrader.DisposeAsync().AsTask().WaitAsync(_timeout);

			Assert.Equal(WorkerState.Stopped, upgrader.State);
			Assert.True(upgrader.IsDisposed);

			await upgrader.DisposeAsync().AsTask().WaitAsync(_timeout);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => upgrader.StartAsync());
			await Assert.ThrowsAsync<ObjectDisposedException>(() => upgrader.StopAsync());
		}
		finally { await upgrader.DisposeAsync().AsTask().WaitAsync(_timeout); }
	}

	private static Upgrader CreateUpgrader()
	{
		Assert.False(Deployer.HasDeployment());

		//小于五分钟的周期跳过首次十秒检查；不存在的通道阻止访问任何发布源。
		return new()
		{
			Channel = nameof(UpgraderLifecycleTest),
			Period = TimeSpan.FromMinutes(4),
		};
	}
}
