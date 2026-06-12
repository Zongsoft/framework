/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * The MIT License (MIT)
 * 
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Diagnostics;

namespace Zongsoft.Upgrading;

partial class Launcher
{
	private sealed class WebLauncher() : Launcher(NAME)
	{
		public const string NAME = "Web";

		protected override Process OnLaunch(Deployer.Argument argument)
		{
			const string IIS_APP = "iis.app";

			if(OperatingSystem.IsWindows())
				return argument.TryGetValue(IIS_APP, out var apppool) ? LaunchIIS(argument, apppool) : Launch(argument);

			if(OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
				return Launch("systemctl", $"start {GetDaemonName(argument)}", argument.AppPath);

			Zongsoft.Diagnostics.Logging.GetLogging().Error($"The {this.Name} launcher is not supported on {Environment.OSVersion} operating system.");
			return null;
		}

		private static Process Launch(Deployer.Argument argument)
		{
			var info = new ProcessStartInfo(argument.HostPath, argument.HostArgs)
			{
				CreateNoWindow = false,
				UseShellExecute = false,
				WorkingDirectory = argument.AppPath,
			};

			return Process.Start(info);
		}

		private static Process Launch(string command, string args, string workingDirectory)
		{
			var info = new ProcessStartInfo(command, args)
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = workingDirectory,
			};

			return Process.Start(info);
		}

		private static Process LaunchIIS(Deployer.Argument argument, string apppool)
		{
			if(string.IsNullOrWhiteSpace(apppool) || apppool == "." || apppool == "~")
				apppool = argument.AppName;

			var command = Environment.ExpandEnvironmentVariables(@"%windir%\system32\inetsrv\appcmd.exe");
			return Launch(command, $"recycle apppool {apppool}", argument.AppPath);
		}
	}
}
