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
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

public static partial class Deployer
{
	public static Version Version => field ??= Assembly.GetEntryAssembly().GetName().Version;

	public static void Deploy(Dictionary<string, string> parameters)
	{
		var argument = new Argument(parameters);

		//等待宿主程序退出
		if(!WaitHostExit(argument))
			return;
	}

	private static Manifest GetManifest(Argument argument, out DirectoryInfo directory)
	{
		directory = null;

		if(argument.TryGetValue("bootstrap", out var bootstrap))
		{
			if(!File.Exists(bootstrap))
				return null;

			try
			{
				directory = new DirectoryInfo(Path.GetDirectoryName(bootstrap));
				File.Open(bootstrap, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch
			{
				return null;
			}
		}

		return null;
	}

	private static bool WaitHostExit(Argument argument, TimeSpan timeout = default)
	{
		if(argument.TryGetInt32("process", out var id))
		{
			//获取指定编号的进程
			var process = GetProcess(id);

			//如果进程已退出则返回成功
			if(HasExited(process))
				return true;

			try
			{
				process.EnableRaisingEvents = true;

				if(timeout > TimeSpan.Zero)
					return process.WaitForExit(timeout);
				else
					return process.WaitForExit(TimeSpan.FromMinutes(1));
			}
			catch { return false; }
		}

		return false;

		static Process GetProcess(int id)
		{
			try { return id == 0 ? null : Process.GetProcessById(id); }
			catch { return null; }
		}

		static bool HasExited(Process process)
		{
			try { return process == null || process.HasExited; }
			catch { return false; }
		}
	}
}
