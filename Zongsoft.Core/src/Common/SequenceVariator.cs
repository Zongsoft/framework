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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Common;

public class SequenceVariator : ISequence
{
	public long Decrease(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null) => throw new NotImplementedException();
	public double Decrease(string key, double interval, double seed = 0, TimeSpan? expiry = null) => throw new NotImplementedException();
	public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<double> DecreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public long Increase(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null) => throw new NotImplementedException();
	public double Increase(string key, double interval, double seed = 0, TimeSpan? expiry = null) => throw new NotImplementedException();
	public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<double> IncreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public void Reset(string key, int value = 0, TimeSpan? expiry = null) => throw new NotImplementedException();
	public void Reset(string key, double value, TimeSpan? expiry = null) => throw new NotImplementedException();
	public ValueTask ResetAsync(string key, int value = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask ResetAsync(string key, double value, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
}
