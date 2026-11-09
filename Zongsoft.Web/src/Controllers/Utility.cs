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
using System.IO;

using Zongsoft.Services;

namespace Zongsoft.Web.Controllers;

internal static class Utility
{
	private static string _root;
	private static string RootDirectory => _root ??= ApplicationContext.Current.Environment is IWebEnvironment environment ? environment.RootDirectory : "wwwroot";
	public static string GetPhysicalPath(string path) => string.IsNullOrEmpty(path) ? Path.Combine(ApplicationContext.Current.ApplicationPath, RootDirectory) :
		Path.Combine(ApplicationContext.Current.ApplicationPath, RootDirectory, path.Trim().TrimStart('/', '\\'));
}
