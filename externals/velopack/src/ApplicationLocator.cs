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
using System.IO;

using NuGet.Versioning;

using Velopack;
using Velopack.Logging;
using Velopack.Locators;

namespace Zongsoft.Externals.Velopack;

public sealed class ApplicationLocator : VelopackLocator
{
	public override string AppId { get; }
	public override string RootAppDir { get; }
	public override string PackagesDir { get; }
	public override string UpdateExePath { get; }
	public override string AppContentDir { get; }
	public override string Channel { get; }
	public override IVelopackLogger Log { get; }
	public override uint ProcessId { get; }
	public override string ProcessExePath { get; }
	public override SemanticVersion CurrentlyInstalledVersion { get; }

	public ApplicationLocator()
	{
		var manifest = ApplicationManifest.Current;
		this.AppId = manifest.Name;
		this.RootAppDir = AppContext.BaseDirectory;
		this.AppContentDir = AppContext.BaseDirectory;
		this.PackagesDir = Path.Combine(AppContext.BaseDirectory, ".packages");
		this.UpdateExePath = Path.Combine(AppContext.BaseDirectory, ".upgrader", "Update.exe");
		this.Channel = VelopackRuntimeInfo.GetOsShortName(VelopackRuntimeInfo.SystemOs);
		this.CurrentlyInstalledVersion = new(manifest.Version.Major, manifest.Version.Minor, manifest.Version.Build, manifest.Version.Revision.ToString());
		this.ProcessId = (uint)Environment.ProcessId;
		this.ProcessExePath = Environment.ProcessPath;
		this.Log = NullVelopackLogger.Instance;
	}
}
