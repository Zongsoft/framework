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
