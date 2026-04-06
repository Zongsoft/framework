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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Zongsoft.Services;

namespace Zongsoft.Upgrading;

public static class Utility
{
	static Utility()
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

	/// <summary>获取当前系统平台。</summary>
	public static readonly Platform Platform;
	/// <summary>获取当前体系架构。</summary>
	public static readonly Architecture Architecture;
	/// <summary>获取当前运行标识，即系统平台与体系架构组合。如：<c>win-x64</c>、<c>linux-x64</c>、<c>linux-arm64</c>。</summary>
	public static readonly string RuntimeIdentifier;
	/// <summary>获取当前应用程序名称。</summary>
	public static string ApplicationName => field ??= ApplicationContext.Current?.Name ?? Assembly.GetEntryAssembly().GetName().Name;
	public static string ApplicationPath => field ??= ApplicationContext.Current?.ApplicationPath ?? AppContext.BaseDirectory;
	/// <summary>获取当前应用程序版本。</summary>
	public static Version ApplicationVersion => field ??= ApplicationContext.Current?.Version ?? GetVersion() ?? Assembly.GetExecutingAssembly().GetName().Version;

	/// <summary>判断指定的版本号是否为零。</summary>
	/// <param name="version">指定的版本。</param>
	/// <returns>如果版本号为零则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool IsZero(this Version version) => version == null ||
	(
		version.Major == 0 &&
		version.Minor == 0 &&
		version.Build == 0 &&
		version.Revision == 0
	);

	public static string GetRuntimeIdentifier(this Release release) => release == null ? null : GetRuntimeIdentifier(release.Platform, release.Architecture);
	public static string GetRuntimeIdentifier(Platform platform, Architecture architecture) => platform == Platform.Windows ?
		(architecture == Architecture.Other ? "win" : $"win-{architecture.ToString().ToLowerInvariant()}"):
		(architecture == Architecture.Other ? platform.ToString().ToLowerInvariant() : $"{platform.ToString().ToLowerInvariant}-{architecture.ToString().ToLowerInvariant()}");

	private static Version GetVersion() => GetVersion(ApplicationName, ApplicationPath);
	private static Version GetVersion(string name, string path)
	{
		if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
			return null;

		var version = GetVersion(Path.Combine(path, ".version"), out var appname);
		return version != null && (string.IsNullOrEmpty(appname) || string.Equals(name, appname, StringComparison.OrdinalIgnoreCase)) ? version : null;
	}

	internal static Version GetVersion(string path, out string name)
	{
		if(string.IsNullOrEmpty(path))
		{
			name = null;
			return null;
		}

		//定义版本文件信息
		var info = new FileInfo(path);

		//如果文件不存在或者文件大小超过指定大小，则认为该文件无效
		if(!info.Exists || info.Length > 1024 * 10)
		{
			name = null;
			return null;
		}

		using var reader = info.OpenText();
		return GetVersion(reader, out name);
	}

	internal static Version GetVersion(StreamReader reader, out string name)
	{
		string text;

		while((text = reader.ReadLine()) != null)
		{
			if(string.IsNullOrEmpty(text))
				continue;

			var index = text.LastIndexOf('@');

			if(index < 0)
			{
				name = null;
				return Version.TryParse(text, out var version) ? version : null;
			}
			else
			{
				name = text[..index];
				return Version.TryParse(text.AsSpan()[(index + 1)..], out var version) ? version : null;
			}
		}

		name = null;
		return null;
	}
}
