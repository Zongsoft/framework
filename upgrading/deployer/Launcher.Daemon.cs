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
 * The above copyright notice and this permission notice shall be included in all
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Diagnostics;

namespace Zongsoft.Upgrading;

partial class Launcher
{
	private sealed class DaemonLauncher() : Launcher(NAME)
	{
		public const string NAME = "Daemon";

		protected override Process OnLaunch(Deployer.Argument argument)
		{
			string command, args;

			if(OperatingSystem.IsWindows())
			{
				command = "sc";
				args = $"start {GetDaemonName(argument)}";
			}
			else if(OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
			{
				command = "systemctl";
				args = $"start {GetDaemonName(argument)}";
			}
			else
			{
				Zongsoft.Diagnostics.Logging.GetLogging().Error($"The {this.Name} launcher is not supported on {Environment.OSVersion} operating system.");
				return null;
			}

			var info = new ProcessStartInfo(command, args)
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = argument.AppPath,
			};

			return Process.Start(info);
		}
	}
}
