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
using System.Diagnostics;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Upgrading;

partial class Upgrader
{
	internal static Process Launch(string deployer, string deployment)
	{
		if(OperatingSystem.IsLinux())
			return LaunchOnLinux(deployer, deployment);

		var info = new ProcessStartInfo(deployer)
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			WorkingDirectory = Application.ApplicationPath,
			Verb = OperatingSystem.IsWindows() ? "runas" : string.Empty,
		};

		//设置部署器程序的参数集
		info.ArgumentList.Add($"{Deployer.Argument.Keys.Site}={Application.Site}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.AppId}={Environment.ProcessId}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.AppType}={Application.ApplicationType}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.AppName}={Application.ApplicationName}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.AppPath}={Application.ApplicationPath}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.HostPath}={Environment.ProcessPath}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.Deployment}={deployment}");
		info.ArgumentList.Add($"{Deployer.Argument.Keys.Daemon}={GetConfiguration(Deployer.Argument.Keys.Daemon)}");

		//获取当前应用程序的命令行参数
		var args = Environment.GetCommandLineArgs();

		//依次将当前应用程序命令行参数加入到部署器的命令行参数集中
		for(int i = 0; i < args.Length; i++)
			info.ArgumentList.Add($"{Deployer.Argument.Keys.HostArgs}#{i}={args[i]}");

		return Process.Start(info);
	}

	private static Process LaunchOnLinux(string deployer, string deployment)
	{
		var text = new System.Text.StringBuilder();

		text.Append($"--scope --slice=system.slice --unit=zongsoft-deployer {deployer}");
		text.Append($" {Deployer.Argument.Keys.Site}={Application.Site}");
		text.Append($" {Deployer.Argument.Keys.AppId}={Environment.ProcessId}");
		text.Append($" {Deployer.Argument.Keys.AppType}={Application.ApplicationType}");
		text.Append($" {Deployer.Argument.Keys.AppName}={Application.ApplicationName}");
		text.Append($" {Deployer.Argument.Keys.AppPath}={Application.ApplicationPath}");
		text.Append($" {Deployer.Argument.Keys.HostPath}={Environment.ProcessPath}");
		text.Append($" {Deployer.Argument.Keys.Deployment}={deployment}");
		text.Append($" {Deployer.Argument.Keys.Daemon}={GetConfiguration(Deployer.Argument.Keys.Daemon)}");

		//获取当前应用程序的命令行参数
		var args = Environment.GetCommandLineArgs();

		//依次将当前应用程序命令行参数加入到部署器的命令行参数集中
		for(int i = 0; i < args.Length; i++)
			text.Append($" {Deployer.Argument.Keys.HostArgs}#{i}={args[i]}");

		var info = new ProcessStartInfo("systemd-run", text.ToString())
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			WorkingDirectory = Application.ApplicationPath,
		};

		return Process.Start(info);
	}

	private static string GetConfiguration(string key) => GetConfiguration(Services.ApplicationContext.Current?.Configuration, key);
	private static string GetConfiguration(IConfiguration configuration, string key) => string.IsNullOrEmpty(key) || configuration == null ? null : configuration.GetSection(key)?.Value;
}
