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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Zongsoft.Net.Benchmarks;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		if(args.Length == 0 || args[0] is "--help" or "help")
		{
			Console.WriteLine("micro --filter <pattern> [BenchmarkDotNet options]");
			Console.WriteLine("server|client [--port 7969] [--connections 16] [--messages 1000] [--size 128] [--warmup 100] [--timeout 60]");
			Console.WriteLine("server/client: original client-path comparison; service/device: bounded batches and high/idle/high phases; service/contention: concurrent Net senders.");
			Console.WriteLine("service|device|contention also accept --batch 1 --rate 0 --rounds 1 --idle 0 --settle 0 --heartbeat 60 --mixed 0 --broadcast 0 --disturbance 0 --senders 1 --zero-length-reads 1 --frame-budget 64 --byte-budget 65536 --diagnostics 0 --accumulate-ms 0.");
			return 0;
		}

		try
		{
			if(args[0] == "micro")
			{
				var arguments = args[1..];
				if(!arguments.Contains("--filter") && !arguments.Contains("--list"))
					throw new ArgumentException(null, nameof(args));

				var configuration = DefaultConfig.Instance.AddJob(Job.Default.WithArguments([
					new MsBuildArgument("/p:GeneratePackageOnBuild=false"),
					new MsBuildArgument("/bl:{}"),
				]).AsMutator());
				var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(arguments, configuration).ToArray();
				if(arguments.Contains("--list"))
					return 0;

				return summaries.Length > 0 && summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.All(report => report.Success)) ? 0 : 1;
			}

			var options = LoadOptions.Parse(args[1..]);
			using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(options.Timeout));

			if(args[0] == "server")
				await LoadServer.RunAsync(options, cancellation.Token);
			else if(args[0] == "service")
				await ServiceLoad.RunAsync(options, cancellation.Token);
			else if(args[0] == "device")
				await DeviceLoad.RunAsync(options, cancellation.Token);
			else if(args[0] == "contention")
				await ContentionLoad.RunAsync(options, cancellation.Token);
			else if(args[0] == "client")
				await LoadClient.RunAsync(options, cancellation.Token);
			else
				throw new ArgumentOutOfRangeException(nameof(args));

			return 0;
		}
		catch(Exception exception)
		{
			Console.Error.WriteLine(exception);
			return 1;
		}
	}
}
