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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Configuration.Profiles;

namespace Zongsoft.Services;

/// <summary>表示应用 <c>.edition</c> 清单中的名称、版本号、发行版集合及当前选择，并提供文件、流及文本读写器的加载和保存功能。</summary>
/// <remarks>
///		<para><c>.edition</c> 是采用 INI 段落形式的文本文件，支持以下两种互斥的格式：</para>
///		<list type="bullet">
///			<item><description>不区分版本名时，根条目采用 <c>name@version</c> 格式，分别对应 <see cref="Name"/> 和 <see cref="Version"/>，<see cref="Editions"/> 为空；其后只能包含空行或整行注释。</description></item>
///			<item><description>区分版本名时，根条目采用 <c>name</c> 或 <c>name=edition</c> 格式，可选的值指定 <see cref="EditionCollection.Current"/>，空值表示未选择。随后每个 <c>[edition]</c> 段落包含且仅包含一个裸版本号，分别对应 <see cref="Edition.Name"/> 和 <see cref="Edition.Version"/>；顶层 <see cref="Version"/> 为 <see langword="null"/>。</description></item>
///		</list>
///		<para>版本号采用 <see cref="System.Version"/> 支持的两段、三段或四段数字格式。版本名不区分大小写且必须唯一，段落顺序保留在 <see cref="Editions"/> 中；指定的当前发行版必须存在，未指定时不自动选择首项。</para>
///		<para>读取时忽略空行、行、名称及首行值两端的空白，以及以 <c>;</c> 或 <c>#</c> 开始的整行注释；接受文件开头的 BOM 和 CRLF、LF、CR 换行。段落内不支持 <c>Version=1.0.1</c> 这样的键值项，清单不支持嵌套段落或行尾注释。读取复用 Profile 的解析规则，但不支持导入；发现 #@import 或 ;@import 指令时，在打开导入文件前抛出格式异常。</para>
///		<para>构造时仅检查名称非空并移除两端空白；名称是否可用于文件格式由调用方确定，读写遵循 Profile 原生规则。应用名称中的 <c>@</c> 和 <c>=</c> 为格式分隔符，发行版名遵循 Profile 段名规则。</para>
///		<para>保存到文件或流时输出 UTF-8 无 BOM 文本；保存到文本写入器时编码由写入器决定。保存采用写入器的标准换行；文件和流采用平台默认换行，文本输出遵循 <see cref="TextWriter.NewLine"/>，文件末尾保留换行；默认保留已有空行和注释内容，注释标记按 Profile 规则统一为 #；新建清单的段落之间保留一个空行。结构变更后，原段落的备注和空行仍保留，位置可能随段落调整。</para>
///		<para>传入的流和文本读写器由调用方负责释放，加载或保存不会关闭它们。加载从当前位置读取至结尾，保存从当前位置写入；流重载不重置位置或截断内容，也不要求支持定位。</para>
///		<para>顶层版本号与非空的具名版本集互斥，切换表示方式前须先清空原有表示。允许暂时不设置任何版本信息，但保存时必须具有顶层版本号或至少一个具名版本。本类型及其版本集不保证线程安全。</para>
/// </remarks>
/// <example>
///		<para>不区分版本名的 <c>.edition</c> 文件：</para>
///		<code language="ini">MyApplicationName@1.0.1</code>
///		<para>包含多个具名版本的 <c>.edition</c> 文件：</para>
///		<code language="ini">
///			MyApplicationName=Professional
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
public class ApplicationManifest
{
	#region 常量定义
	private const string FILE_NAME = ".edition";
	#endregion

	#region 成员字段
	private Version _version;
	private Profile _profile;
	#endregion

	#region 构造函数
	/// <summary>初始化应用清单；未指定版本号时，可随后添加具名版本。</summary>
	/// <param name="name">应用名称，自动移除两端的空白。</param>
	/// <param name="version">不区分版本名时的版本号；为 <see langword="null"/> 时，可通过 <see cref="Editions"/> 添加具名版本。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="name"/> 为空串或空白。</exception>
	public ApplicationManifest(string name, Version version = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		this.Name = name.Trim();
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
				throw new InvalidOperationException(Properties.Resources.Services_ApplicationManifest_Conflict_Message);

			_version = value;
		}
	}

	/// <summary>获取按段落顺序排列的具名版本集。</summary>
	/// <remarks>每个条目对应文件中的一个版本段落；不区分版本名时此集合为空。</remarks>
	public EditionCollection Editions { get; }
	#endregion

	#region 公共方法
	/// <summary>从指定目录中的版本文件或指定文件加载应用版本信息。</summary>
	/// <param name="path">指定的目录或文件路径；为空(<c>null</c>)或空串时默认为当前应用的根目录。路径为现有目录时读取其中的 <c>.edition</c> 文件，否则读取指定的现有文件。</param>
	/// <returns>从文件中读取的应用版本信息；文件不存在时返回 <see langword="null"/>。</returns>
	/// <exception cref="FormatException">文件为空或内容不符合应用版本格式，清单校验错误包含行号，Profile 语法错误保留为内部异常。</exception>
	/// <remarks>文件格式及示例见 <see cref="ApplicationManifest"/>。缺少版本号、版本段落内容无效、指定的当前发行版不存在或混用两种格式均视为格式错误；打开和读取文件时的文件系统异常直接向调用方传播。</remarks>
	public static ApplicationManifest Load(string path)
	{
		if(string.IsNullOrEmpty(path))
			path = AppContext.BaseDirectory;

		if(Directory.Exists(path))
			path = Path.Combine(path, FILE_NAME);

		if(!File.Exists(path))
			return null;

		return LoadProfile(() => Profile.Load(path, new ProfileOptions { Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) } }));
	}

	/// <summary>从指定流加载应用版本信息。</summary>
	/// <param name="stream">可读取的流，从当前位置读取至结尾，不要求支持定位。</param>
	/// <returns>从流中读取的应用版本信息。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="stream"/> 不支持读取。</exception>
	/// <exception cref="FormatException">内容为空或不符合应用版本格式，清单校验错误包含行号，Profile 语法错误保留为内部异常。</exception>
	/// <remarks>默认按 UTF-8 解码，并通过 BOM 检测编码。无论成功或失败均不关闭传入的流，由调用方负责释放；读取异常直接向调用方传播。格式及示例见 <see cref="ApplicationManifest"/>。</remarks>
	public static ApplicationManifest Load(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
		return Load(reader);
	}

	/// <summary>从指定文本读取器加载应用版本信息。</summary>
	/// <param name="reader">文本读取器，从当前位置读取至结尾。</param>
	/// <returns>从读取器中读取的应用版本信息。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="reader"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="FormatException">内容为空或不符合应用版本格式，清单校验错误包含行号，Profile 语法错误保留为内部异常。</exception>
	/// <remarks>编码由读取器决定；无论成功或失败均不关闭读取器，由调用方负责释放。读取异常直接向调用方传播。格式及示例见 <see cref="ApplicationManifest"/>。</remarks>
	public static ApplicationManifest Load(TextReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);
		return LoadProfile(() => Profile.Load(reader, new ProfileOptions { Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) } }));
	}

	/// <summary>将应用版本信息保存到指定目录中的版本文件或指定文件，覆盖原有内容。</summary>
	/// <param name="path">指定的目录或文件路径；为空(<c>null</c>)或空串时默认为当前应用的根目录。路径为现有目录时保存到其中的 <c>.edition</c> 文件，否则保存到指定的现有文件。</param>
	/// <exception cref="InvalidOperationException">未设置顶层版本号且版本集为空。</exception>
	/// <remarks>
	/// <para>设置了 <see cref="Version"/> 时保存为 <c>name@version</c>；否则首行输出应用名称及可选的 <c>=edition</c> 当前选择，随后按 <see cref="Editions"/> 的顺序输出版本段落。未选择时省略等号，选中时采用段落名称的大小写，格式示例见 <see cref="ApplicationManifest"/>。</para>
	/// <para>首先检查版本信息是否完整；指定路径既不是现有目录也不是现有文件时不执行写入。现有目录中的 <c>.edition</c> 文件可自动创建，通过 Profile 保存替换现有目标文件，不自动创建父目录。写入前读取现有目标文件的空行和注释；目标为空或新建时采用已加载的布局。输出 UTF-8 无 BOM 文本和平台默认换行，保留空行和注释内容；输出前由 Profile 验证文件语法，文件写入和错误处理沿用 Profile 的保存行为。</para>
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

		var source = File.Exists(path) ? ReadProfile(() => Profile.Load(path, new ProfileOptions { Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) } })) : _profile;
		if(source == null || source.Statements.Count == 0 && (source.Blanks == null || source.Blanks.Length == 0))
			source = _profile;

		var profile = this.CreateProfile(source);
		profile.Save(path, new UTF8Encoding(false));
		_profile = profile;
	}

	/// <summary>将应用版本信息保存到指定流。</summary>
	/// <param name="stream">可写入的流，从当前位置写入，不重置位置或截断内容。</param>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException"><paramref name="stream"/> 不支持写入。</exception>
	/// <exception cref="InvalidOperationException">未设置顶层版本号且版本集为空。</exception>
	/// <remarks>写入前检查版本信息是否完整。输出 UTF-8 无 BOM 文本和平台默认换行，完成后刷新缓冲区；不要求流支持定位。无论成功或失败均不关闭传入的流，由调用方负责释放；写入和刷新异常直接向调用方传播。</remarks>
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
	/// <remarks>写入前检查版本信息是否完整。使用写入器的标准换行，不改变写入器的 <see cref="TextWriter.NewLine"/> 属性；完成后调用 <see cref="TextWriter.Flush()"/>。无论成功或失败均不关闭写入器，由调用方负责释放；写入和刷新异常直接向调用方传播。</remarks>
	public void Save(TextWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		this.ValidateVersion();
		this.SaveProfile(this.CreateProfile(_profile), writer);
	}
	#endregion

	#region 私有方法
	private void ValidateVersion()
	{
		if(_version == null && this.Editions.IsEmpty)
			throw new InvalidOperationException(Properties.Resources.Services_ApplicationManifest_VersionRequired_Message);
	}

	private void SaveProfile(Profile profile, TextWriter writer)
	{
		profile.Save(writer);
		writer.Flush();
		_profile = profile;
	}

	private Profile CreateProfile(Profile source)
	{
		var profile = new Profile();
		profile.BeginRead();

		var line = 0;
		List<int> blanks = [];
		var headerWritten = false;
		ProfileSection section = null;
		var layouts = GetLayouts(source);

		foreach(var layout in layouts)
		{
			if(layout.Section == null)
				Append(layout);
		}

		if(!headerWritten)
			WriteHeader();

		foreach(var edition in this.Editions)
		{
			var found = false;

			foreach(var layout in layouts)
			{
				if(layout.Section != null && this.Editions.Comparer.Equals(layout.Section.Name, edition.Name))
				{
					Append(layout, edition);
					found = true;
				}
			}

			if(!found)
			{
				if(blanks.Count == 0 || blanks[^1] != line - 1)
					blanks.Add(line++);

				section = profile.Sections.GetOrAdd(edition.Name, line);
				profile.DeclareSection(section, line++, true);
			}

			if(section.Entries.Count == 0)
				section.Entries.AddParsed(line++, edition.Version.ToString());
		}

		//删除段落或切换为单版本时，仍保留原段落的注释和空行。
		foreach(var layout in layouts)
		{
			if(layout.Section != null && !this.Editions.Contains(layout.Section.Name))
				Append(layout);
		}

		profile.CompleteRead([.. blanks]);
		return profile;

		void WriteHeader()
		{
			profile.Entries.AddParsed(line++, _version == null ? this.Name : this.Name + "@" + _version, _version == null ? this.Editions.Current.Name : null);
			headerWritten = true;
		}

		void Append(SectionLayout layout, Edition edition = default)
		{
			foreach(var statement in layout.Items)
			{
				if(statement == null)
					blanks.Add(line++);
				else if(statement.IsSection)
				{
					if(edition.Version != null)
					{
						section = profile.Sections.GetOrAdd(edition.Name, line);
						profile.DeclareSection(section, line++, true);
					}
				}
				else if(statement.Item is ProfileComment comment)
				{
					var comments = layout.Section == null ? profile.Comments : section?.Comments ?? profile.Comments;
					comments.AddParsed(comment.Text, line);
					line += Math.Max(1, comment.Lines.Length);
				}
				else if(statement.Item is ProfileEntry)
				{
					if(layout.Section == null)
					{
						if(!headerWritten)
							WriteHeader();
					}
					else if(edition.Version != null && section.Entries.Count == 0)
						section.Entries.AddParsed(line++, edition.Version.ToString());
				}
			}
		}
	}

	private static List<SectionLayout> GetLayouts(Profile profile)
	{
		List<SectionLayout> layouts = [new(null)];
		if(profile == null)
			return layouts;

		var layout = layouts[0];
		var blanks = profile.Blanks ?? [];
		int blankIndex = 0;

		foreach(var statement in profile.Statements)
		{
			while(blankIndex < blanks.Length && blanks[blankIndex] <= statement.LineNumber)
			{
				layout.Items.Add(null);
				blankIndex++;
			}

			if(layout.Section != statement.Section)
			{
				layout = new SectionLayout(statement.Section);
				layouts.Add(layout);
			}

			layout.Items.Add(statement);
		}

		while(blankIndex++ < blanks.Length)
			layout.Items.Add(null);

		return layouts;
	}

	private static ApplicationManifest LoadProfile(Func<Profile> load) => Parse(ReadProfile(load));

	private static Profile ReadProfile(Func<Profile> load)
	{
		try
		{
			return load();
		}
		catch(Exception exception) when(exception is ProfileException or ArgumentException)
		{
			throw new FormatException(exception.Message, exception);
		}
	}

	private static ApplicationManifest Parse(Profile profile)
	{
		if(profile.Entries.Count != 1)
			throw InvalidFormat(profile.Entries.Count > 1 ? profile.Entries[1].LineNumber + 1 : 1, Properties.Resources.Services_ApplicationManifest_InformationRequired_Message);

		var header = profile.Entries[0];
		var separator = header.Name.IndexOf('@');
		var name = (separator < 0 ? header.Name.AsSpan() : header.Name.AsSpan(0, separator)).Trim();

		if(name.IsEmpty)
			throw InvalidFormat(header.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_ApplicationNameInvalid_Message);

		Version version = null;
		if(separator >= 0)
		{
			if(header.Value != null || profile.Sections.Count > 0)
				throw InvalidFormat(header.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_UnexpectedContent_Message);

			if(!System.Version.TryParse(header.Name.AsSpan(separator + 1).Trim(), out version))
				throw InvalidFormat(header.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_VersionInvalid_Message);
		}

		var manifest = new ApplicationManifest(name.ToString(), version) { _profile = profile };

		foreach(var section in profile.Sections)
		{
			if(section.Sections.Count > 0)
				throw InvalidFormat(section.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_EditionNameInvalid_Message);

			if(section.Entries.Count != 1)
				throw InvalidFormat(section.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_BareVersionRequired_Message);

			var entry = section.Entries[0];
			if(entry.Value != null || !System.Version.TryParse(entry.Name, out var editionVersion))
				throw InvalidFormat(entry.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_BareVersionRequired_Message);

			manifest.Editions.Add(section.Name, editionVersion);
		}

		if(!string.IsNullOrEmpty(header.Value))
		{
			if(!manifest.Editions.TryGetValue(header.Value, out var current))
				throw InvalidFormat(header.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_EditionNotFound_Message);

			manifest.Editions.Current = current;
		}

		if(manifest.Version == null && manifest.Editions.IsEmpty)
			throw InvalidFormat(header.LineNumber + 1, Properties.Resources.Services_ApplicationManifest_InformationRequired_Message);

		return manifest;
	}

	private static FormatException InvalidFormat(int lineNumber, string message) => new(string.Format(Properties.Resources.Services_ApplicationManifest_InvalidFormat_Message, Math.Max(1, lineNumber), message));
	#endregion

	#region 嵌套类型
	private sealed class SectionLayout(ProfileSection section)
	{
		public ProfileSection Section { get; } = section;
		public List<Profile.Statement> Items { get; } = [];
	}
	#endregion

	#region 嵌套结构
	/// <summary>表示一个具名应用版本。</summary>
	/// <remarks>完整发行版对应 <c>.edition</c> 文件中的一个段落：段落标题为 <see cref="Name"/>，段落内的裸版本号为 <see cref="Version"/>。字符串可隐式转换为仅含名称的引用，用于设置 <see cref="EditionCollection.Current"/>；名称引用不能添加到发行版集合中。</remarks>
	public readonly struct Edition : IEquatable<Edition>
	{
		#region 私有构造
		private Edition(string name) => this.Name = name.Trim();
		#endregion

		#region 公共构造
		/// <summary>初始化具有非空名称和版本号的具名版本。</summary>
		/// <param name="name">段落中的版本名，不包含外围的方括号，自动移除两端的空白。</param>
		/// <param name="version">该版本名对应的版本号。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> 或 <paramref name="version"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> 为空串或空白。</exception>
		public Edition(string name, Version version)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			this.Name = name.Trim();
			this.Version = version ?? throw new ArgumentNullException(nameof(version));
		}
		#endregion

		#region 公共属性
		/// <summary>获取版本名。</summary>
		public string Name { get; }
		/// <summary>获取版本号；名称引用或默认值的版本号为 <see langword="null"/>。</summary>
		public Version Version { get; }
		#endregion

		#region 重写方法
		/// <summary>确定名称（忽略大小写）和版本号是否均相同。</summary>
		/// <param name="other">要比较的发行版。</param>
		/// <returns>名称和版本号均相同则返回真，否则返回假。</returns>
		public bool Equals(Edition other) => StringComparer.OrdinalIgnoreCase.Equals(this.Name, other.Name) && Equals(this.Version, other.Version);
		public override bool Equals(object obj) => obj is Edition other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name), this.Version);

		/// <summary>返回发行版的文本表示。</summary>
		/// <returns>完整发行版返回 <c>版本名@版本号</c>，名称引用返回名称，默认值返回空串。</returns>
		/// <remarks>此文本用于表示单个具名版本，不是 <c>.edition</c> 文件的段落格式。</remarks>
		public override string ToString() => this.Version == null ? this.Name ?? string.Empty : $"{this.Name}@{this.Version}";
		#endregion

		#region 符号重载
		public static bool operator ==(Edition left, Edition right) => left.Equals(right);
		public static bool operator !=(Edition left, Edition right) => !left.Equals(right);
		#endregion

		#region 隐式转换
		/// <summary>将名称转换为可用于选择当前发行版的引用。</summary>
		/// <param name="name">发行版名称，移除两端空白；为 <see langword="null"/> 时或空字符串返回默认值，可取消当前选择。</param>
		/// <returns>返回仅含名称的发行版引用，或表示未选择的默认值。</returns>
		public static implicit operator Edition(string name) => string.IsNullOrEmpty(name) ? default : new Edition(name);
		#endregion
	}
	#endregion

	#region 嵌套集合
	/// <summary>表示保持添加顺序、版本名不区分大小写的可变版本集。</summary>
	/// <remarks>每个条目对应 <c>.edition</c> 文件中的一个段落，按集合顺序枚举和保存。版本名必须唯一，名称查找不移除两端的空白。</remarks>
	public sealed class EditionCollection : KeyedCollection<string, Edition>
	{
		#region 成员字段
		private readonly ApplicationManifest _application;
		private string _current;
		#endregion

		#region 构造函数
		internal EditionCollection(ApplicationManifest application) : base(StringComparer.OrdinalIgnoreCase) => _application = application;
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示版本集是否为空。</summary>
		public bool IsEmpty => this.Count == 0;

		/// <summary>获取或设置当前发行版；默认值表示未选择，设置默认值可取消选择。</summary>
		/// <remarks>可通过字符串隐式转换按名称选择，名称忽略大小写；赋值完整发行版时还须匹配版本号。读取时始终返回集合中的完整条目。字符串 <see langword="null"/> 可取消选择。删除当前项或清空集合会取消选择；同名替换跟随新值，改名替换取消选择。不自动选择首项。</remarks>
		/// <exception cref="ArgumentException">指定的非默认值不属于集合。</exception>
		public Edition Current
		{
			get => _current != null && this.TryGetValue(_current, out var current) ? current : default;
			set
			{
				if(value == default)
					_current = null;
				else
				{
					if(!this.TryGetValue(value.Name, out var current) || value.Version != null && current != value)
						throw new ArgumentException(Properties.Resources.Services_ApplicationManifest_EditionNotFound_Message, nameof(value));

					_current = current.Name;
				}
			}
		}
		#endregion

		#region 公共方法
		/// <summary>添加具有指定名称和版本号的发行版。</summary>
		/// <param name="name">发行版名称，自动移除两端的空白。</param>
		/// <param name="version">发行版的版本号。</param>
		/// <returns>返回已添加的发行版。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> 或 <paramref name="version"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException">名称无效或已存在。</exception>
		/// <exception cref="InvalidOperationException">应用已设置顶层版本号。</exception>
		public Edition Add(string name, Version version)
		{
			var edition = new Edition(name, version);
			base.Add(edition);
			return edition;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Edition item) => item.Name;
		protected override void InsertItem(int index, Edition item)
		{
			this.Validate(item);
			base.InsertItem(index, item);
		}
		protected override void SetItem(int index, Edition item)
		{
			this.Validate(item);
			var current = this.Comparer.Equals(_current, this[index].Name);
			base.SetItem(index, item);

			if(current)
				_current = this.Comparer.Equals(_current, item.Name) ? item.Name : null;
		}
		protected override void RemoveItem(int index)
		{
			var current = this.Comparer.Equals(_current, this[index].Name);
			base.RemoveItem(index);

			if(current)
				_current = null;
		}
		protected override void ClearItems()
		{
			base.ClearItems();
			_current = null;
		}
		#endregion

		#region 私有方法
		private void Validate(Edition item)
		{
			if(item.Name == null || item.Version == null)
				throw new ArgumentException(Properties.Resources.Services_ApplicationManifest_EditionInvalid_Message, nameof(item));

			if(_application.Version != null)
				throw new InvalidOperationException(Properties.Resources.Services_ApplicationManifest_Conflict_Message);
		}
		#endregion
	}
	#endregion
}
