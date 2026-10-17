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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;

namespace Zongsoft.Net.Benchmarks;

internal sealed record LoadOptions(int Port, int Connections, int Messages, int PayloadLength, int Warmup, int Timeout)
{
	#region 负载设置
	public int Batch { get; init; } = 1;
	public int Rate { get; init; }
	public int Rounds { get; init; } = 1;
	public int IdleSeconds { get; init; }
	public int SettleSeconds { get; init; }
	public int HeartbeatSeconds { get; init; } = 60;
	public int AccumulationMilliseconds { get; init; }
	public int Mixed { get; init; }
	public int Broadcast { get; init; }
	public int Disturbance { get; init; }
	public int Senders { get; init; } = 1;
	public int ZeroLengthReads { get; init; } = 1;
	public int FrameBudget { get; init; } = 64;
	public int ByteBudget { get; init; } = 65536;
	public int Diagnostics { get; init; }
	public int TotalConnections => this.Connections + (this.Mixed == 0 ? 0 : 2) + (this.Disturbance == 0 ? 0 : 4);
	#endregion

	#region 命令解析
	public static LoadOptions Parse(string[] args)
	{
		var options = new LoadOptions(7969, 16, 1000, 128, 100, 60);
		if(args.Length % 2 != 0)
			throw new ArgumentException(null, nameof(args));

		for(var i = 0; i < args.Length; i += 2)
		{
			var value = int.Parse(args[i + 1], CultureInfo.InvariantCulture);

			options = args[i] switch
			{
				"--port" => options with { Port = value },
				"--connections" => options with { Connections = value },
				"--messages" => options with { Messages = value },
				"--size" => options with { PayloadLength = value },
				"--warmup" => options with { Warmup = value },
				"--timeout" => options with { Timeout = value },
				"--batch" => options with { Batch = value },
				"--rate" => options with { Rate = value },
				"--rounds" => options with { Rounds = value },
				"--idle" => options with { IdleSeconds = value },
				"--settle" => options with { SettleSeconds = value },
				"--heartbeat" => options with { HeartbeatSeconds = value },
				"--accumulate-ms" => options with { AccumulationMilliseconds = value },
				"--mixed" => options with { Mixed = value },
				"--broadcast" => options with { Broadcast = value },
				"--disturbance" => options with { Disturbance = value },
				"--senders" => options with { Senders = value },
				"--zero-length-reads" => options with { ZeroLengthReads = value },
				"--frame-budget" => options with { FrameBudget = value },
				"--byte-budget" => options with { ByteBudget = value },
				"--diagnostics" => options with { Diagnostics = value },
				_ => throw new ArgumentOutOfRangeException(nameof(args)),
			};
		}

		ArgumentOutOfRangeException.ThrowIfNegative(options.Port);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Port, 65535);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Connections, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Connections, 100_000);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Messages, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(options.PayloadLength);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.PayloadLength, 65536);
		ArgumentOutOfRangeException.ThrowIfNegative(options.Warmup);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Warmup, 1_000_000);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Timeout, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Timeout, 3600);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Batch, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Batch, 512);
		ArgumentOutOfRangeException.ThrowIfNegative(options.Rate);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Rounds, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Rounds, 10);
		ArgumentOutOfRangeException.ThrowIfNegative(options.SettleSeconds);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.SettleSeconds, 60);
		ArgumentOutOfRangeException.ThrowIfNegative(options.IdleSeconds);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.IdleSeconds, 600);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.HeartbeatSeconds, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.HeartbeatSeconds, 300);
		ArgumentOutOfRangeException.ThrowIfNegative(options.AccumulationMilliseconds);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.AccumulationMilliseconds, 1000);
		ArgumentOutOfRangeException.ThrowIfNegative(options.Broadcast);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Broadcast, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(options.Disturbance);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Disturbance, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(options.Mixed);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Mixed, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.Senders, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Senders, 64);
		ArgumentOutOfRangeException.ThrowIfNegative(options.ZeroLengthReads);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.ZeroLengthReads, 1);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.FrameBudget);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ByteBudget);
		ArgumentOutOfRangeException.ThrowIfNegative(options.Diagnostics);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Diagnostics, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan((long)options.Connections * options.Messages * options.Rounds, 10_000_000);
		ArgumentOutOfRangeException.ThrowIfGreaterThan((long)options.Connections * options.Batch * (options.PayloadLength + 4), 512 * 1024 * 1024);

		return options;
	}
	#endregion
}
