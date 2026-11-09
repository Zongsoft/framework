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
using System.Runtime;
using System.Reflection;
using System.Runtime.InteropServices;

using Pipelines.Sockets.Unofficial;

using Zongsoft.Components;

namespace Zongsoft.Net.Benchmarks;

internal static class EnvironmentReport
{
	public static object Capture() => new
	{
		Timestamp = DateTimeOffset.UtcNow,
		Framework = RuntimeInformation.FrameworkDescription,
		Target = AppContext.TargetFrameworkName,
		OS = RuntimeInformation.OSDescription,
		Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
		Environment.ProcessorCount,
		AvailableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
		GCSettings.IsServerGC,
		LatencyMode = GCSettings.LatencyMode.ToString(),
		TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
		TieredPGO = Environment.GetEnvironmentVariable("DOTNET_TieredPGO"),
		Net = Describe(typeof(TcpClient).Assembly),
		Core = Describe(typeof(HandlerBase<>).Assembly),
		Transport = Describe(typeof(SocketConnection).Assembly),
	};

	private static object Describe(Assembly assembly) => new
	{
		assembly.Location,
		Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
		assembly.ManifestModule.ModuleVersionId,
	};
}
