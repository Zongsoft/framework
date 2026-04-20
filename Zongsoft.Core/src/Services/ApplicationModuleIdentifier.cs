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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace Zongsoft.Services;

/// <summary>表示应用模块标识的结构。</summary>
public readonly struct ApplicationModuleIdentifier
{
	#region 常量定义
	/// <summary>版本文件的名称。</summary>
	public const string FILE_NAME = ".version";
	#endregion

	#region 构造函数
	public ApplicationModuleIdentifier(string name, string edition, Version version = null)
	{
		this.Name = name;
		this.Edition = edition;
		this.Version = version;
	}
	#endregion

	#region 公共属性
	/// <summary>获取应用模块名称。</summary>
	public string Name { get; }
	/// <summary>获取应用模块版本标识。</summary>
	public string Edition { get; }
	/// <summary>获取应用模块版本号码。</summary>
	public Version Version { get; }
	/// <summary>获取一个值，指示本应用模块标识是否为空。</summary>
	public bool IsEmpty => string.IsNullOrEmpty(this.Name) && this.Version == null;
	#endregion

	#region 公共方法
	/// <summary>从指定应用模块所在路径的版本文件中获取应用标识信息。</summary>
	/// <param name="module">指定的应用模块。</param>
	/// <returns>返回解析完成的标识结构。</returns>
	public static ApplicationModuleIdentifier Load(IApplicationModule module)
	{
		if(module == null || module.Assembly == null)
			return default;

		var identifier = FromFile(Path.GetDirectoryName(module.Assembly.Location));
		if(identifier.IsEmpty)
			return new(module.Name, null, module.Assembly.GetName().Version);

		if(string.IsNullOrEmpty(identifier.Name))
			return identifier.Version == null ?
				new(module.Name, identifier.Edition, module.Assembly.GetName().Version):
				new(module.Name, identifier.Edition, identifier.Version);
		else
			return identifier.Version == null ?
				new(identifier.Name, identifier.Edition, module.Assembly.GetName().Version) :
				new(identifier.Name, identifier.Edition, identifier.Version);
	}

	/// <summary>将指定的应用模块标识信息写入到文件中。</summary>
	/// <param name="directory">指定要写入的目标目录，如果为空(<c>null</c>)则默认为当前应用的根目录。</param>
	/// <param name="name">指定要写入的应用模块名称。</param>
	/// <param name="edition">指定要写入的应用模块版本名。</param>
	/// <param name="version">指定要写入的应用模块版本号。</param>
	/// <returns>如果写入成功则返回写入文件的完整路径，否则返回空(<c>null</c>)。</returns>
	public static string Save(string directory, string name, string edition, Version version)
	{
		if(string.IsNullOrEmpty(directory))
			directory = AppContext.BaseDirectory;

		if(!Directory.Exists(directory))
			return null;

		if(string.IsNullOrEmpty(name) && version == null)
			return null;

		using var writer = File.OpenWrite(Path.Combine(directory, FILE_NAME));

		if(string.IsNullOrEmpty(name))
			writer.Write(System.Text.Encoding.UTF8.GetBytes($"{version}"));
		else if(string.IsNullOrEmpty(edition))
			writer.Write(System.Text.Encoding.UTF8.GetBytes($"{name}{version}"));
		else
			writer.Write(System.Text.Encoding.UTF8.GetBytes($"{name}({edition}){version}"));

		return writer.Name;
	}

	/// <summary>将当前应用模块标识信息写入到文件中。</summary>
	/// <param name="module">指定要写入的应用模块，如果为空(<c>null</c>)则表示当前宿主应用程序。</param>
	/// <returns>如果写入成功则返回写入文件的完整路径，否则返回空(<c>null</c>)。</returns>
	public string Save(IApplicationModule module = null) => module == null || module.Assembly == null ?
		Save(null, this.Name, this.Edition, this.Version) :
		Save(Path.GetDirectoryName(module.Assembly.Location), this.Name, this.Edition, this.Version);
	#endregion

	#region 私有方法
	static ApplicationModuleIdentifier FromFile(string directory)
	{
		if(string.IsNullOrEmpty(directory))
			return default;

		//定义版本文件信息
		var info = new FileInfo(Path.Combine(directory, FILE_NAME));

		//如果文件不存在或者文件大小超过指定大小，则认为该文件无效
		if(!info.Exists || info.Length > 1024 * 10)
			return default;

		string text;
		Version version;
		using var reader = info.OpenText();

		while((text = reader.ReadLine()) != null)
		{
			if(string.IsNullOrWhiteSpace(text))
				continue;

			var index = text.LastIndexOf('@');

			if(index < 0)
			{
				if(Version.TryParse(text, out version))
					return new(null, null, version);

				var (name, edition) = Resolve(text);
				return new(name, edition, null);
			}

			if(Version.TryParse(text.AsSpan()[(index + 1)..], out version))
			{
				var (name, edition) = Resolve(text[..index]);
				return new(name, edition, version);
			}
			else
			{
				var (name, edition) = Resolve(text[..index]);
				return new(name, edition, null);
			}
		}

		return default;
	}

	static (string name, string edition) Resolve(string module)
	{
		if(string.IsNullOrEmpty(module))
			return default;

		var index = module.IndexOf(':');
		if(index > 0)
			return (module[0..index], module[(index + 1)..]);

		index = module.IndexOf('(');
		if(index > 0)
		{
			var final = module.IndexOf(')', index);

			return final > 0 ?
				(module[..index], module[(index + 1)..final]) :
				(module[..index], module[(index + 1)..]);
		}

		return (module, null);
	}
	#endregion
}
