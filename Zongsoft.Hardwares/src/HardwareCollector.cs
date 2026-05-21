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
 * This file is part of Zongsoft.Hardwares library.
 *
 * The Zongsoft.Hardwares is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Hardwares is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Hardwares library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Hardwares;

/// <summary>
/// 表示当前机器硬件信息的采集器。
/// </summary>
[Service<IO.Hardwares.IHardwareCollector>(Members = nameof(Instance))]
public sealed partial class HardwareCollector : IO.Hardwares.IHardwareCollector
{
	#region 单例字段
	public static readonly HardwareCollector Instance = new();
	#endregion

	#region 私有构造
	private HardwareCollector() { }
	#endregion

	#region 采集方法
	public IEnumerable<IO.Hardwares.IHardware> Collect() => OnCollect();
	public async IAsyncEnumerable<IO.Hardwares.IHardware> CollectAsync([System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		foreach(var hardware in this.Collect())
		{
			cancellation.ThrowIfCancellationRequested();
			yield return hardware;
			await Task.Yield();
		}
	}
	#endregion

	#region 私有方法
	private static IEnumerable<IO.Hardwares.IHardware> OnCollect()
	{
		var networks = GetNetworks();

		if(OperatingSystem.IsWindows())
			return WindowsGatherer.Gather().Concat(networks);

		if(OperatingSystem.IsLinux())
			return LinuxGatherer.Gather().Concat(networks);

		if(OperatingSystem.IsMacOS())
			return MacosGatherer.Gather().Concat(networks);

		return networks;
	}
	#endregion
}
