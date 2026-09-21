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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Services;

/// <summary>表示应用 <c>.version</c> 文件中的名称、版本号和版本集，并提供文件、流及文本读写器的加载和保存功能。</summary>
/// <remarks>
///		<para><c>.version</c> 是采用 INI 段落形式的文本文件，支持以下两种互斥的格式：</para>
///		<list type="bullet">
///			<item><description>不区分版本名时，首个有效行采用 <c>name@version</c> 格式，分别对应 <see cref="Name"/> 和 <see cref="Version"/>，<see cref="Editions"/> 为空；其后只能包含空行或整行注释。</description></item>
///			<item><description>区分版本名时，首个有效行只能包含应用名称，不能附加 <c>@version</c>。随后每个 <c>[edition]</c> 段落包含且仅包含一个裸版本号，分别对应 <see cref="Edition.Name"/> 和 <see cref="Edition.Version"/>；顶层 <see cref="Version"/> 为 <see langword="null"/>。</description></item>
///		</list>
///		<para>版本号采用 <see cref="System.Version"/> 支持的两段、三段或四段数字格式。版本名不区分大小写且必须唯一，段落顺序保留在 <see cref="Editions"/> 中；文件不指定当前或默认版本名。</para>
///		<para>读取时忽略空行、行及名称两端的空白，以及以 <c>;</c> 或 <c>#</c> 开始的整行注释；接受文件开头的 BOM 和 CRLF、LF、CR 换行。不支持 <c>Version=1.0.1</c> 这样的键值项、嵌套段落或行尾注释。名称中的非行首注释符作为普通字符处理。</para>
///		<para>应用名称和版本名均不能为空，且不能包含换行、方括号、空字符或 BOM；应用名称另不能包含 <c>@</c>，也不能以 <c>;</c> 或 <c>#</c> 开始。</para>
///		<para>保存到文件或流时输出 UTF-8 无 BOM 文本；保存到文本写入器时编码由写入器决定。所有保存方式均使用 CRLF 换行，文件末尾保留换行；应用名称与首个段落之间、段落之间保留一个空行，不保留原文件的注释或空白布局。</para>
///		<para>传入的流和文本读写器由调用方负责释放，加载或保存不会关闭它们。加载从当前位置读取至结尾，保存从当前位置写入；流重载不重置位置或截断内容，也不要求支持定位。</para>
///		<para>顶层版本号与非空的具名版本集互斥，切换表示方式前须先清空原有表示。允许暂时不设置任何版本信息，但保存时必须具有顶层版本号或至少一个具名版本。本类型及其版本集不保证线程安全。</para>
/// </remarks>
/// <example>
///		<para>不区分版本名的 <c>.version</c> 文件：</para>
///		<code language="ini">MyApplicationName@1.0.1</code>
///		<para>包含多个具名版本的 <c>.version</c> 文件：</para>
///		<code language="ini">
///			MyApplicationName
///
///			[Community]
///			1.0.1
///
///			[Professional]
///			1.1.0
///
///			[Enterprise]
///			1.1.2
///		</code>
/// </example>
public class ApplicationVersion
{
	#region 常量定义
	private const string FILE_NAME = ".version";
	#endregion

	#region 成员字段
	private Version _version;
	#endregion

	#region 构造函数
	/// <summary>初始化应用版本信息；未指定版本号时，可随后添加具名版本。</summary>
	/// <param name="name">应用名称，自动移除两端的空白。</param>
	/// <param name="version">不区分版本名时的版本号；为 <see langword="null"/> 时，可通过 <see cref="Editions"/> 添加具名版本。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="name"/> 为空白或包含文件格式的保留字符。</exception>
	public ApplicationVersion(string name, Version version = null)
	{
		this.Name = ValidateName(name, true);
		this.Editions = new EditionCollection(this);
		_version = version;
	}
	#endregion

	#region 公共属性
	/// <summary>获取应用名称。</summary>
	public string Name { get; }
	/// <summary>获取或设置不区分版本名时的版本号。</summary>
	/// <value>单行格式中 <c>@</c> 后的版本号；包含具名版本时为 <see langword="null"/>。</value>
	/// <exception cref="InvalidOperationException">版本集非空时设置非空版本号。</exception>
	public Version Version
	{
		get => _version;
		set
		{
			if(value != null && !this.Editions.IsEmpty)
				throw new InvalidOperationException(Properties.Resources.Services_ApplicationVersion_Conflict_Message);

			_version = value;
		}
	}

	/// <summary>获取按添加顺序排列的具名版本集。</summary>
	/// <remarks>每个条目对应文件中的一个版本段落；不区分版本名时此集合为空。</remarks>
	public EditionCollection Editions { get; }
	#endregion

	#region 公共方法
	/// <summary>从指定目录中的版本文件或指定文件加载应用版本信息。</summary>
	/// <param name="path">指定的目录或文件路径；为空(<c>null</c>)或空串时默认为当前应用的根目录。路径为现有目录时读取其中的 <c>.version</c> 文件，否则读取指定的现有文件。</param>
	/// <returns>从文件中读取的应用版本信息；文件不存在时返回 <see langword="null"/>。</returns>
	/// <exception cref="FormatException">文件为空或内容不符合应用版本格式，异常信息包含行号。</exception>
	/// <remarks>文件格式及示例见 <see cref="ApplicationVersion"/>。缺少版本号、重复版本名或混用两种格式均视为格式错误；打开和读取文件时的文件系统异常直接向调用方传播。</remarks>
	public static ApplicationVersion Load(string path)
	{
		if(string.IsNullOrEmpty(path))
			path = AppContext.BaseDirectory;

		if(Directory.Exists(path))
			path = Path.Combine(path, FILE_NAME);

		if(!File.Exists(path))
			return null;

		using var reader = File.OpenText(path);
		return Load(reader);
	}

	/// <summary>从指定流加载应用版本信息。</summary>
	/// <param name="stream">可读取的流，从当前位置读取至结尾，不要求支持定位。</param>
	/// <returns>从流中读取的应用版本信息。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="stream"/> 不支持读取。</exception>
	/// <exception cref="FormatException">内容为空或不符合应用版本格式，异常信息包含行号。</exception>
	/// <remarks>默认按 UTF-8 解码，并通过 BOM 检测编码。无论成功或失败均不关闭传入的流，由调用方负责释放；读取异常直接向调用方传播。格式及示例见 <see cref="ApplicationVersion"/>。</remarks>
	public static ApplicationVersion Load(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
		return Load(reader);
	}

	/// <summary>从指定文本读取器加载应用版本信息。</summary>
	/// <param name="reader">文本读取器，从当前位置读取至结尾。</param>
	/// <returns>从读取器中读取的应用版本信息。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="reader"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="FormatException">内容为空或不符合应用版本格式，异常信息包含行号。</exception>
	/// <remarks>编码由读取器决定；无论成功或失败均不关闭读取器，由调用方负责释放。读取异常直接向调用方传播。格式及示例见 <see cref="ApplicationVersion"/>。</remarks>
	public static ApplicationVersion Load(TextReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);
		return Parse(reader.ReadToEnd().AsSpan());
	}

	/// <summary>将应用版本信息保存到指定目录中的版本文件或指定文件，覆盖原有内容。</summary>
	/// <param name="path">指定的目录或文件路径；为空(<c>null</c>)或空串时默认为当前应用的根目录。路径为现有目录时保存到其中的 <c>.version</c> 文件，否则保存到指定的现有文件。</param>
	/// <exception cref="InvalidOperationException">未设置顶层版本号且版本集为空。</exception>
	/// <remarks>
	/// <para>设置了 <see cref="Version"/> 时保存为 <c>name@version</c>；否则按 <see cref="Editions"/> 的顺序输出应用名称及版本段落，格式示例见 <see cref="ApplicationVersion"/>。</para>
	/// <para>首先检查版本信息是否完整；指定路径既不是现有目录也不是现有文件时不执行写入。现有目录中的 <c>.version</c> 文件可自动创建，现有目标文件会被截断，不自动创建父目录。输出 UTF-8 无 BOM 文本和 CRLF 换行，不保留原文件的注释和空白布局；打开和写入文件时的文件系统异常直接向调用方传播。</para>
	/// </remarks>
	public void Save(string path)
	{
		this.ValidateVersion();

		if(string.IsNullOrEmpty(path))
			path = AppContext.BaseDirectory;

		if(Directory.Exists(path))
			path = Path.Combine(path, FILE_NAME);
		else if(!File.Exists(path))
			return;

		using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
		this.Save(writer);
	}

	/// <summary>将应用版本信息保存到指定流。</summary>
	/// <param name="stream">可写入的流，从当前位置写入，不重置位置或截断内容。</param>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="stream"/> 不支持写入。</exception>
	/// <exception cref="InvalidOperationException">未设置顶层版本号且版本集为空。</exception>
	/// <remarks>写入前检查版本信息是否完整。输出 UTF-8 无 BOM 文本和 CRLF 换行，完成后刷新缓冲区；不要求流支持定位。无论成功或失败均不关闭传入的流，由调用方负责释放；写入和刷新异常直接向调用方传播。</remarks>
	public void Save(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		this.ValidateVersion();

		using var writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
		this.Save(writer);
	}

	/// <summary>将应用版本信息保存到指定文本写入器。</summary>
	/// <param name="writer">接收版本信息的文本写入器，编码由写入器决定。</param>
	/// <exception cref="ArgumentNullException"><paramref name="writer"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="InvalidOperationException">未设置顶层版本号且版本集为空。</exception>
	/// <remarks>写入前检查版本信息是否完整。始终输出 CRLF 换行，不改变写入器的 <see cref="TextWriter.NewLine"/> 属性；完成后调用 <see cref="TextWriter.Flush()"/>。无论成功或失败均不关闭写入器，由调用方负责释放；写入和刷新异常直接向调用方传播。</remarks>
	public void Save(TextWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		this.ValidateVersion();

		writer.Write(this.Name);

		//四个非负 Int32 分量以及三个分隔点最多占用 43 个字符。
		Span<char> buffer = stackalloc char[43];

		if(_version != null)
		{
			writer.Write('@');
			WriteVersion(writer, _version, buffer);
		}
		else
		{
			writer.Write("\r\n");

			foreach(var edition in this.Editions)
			{
				writer.Write("\r\n");
				writer.Write('[');
				writer.Write(edition.Name);
				writer.Write("]\r\n");
				WriteVersion(writer, edition.Version, buffer);
			}
		}

		writer.Flush();
	}
	#endregion

	#region 私有方法
	private void ValidateVersion()
	{
		if(_version == null && this.Editions.IsEmpty)
			throw new InvalidOperationException(Properties.Resources.Services_ApplicationVersion_VersionRequired_Message);
	}

	private static void WriteVersion(TextWriter writer, Version version, Span<char> buffer)
	{
		if(!version.TryFormat(buffer, out var count))
			throw new InvalidOperationException(Properties.Resources.Services_ApplicationVersion_FormattingBufferExceeded_Message);

		writer.Write(buffer[..count]);
		writer.Write("\r\n");
	}

	private static ApplicationVersion Parse(ReadOnlySpan<char> text)
	{
		if(!text.IsEmpty && text[0] == '\uFEFF')
			text = text[1..];

		ApplicationVersion application = null;
		string editionName = null;
		var editionLine = 0;
		var lineNumber = 0;

		while(!text.IsEmpty)
		{
			lineNumber++;
			var end = text.IndexOfAny('\r', '\n');
			var line = end < 0 ? text : text[..end];

			if(end < 0)
				text = default;
			else
			{
				var length = text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? 2 : 1;
				text = text[(end + length)..];
			}

			line = line.Trim();
			if(line.IsEmpty || line[0] is ';' or '#')
				continue;

			if(application == null)
			{
				var separator = line.IndexOf('@');
				var name = (separator < 0 ? line : line[..separator]).Trim();
				if(!IsValidName(name, true))
					throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_ApplicationNameInvalid_Message);

				Version version = null;
				if(separator >= 0 && !System.Version.TryParse(line[(separator + 1)..].Trim(), out version))
					throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_VersionInvalid_Message);

				application = new ApplicationVersion(name.ToString(), version);
				continue;
			}

			if(application.Version != null)
				throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_UnexpectedContent_Message);

			if(line[0] == '[')
			{
				if(editionName != null)
					throw InvalidFormat(editionLine, Properties.Resources.Services_ApplicationVersion_EditionVersionRequired_Message);

				if(line.Length < 3 || line[^1] != ']')
					throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_EditionHeaderInvalid_Message);

				var name = line[1..^1].Trim();
				if(!IsValidName(name, false))
					throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_EditionNameInvalid_Message);

				editionName = name.ToString();
				editionLine = lineNumber;
			}
			else
			{
				if(editionName == null)
					throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_EditionHeaderRequired_Message);

				if(!System.Version.TryParse(line, out var version))
					throw InvalidFormat(lineNumber, Properties.Resources.Services_ApplicationVersion_BareVersionRequired_Message);

				try
				{
					application.Editions.Add(new Edition(editionName, version));
				}
				catch(ArgumentException)
				{
					throw InvalidFormat(editionLine, Properties.Resources.Services_ApplicationVersion_EditionDuplicated_Message);
				}

				editionName = null;
			}
		}

		if(editionName != null)
			throw InvalidFormat(editionLine, Properties.Resources.Services_ApplicationVersion_EditionVersionRequired_Message);

		if(application == null || application.Version == null && application.Editions.IsEmpty)
			throw InvalidFormat(Math.Max(1, lineNumber), Properties.Resources.Services_ApplicationVersion_InformationRequired_Message);

		return application;
	}

	private static FormatException InvalidFormat(int lineNumber, string message) => new(string.Format(Properties.Resources.Services_ApplicationVersion_InvalidFormat_Message, lineNumber, message));

	private static string ValidateName(string name, bool application)
	{
		ArgumentNullException.ThrowIfNull(name);
		name = name.Trim();

		if(!IsValidName(name.AsSpan(), application))
			throw new ArgumentException(Properties.Resources.Services_ApplicationVersion_NameInvalid_Message, nameof(name));

		return name;
	}

	private static bool IsValidName(ReadOnlySpan<char> name, bool application)
	{
		if(name.IsEmpty || name.IndexOfAny('\r', '\n') >= 0 || name.IndexOfAny('[', ']') >= 0 || name.IndexOfAny('\0', '\uFEFF') >= 0)
			return false;

		return !application || name.IndexOf('@') < 0 && name[0] is not (';' or '#');
	}
	#endregion

	#region 嵌套结构
	/// <summary>表示一个具名应用版本。</summary>
	/// <remarks>对应 <c>.version</c> 文件中的一个段落：段落标题为 <see cref="Name"/>，段落内的裸版本号为 <see cref="Version"/>。</remarks>
	public readonly struct Edition
	{
		/// <summary>初始化具有非空名称和版本号的具名版本。</summary>
		/// <param name="name">段落中的版本名，不包含外围的方括号，自动移除两端的空白。</param>
		/// <param name="version">该版本名对应的版本号。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> 或 <paramref name="version"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> 为空白或包含文件格式的保留字符。</exception>
		public Edition(string name, Version version)
		{
			this.Name = ValidateName(name, false);
			this.Version = version ?? throw new ArgumentNullException(nameof(version));
		}

		/// <summary>获取版本名。</summary>
		public string Name { get; }
		/// <summary>获取版本号。</summary>
		public Version Version { get; }

		/// <summary>返回 <c>版本名@版本号</c> 形式的文本表示。</summary>
		/// <returns>返回由版本名、@ 分隔符和版本号组成的字符串。</returns>
		/// <remarks>此文本用于表示单个具名版本，不是 <c>.version</c> 文件的段落格式。</remarks>
		public override string ToString() => $"{this.Name}@{this.Version}";
	}
	#endregion

	#region 嵌套集合
	/// <summary>表示保持添加顺序、版本名不区分大小写的可变版本集。</summary>
	/// <remarks>每个条目对应 <c>.version</c> 文件中的一个段落，按添加顺序枚举和保存。版本名必须唯一，名称查找不移除两端的空白；不保证线程安全。</remarks>
	public sealed class EditionCollection : ICollection<Edition>
	{
		private readonly ApplicationVersion _application;
		private readonly List<Edition> _items = new();
		private readonly Dictionary<string, Edition> _names = new(StringComparer.OrdinalIgnoreCase);

		internal EditionCollection(ApplicationVersion application) => _application = application;

		/// <summary>获取具名版本的数量。</summary>
		public int Count => _items.Count;
		bool ICollection<Edition>.IsReadOnly => false;
		/// <summary>获取一个值，指示版本集是否为空。</summary>
		public bool IsEmpty => _items.Count == 0;
		/// <summary>获取指定位置的版本。</summary>
		public Edition this[int index] => _items[index];
		/// <summary>获取指定名称的版本，名称比较不区分大小写。</summary>
		/// <exception cref="KeyNotFoundException">指定名称不存在。</exception>
		public Edition this[string name] => _names[name];

		/// <summary>添加具名版本，拒绝未初始化的版本或重复名称。</summary>
		/// <param name="item">要添加的具名版本。</param>
		/// <exception cref="ArgumentException"><paramref name="item"/> 未初始化或其名称已存在（忽略大小写）。</exception>
		/// <exception cref="InvalidOperationException">应用已设置顶层版本号。</exception>
		public void Add(Edition item)
		{
			if(item.Name == null || item.Version == null)
				throw new ArgumentException(Properties.Resources.Services_ApplicationVersion_EditionInvalid_Message, nameof(item));

			if(_application.Version != null)
				throw new InvalidOperationException(Properties.Resources.Services_ApplicationVersion_Conflict_Message);

			if(!_names.TryAdd(item.Name, item))
				throw new ArgumentException(Properties.Resources.Services_ApplicationVersion_EditionDuplicated_Message, nameof(item));

			_items.Add(item);
		}

		/// <summary>尝试获取指定名称的版本，名称比较不区分大小写。</summary>
		/// <param name="name">指定的版本名；为空时返回失败。不移除名称两端的空白。</param>
		/// <param name="result">找到的版本；未找到时为默认值。</param>
		/// <returns>找到指定名称的版本则返回真，否则返回假。</returns>
		public bool TryGetValue(string name, out Edition result)
		{
			result = default;
			return name != null && _names.TryGetValue(name, out result);
		}

		/// <summary>确定是否包含名称（忽略大小写）和版本号均相同的条目。</summary>
		/// <param name="item">要查找的具名版本。</param>
		/// <returns>名称和版本号均匹配时返回真，否则返回假。</returns>
		public bool Contains(Edition item) => this.TryGetValue(item.Name, out var edition) && Equals(edition.Version, item.Version);

		/// <summary>移除名称（忽略大小写）和版本号均相同的条目。</summary>
		/// <param name="item">要移除的具名版本。</param>
		/// <returns>找到并移除匹配条目时返回真，否则返回假。</returns>
		public bool Remove(Edition item)
		{
			if(!this.Contains(item))
				return false;

			for(var i = 0; i < _items.Count; i++)
			{
				if(StringComparer.OrdinalIgnoreCase.Equals(_items[i].Name, item.Name))
				{
					_items.RemoveAt(i);
					break;
				}
			}

			_names.Remove(item.Name);
			return true;
		}

		/// <summary>移除所有具名版本。</summary>
		public void Clear()
		{
			_items.Clear();
			_names.Clear();
		}

		void ICollection<Edition>.CopyTo(Edition[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		/// <summary>获取按添加顺序遍历版本集的枚举器。</summary>
		/// <returns>返回按添加顺序枚举版本项的枚举器。</returns>
		public List<Edition>.Enumerator GetEnumerator() => _items.GetEnumerator();
		IEnumerator<Edition> IEnumerable<Edition>.GetEnumerator() => this.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}
	#endregion
}
