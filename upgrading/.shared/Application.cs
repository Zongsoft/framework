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
using System.Reflection;
using System.Runtime.InteropServices;

using Zongsoft.Services;

namespace Zongsoft.Upgrading;

public static class Application
{
	#region 静态构造
	static Application()
	{
		Platform = GetPlatform();
		Architecture = RuntimeInformation.OSArchitecture switch
		{
			System.Runtime.InteropServices.Architecture.X86 => Architecture.X86,
			System.Runtime.InteropServices.Architecture.X64 => Architecture.X64,
			System.Runtime.InteropServices.Architecture.Arm => Architecture.Arm32,
			System.Runtime.InteropServices.Architecture.Arm64 => Architecture.Arm64,
			System.Runtime.InteropServices.Architecture.Wasm => Architecture.Wasm,
			System.Runtime.InteropServices.Architecture.LoongArch64 => Architecture.Loong,
			_ => Architecture.Other,
		};

		RuntimeIdentifier = GetRuntimeIdentifier(Platform, Architecture);

		static Platform GetPlatform()
		{
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return Platform.Windows;
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return Platform.Linux;
			if(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
				return Platform.Unix;
			if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return Platform.OSX;

			return Platform.Unknown;
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前系统平台。</summary>
	public static readonly Platform Platform;
	/// <summary>获取当前体系架构。</summary>
	public static readonly Architecture Architecture;
	/// <summary>获取当前运行标识，即系统平台与体系架构组合。如：<c>win-x64</c>、<c>linux-x64</c>、<c>linux-arm64</c>。</summary>
	public static readonly string RuntimeIdentifier;
	/// <summary>获取当前应用的站点标识。</summary>
	public static string Site => field ??= ApplicationContext.Current?.Configuration.GetSection("Site")?.Value;
	/// <summary>获取当前应用程序名称。</summary>
	public static string ApplicationName => field ??= ApplicationContext.Current?.Name ?? Assembly.GetEntryAssembly().GetName().Name;
	/// <summary>获取当前应用程序类型。</summary>
	public static string ApplicationType => field ??= ApplicationContext.Current?.ApplicationType ?? (IsWebApplication() ? "Web" : string.Empty);
	/// <summary>获取当前应用程序根目录。</summary>
	public static string ApplicationPath => field ??= ApplicationContext.Current?.ApplicationPath ?? AppContext.BaseDirectory;
	/// <summary>获取当前应用程序版本名。</summary>
	public static string ApplicationEdition => field ??= ApplicationContext.Current?.Edition ?? GetIdentifier().Edition;
	/// <summary>获取当前应用程序版本号。</summary>
	public static Version ApplicationVersion => field ??= ApplicationContext.Current?.Version ?? GetIdentifier().Version ?? Assembly.GetExecutingAssembly().GetName().Version;
	#endregion

	#region 公共方法
	public static string GetRuntimeIdentifier(this Release release) => release == null ? null : GetRuntimeIdentifier(release.Platform, release.Architecture);
	public static string GetRuntimeIdentifier(Platform platform, Architecture architecture) => platform == Platform.Windows ?
		(architecture == Architecture.Other ? "win" : $"win-{architecture.ToString().ToLowerInvariant()}"):
		(architecture == Architecture.Other ? platform.ToString().ToLowerInvariant() : $"{platform.ToString().ToLowerInvariant}-{architecture.ToString().ToLowerInvariant()}");
	#endregion

	#region 私有方法
	private static ApplicationIdentifier GetIdentifier() => GetIdentifier(ApplicationName, ApplicationPath);
	private static ApplicationIdentifier GetIdentifier(string name, string path)
	{
		if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
			return default;

		var identifier = ApplicationIdentifier.Load(path);
		return !identifier.IsEmpty && (string.IsNullOrEmpty(identifier.Name) || string.Equals(name, identifier.Name, StringComparison.OrdinalIgnoreCase)) ? identifier : default;
	}

	private static bool IsWebApplication()
	{
		var names = Assembly.GetEntryAssembly()?.GetReferencedAssemblies();
		if(names == null || names.Length == 0)
			return false;

		for(int i = 0; i < names.Length; i++)
		{
			if(names[i].Name.StartsWith("Microsoft.AspNetCore"))
				return true;
		}

		return false;
	}
	#endregion
}
