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
	private sealed class UniversalLauncher() : Launcher(NAME)
	{
		public const string NAME = "";

		protected override Process OnLaunch(Deployer.Argument argument)
		{
			var info = new ProcessStartInfo(argument.HostPath, argument.HostArgs)
			{
				CreateNoWindow = false,
				UseShellExecute = false,
				WorkingDirectory = argument.AppPath,
			};

			//启动升级部署器程序
			return Process.Start(info);
		}
	}
}
