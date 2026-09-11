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
using System.Text;

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

			var index = identifier.LastIndexOf(':');
			if(index > 0)
				return (identifier[0..index].Trim().ToString(), identifier[(index + 1)..].Trim().ToString());

			index = identifier.LastIndexOf('-');
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

	/// <summary>从指定目录中的版本文件或指定文件中加载应用标识信息。</summary>
	/// <param name="path">指定的目录或文件路径；为空(<c>null</c>)或空串时默认为当前应用的根目录。路径为现有目录时读取其中的 <c>.version</c> 文件，否则读取指定的现有文件。</param>
	/// <returns>返回加载完成的应用标识；文件不存在、超过 16 KiB 或没有非空行时返回空标识。</returns>
	/// <remarks>读取文件中的第一个非空行，并按 <see cref="Parse(ReadOnlySpan{char})"/> 的规则解析。</remarks>
	public static ApplicationIdentifier Load(string path = null)
	{
		if(string.IsNullOrEmpty(path))
			path = AppContext.BaseDirectory;

		if(Directory.Exists(path))
			path = Path.Combine(path, FILE_NAME);

		//定义版本文件信息
		var info = new FileInfo(path);

		//如果文件不存在或者文件大小超过指定大小，则认为该文件无效
		if(!info.Exists || info.Length > 1024 * 16)
			return default;

		using var reader = info.OpenText();
		return Load(reader);
	}

	/// <summary>从指定流加载应用标识信息。</summary>
	/// <param name="stream">可读取的流，从当前位置开始读取，不要求支持定位。</param>
	/// <returns>第一个非空行对应的应用标识；没有非空行时返回空标识。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="stream"/> 不支持读取。</exception>
	/// <exception cref="FormatException">第一个非空行不是有效的应用标识。</exception>
	/// <remarks>
	///		<para>默认按 UTF-8 解码，并通过 BOM 检测编码；此重载不检查流的大小。无论成功或失败均不关闭传入的流，由调用方负责释放；读取异常直接向调用方传播。</para>
	///		<para>内部读取器可能预读后续内容，使底层流位置超过被解析的行。需要连续读取多行时，应复用读取器并调用 <see cref="Load(TextReader)"/>。</para>
	/// </remarks>
	public static ApplicationIdentifier Load(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
		return Load(reader);
	}

	/// <summary>从指定文本读取器加载应用标识信息。</summary>
	/// <param name="reader">文本读取器，从当前位置开始读取。</param>
	/// <returns>第一个非空行对应的应用标识；没有非空行时返回空标识。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="reader"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="FormatException">第一个非空行不是有效的应用标识。</exception>
	/// <remarks>跳过空白行，仅解析第一个非空行并移除其两端的空白，其余行留给调用方读取。编码由读取器决定，无论成功或失败均不关闭读取器；读取异常直接向调用方传播。</remarks>
	public static ApplicationIdentifier Load(TextReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);
		string text;

		while((text = reader.ReadLine()) != null)
		{
			if(string.IsNullOrWhiteSpace(text))
				continue;

			return Parse(text.AsSpan().Trim());
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
	/// <param name="path">指定的目录或文件路径；为空(<c>null</c>)或空串时默认为当前应用的根目录。路径为现有目录时保存到其中的 <c>.version</c> 文件，否则保存到指定的现有文件。</param>
	/// <param name="name">指定要保存的应用名称。</param>
	/// <param name="edition">指定要保存的应用版本名。</param>
	/// <param name="version">指定要保存的应用版本号。</param>
	/// <returns>如果保存成功则返回保存文件的完整路径；标识为空或指定路径既不是现有目录也不是现有文件时返回空(<c>null</c>)。</returns>
	public static string Save(string path, string name, string edition, Version version)
	{
		var identifier = new ApplicationIdentifier(name, edition, version);
		if(identifier.IsEmpty)
			return null;

		if(string.IsNullOrEmpty(path))
			path = AppContext.BaseDirectory;

		if(Directory.Exists(path))
			path = Path.Combine(path, FILE_NAME);
		else if(!File.Exists(path))
			return null;

		using var stream = File.OpenWrite(path);
		identifier.Save(stream);
		return stream.Name;
	}

	/// <summary>将当前应用标识信息保存到指定流。</summary>
	/// <param name="stream">可写入的流，从当前位置写入，不重置位置或截断内容。</param>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException">标识非空时，<paramref name="stream"/> 不支持写入。</exception>
	/// <remarks>按 <see cref="ToString()"/> 的格式输出 UTF-8 无 BOM 文本，不附加换行，完成后刷新缓冲区。空标识不写入或刷新流；流不必支持定位。无论成功或失败均不关闭传入的流，由调用方负责释放；写入和刷新异常直接向调用方传播。</remarks>
	public void Save(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		if(this.IsEmpty)
			return;

		using var writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
		this.Save(writer);
	}

	/// <summary>将当前应用标识信息保存到指定文本写入器。</summary>
	/// <param name="writer">接收应用标识信息的文本写入器，编码由写入器决定。</param>
	/// <exception cref="ArgumentNullException"><paramref name="writer"/> 为 <see langword="null"/>。</exception>
	/// <remarks>按 <see cref="ToString()"/> 的格式输出文本，不附加换行，也不改变 <see cref="TextWriter.NewLine"/>；完成后调用 <see cref="TextWriter.Flush()"/>。空标识不写入或刷新写入器。无论成功或失败均不关闭写入器，由调用方负责释放；写入和刷新异常直接向调用方传播。</remarks>
	public void Save(TextWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		if(this.IsEmpty)
			return;

		writer.Write(this.ToString());
		writer.Flush();
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
			return string.IsNullOrEmpty(edition) ? $"{name}" : $"{name}-{edition}";
		else
			return string.IsNullOrEmpty(edition) ? $"{name}@{version}" : $"{name}-{edition}@{version}";
	}
	#endregion
}
