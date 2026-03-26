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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Zongsoft.Externals.Velopack;

public class ApplicationManifest
{
	private static readonly Lazy<ApplicationManifest> _current = new(() => new(), true);
	public static ApplicationManifest Current => _current.Value;

	private ApplicationManifest()
	{
		var assembly = Assembly.GetEntryAssembly();
		if(assembly == null)
			return;

		this.Name = assembly.GetName().Name;
		this.Version = assembly.GetName().Version;
		this.Framework = RuntimeInformation.FrameworkDescription;
		this.Architecture = RuntimeInformation.OSArchitecture.ToString();
		this.Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
		this.Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
		this.Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
		this.Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
		this.Trademark = assembly.GetCustomAttribute<AssemblyTrademarkAttribute>()?.Trademark;
		this.Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
	}

	public string Name { get; }
	public string Title { get; }
	public Version Version { get; }
	public string Architecture { get; }
	public string Framework { get; }
	public string Company { get; }
	public string Product { get; }
	public string Copyright { get; }
	public string Trademark { get; }
	public string Description { get; }
}
