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

using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Hardwares;

internal abstract class HardwareCollectorBase : IO.Hardwares.IHardwareCollector
{
	public abstract IEnumerable<IO.Hardwares.IHardware> Collect();
	public virtual async IAsyncEnumerable<IO.Hardwares.IHardware> CollectAsync([EnumeratorCancellation] CancellationToken cancellation = default)
	{
		foreach(var hardware in this.Collect())
		{
			cancellation.ThrowIfCancellationRequested();
			yield return hardware;
			await System.Threading.Tasks.Task.Yield();
		}
	}
}

internal sealed class EmptyHardwareCollector : HardwareCollectorBase
{
	public static readonly EmptyHardwareCollector Instance = new();

	private EmptyHardwareCollector() { }
	public override IEnumerable<IO.Hardwares.IHardware> Collect() => [];
}
