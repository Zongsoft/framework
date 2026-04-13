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
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

/// <summary>提供宿主应用程序启动功能。</summary>
public static partial class Launcher
{
	#region 私有字段
	private static readonly Dictionary<string, ILauncher> _launchers;
	#endregion

	#region 静态构造
	static Launcher()
	{
		_launchers = new(StringComparer.OrdinalIgnoreCase);

		foreach(var type in typeof(Launcher).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
		{
			if(typeof(ILauncher).IsAssignableFrom(type))
			{
				var launcher = (ILauncher)Activator.CreateInstance(type);
				_launchers.Add(launcher.Name, launcher);
			}
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取默认启动器。</summary>
	public static ILauncher Default => _launchers.TryGetValue(string.Empty, out var launcher) ? launcher : null;
	#endregion

	#region 公共方法
	public static void Launch(Deployer.Deployment deployment, Deployer.Argument argument)
	{
		if(argument == null || deployment == null)
			return;

		try
		{
			//根据宿主应用类型获取对应的启动器
			if(_launchers.TryGetValue(argument.AppType ?? string.Empty, out var launcher))
				launcher.Launch(argument);
			else
				Default.Launch(argument);

			//释放部署文件(解除排他性锁)
			deployment.Dispose();

			//删除部署文件
			if(File.Exists(argument.Deployment))
				File.Delete(argument.Deployment);
		}
		catch(Exception ex)
		{
			Zongsoft.Diagnostics.Logging.GetLogging<Program>().Error(ex);
		}
	}
	#endregion
}
