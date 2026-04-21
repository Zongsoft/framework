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

/// <summary>表示应用标识的结构。</summary>
public readonly struct ApplicationIdentifier
{
	#region 常量定义
	/// <summary>版本文件的名称。</summary>
	private const string FILE_NAME = ".version";
	#endregion

	#region 构造函数
	public ApplicationIdentifier(string name, Version version = null) : this(name, null, version) { }
	public ApplicationIdentifier(string name, string edition, Version version = null)
	{
		this.Name = name;
		this.Edition = edition;
		this.Version = version;
	}
	#endregion

	#region 公共属性
	/// <summary>获取标识名称。</summary>
	public string Name { get; }
	/// <summary>获取版本标识。</summary>
	public string Edition { get; }
	/// <summary>获取版本号码。</summary>
	public Version Version { get; }
	/// <summary>获取一个值，指示本标识是否为空。</summary>
	public bool IsEmpty => string.IsNullOrEmpty(this.Name) && this.Version == null;
	#endregion

	#region 公共方法
	/// <summary>将指定的字符串解析为应用标识。</summary>
	/// <param name="text">指定待解析的字符串，如果为空(<c>null</c>)或空串则返回一个空的应用标识。</param>
	/// <returns>返回解析成功后的应用标识。</returns>
	/// <exception cref="FormatException">当指定的 <paramref name="text"/> 参数不是一个有效的应用标识文本。</exception>
	public static ApplicationIdentifier Parse(ReadOnlySpan<char> text) => text.IsEmpty || text.IsWhiteSpace() ? default :
		TryParse(text, out var result) ? result : throw new FormatException($"The specified '{text}' is an invalid application identifier format.");

	/// <summary>尝试将指定的字符串解析为应用标识。</summary>
	/// <param name="text">指定待解析的字符串，如果为空或空串则返回失败。</param>
	/// <param name="result">解析成功后的应用标识。</param>
	/// <returns>如果解析成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, out ApplicationIdentifier result)
	{
		if(text.IsEmpty || text.IsWhiteSpace())
		{
			result = default;
			return false;
		}

		Version version;
		var index = text.LastIndexOf('@');

		if(index < 0)
		{
			if(Version.TryParse(text, out version))
				result = new(null, null, version);
			else
			{
				var (name, edition) = Resolve(text);
				result = new(name, edition, null);
			}

			return true;
		}

		if(Version.TryParse(text[(index + 1)..], out version))
		{
			var (name, edition) = Resolve(text[..index]);
			result = new(name, edition, version);
			return true;
		}

		result = default;
		return false;

		static (string name, string edition) Resolve(ReadOnlySpan<char> identifier)
		{
			if(identifier.IsEmpty)
				return default;

			var index = identifier.IndexOf(':');
			if(index > 0)
				return (identifier[0..index].Trim().ToString(), identifier[(index + 1)..].Trim().ToString());

			index = identifier.IndexOf('(');
			if(index > 0)
			{
				var final = identifier.LastIndexOf(')');

				return final > index ?
					(identifier[..index].Trim().ToString(), identifier[(index + 1)..final].Trim().ToString()) :
					(identifier[..index].Trim().ToString(), identifier[(index + 1)..].Trim().ToString());
			}

			return (identifier.Trim().ToString(), null);
		}
	}

	/// <summary>从指定的目录中的版本文件中加载应用标识信息。</summary>
	/// <param name="directory">指定的目录路径。</param>
	/// <returns>返回加载完成的应用标识。</returns>
	public static ApplicationIdentifier Load(string directory = null)
	{
		if(string.IsNullOrEmpty(directory))
			directory = AppContext.BaseDirectory;

		//定义版本文件信息
		var info = new FileInfo(Path.Combine(directory, FILE_NAME));

		//如果文件不存在或者文件大小超过指定大小，则认为该文件无效
		if(!info.Exists || info.Length > 1024 * 10)
			return default;

		string text;
		using var reader = info.OpenText();

		while((text = reader.ReadLine()) != null)
		{
			if(string.IsNullOrWhiteSpace(text))
				continue;

			return Parse(text.Trim());
		}

		return default;
	}

	/// <summary>从指定应用所在路径的版本文件中加载应用标识信息。</summary>
	/// <param name="module">指定的应用。</param>
	/// <returns>返回加载完成的应用标识。</returns>
	public static ApplicationIdentifier Load(IApplicationModule module)
	{
		if(module == null || module.Assembly == null)
			return default;

		var identifier = Load(Path.GetDirectoryName(module.Assembly.Location));
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

	/// <summary>将指定的应用标识信息保存到文件中。</summary>
	/// <param name="directory">指定要保存的目标目录，如果为空(<c>null</c>)则默认为当前应用的根目录。</param>
	/// <param name="name">指定要保存的应用名称。</param>
	/// <param name="edition">指定要保存的应用版本名。</param>
	/// <param name="version">指定要保存的应用版本号。</param>
	/// <returns>如果保存成功则返回保存文件的完整路径，否则返回空(<c>null</c>)。</returns>
	public static string Save(string directory, string name, string edition, Version version)
	{
		var identifier = ToString(name, edition, version);
		if(string.IsNullOrEmpty(identifier))
			return null;

		if(string.IsNullOrEmpty(directory))
			directory = AppContext.BaseDirectory;

		if(!Directory.Exists(directory))
			return null;

		using var writer = File.OpenWrite(Path.Combine(directory, FILE_NAME));
		writer.Write(System.Text.Encoding.UTF8.GetBytes(identifier));
		return writer.Name;
	}

	/// <summary>将当前应用标识信息保存到文件中。</summary>
	/// <param name="module">指定要保存的应用，如果为空(<c>null</c>)则表示当前宿主应用程序。</param>
	/// <returns>如果保存成功则返回保存文件的完整路径，否则返回空(<c>null</c>)。</returns>
	public string Save(IApplicationModule module = null) => module == null || module.Assembly == null ?
		Save(null, this.Name, this.Edition, this.Version) :
		Save(Path.GetDirectoryName(module.Assembly.Location), this.Name, this.Edition, this.Version);
	#endregion

	#region 重写方法
	public override string ToString() => ToString(this.Name, this.Edition, this.Version) ?? string.Empty;
	#endregion

	#region 私有方法
	static string ToString(string name, string edition, Version version)
	{
		if(string.IsNullOrEmpty(name))
			return version?.ToString();

		if(version == null)
			return string.IsNullOrEmpty(edition) ? $"{name}" : $"{name}({edition})";
		else
			return string.IsNullOrEmpty(edition) ? $"{name}@{version}" : $"{name}({edition})@{version}";
	}
	#endregion
}
