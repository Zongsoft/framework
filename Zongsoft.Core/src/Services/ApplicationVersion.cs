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

/// <summary>表示应用版本文件中的名称、版本号和版本集。</summary>
/// <remarks>顶层版本号与具名版本集互斥。本类型及其版本集不保证线程安全。</remarks>
public class ApplicationVersion
{
	#region 成员字段
	private Version _version;
	#endregion

	#region 构造函数
	/// <summary>初始化应用版本信息；未指定版本号时，可随后添加具名版本。</summary>
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
	public EditionCollection Editions { get; }
	#endregion

	#region 公共方法
	/// <summary>从指定文件加载应用版本信息。</summary>
	/// <param name="path">版本文件的路径，不是应用目录。</param>
	/// <exception cref="FormatException">文件为空或内容不符合应用版本格式，异常信息包含行号。</exception>
	/// <remarks>接受空行和以分号或井号开始的整行注释；文件系统异常直接向调用方传播。</remarks>
	public static ApplicationVersion Load(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		return Parse(File.ReadAllText(path).AsSpan());
	}

	/// <summary>将应用版本信息保存到指定文件，覆盖原有内容。</summary>
	/// <param name="path">版本文件的路径，不是应用目录；父目录必须存在。</param>
	/// <exception cref="InvalidOperationException">未设置顶层版本号且版本集为空。</exception>
	/// <remarks>输出 UTF-8 无 BOM 文本和 CRLF 换行，不保留原文件的注释和空白。</remarks>
	public void Save(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		if(_version == null && this.Editions.IsEmpty)
			throw new InvalidOperationException(Properties.Resources.Services_ApplicationVersion_VersionRequired_Message);

		using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
		writer.NewLine = "\r\n";
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
			writer.WriteLine();

			foreach(var edition in this.Editions)
			{
				writer.WriteLine();
				writer.Write('[');
				writer.Write(edition.Name);
				writer.WriteLine(']');
				WriteVersion(writer, edition.Version, buffer);
			}
		}
	}
	#endregion

	#region 私有方法
	private static void WriteVersion(StreamWriter writer, Version version, Span<char> buffer)
	{
		if(!version.TryFormat(buffer, out var count))
			throw new InvalidOperationException(Properties.Resources.Services_ApplicationVersion_FormattingBufferExceeded_Message);

		writer.Write(buffer[..count]);
		writer.WriteLine();
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
	public readonly struct Edition
	{
		/// <summary>初始化具有非空名称和版本号的具名版本。</summary>
		public Edition(string name, Version version)
		{
			this.Name = ValidateName(name, false);
			this.Version = version ?? throw new ArgumentNullException(nameof(version));
		}

		/// <summary>获取版本名。</summary>
		public string Name { get; }
		/// <summary>获取版本号。</summary>
		public Version Version { get; }

		public override string ToString() => $"{this.Name}@{this.Version}";
	}
	#endregion

	#region 嵌套集合
	/// <summary>表示保持添加顺序、版本名不区分大小写的可变版本集。</summary>
	/// <remarks>版本名必须唯一；不保证线程安全。</remarks>
	public sealed class EditionCollection : ICollection<Edition>
	{
		private readonly ApplicationVersion _application;
		private readonly List<Edition> _items = new();
		private readonly Dictionary<string, Edition> _names = new(StringComparer.OrdinalIgnoreCase);

		internal EditionCollection(ApplicationVersion application) => _application = application;

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
		public bool Contains(Edition item) => this.TryGetValue(item.Name, out var edition) && Equals(edition.Version, item.Version);

		/// <summary>移除名称（忽略大小写）和版本号均相同的条目。</summary>
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

		public void Clear()
		{
			_items.Clear();
			_names.Clear();
		}

		void ICollection<Edition>.CopyTo(Edition[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		/// <summary>获取按添加顺序遍历版本集的枚举器。</summary>
		public List<Edition>.Enumerator GetEnumerator() => _items.GetEnumerator();
		IEnumerator<Edition> IEnumerable<Edition>.GetEnumerator() => this.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}
	#endregion
}
