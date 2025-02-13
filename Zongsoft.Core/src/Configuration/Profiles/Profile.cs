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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles
{
	/// <summary>
	/// 提供了对INI文件格式的各项操作。
	/// </summary>
	/// <remarks>
	///		<para>INI文件就是简单的文本文件，只不过这种文本文件要遵循一定的INI文件格式，其扩展名通常为“.ini”、“.cfg”、“.conf”等。</para>
	///		<para>INI文件中的每一行文本为一个元素单位，其类型分别为 Section(节)、Entry/Parameter(条目/参数)、Comment(注释)。</para>
	///		<para>Entry: INI所包含的最基本的“元素”就是 Entry/Parameter，每一个“条目”都由一个名称和一个值组成(值可选)，名称与值由等号“=”分隔，名称在等号的左边；值在等号右边，值的内容可省略。譬如：name=value 或者只有名称部分。注意：在同一个设置节中，条目名称必须唯一。</para>
	///		<para>Section: 所有的“条目”都是以“节”为单位结合在一起的。“节”名字都被方括号包围着。在“节”声明后的所有“条目”都是属于该“节”。对于一个“节”没有明显的结束标志符，一个“节”的开始就是上一个“节”的结束。</para>
	///		<para>注意：节是支持分层嵌套的，即在配置节中以空格或制表符(Tab)来分隔节的层级关系。</para>
	///		<para>Comment: 在INI文件中注释语句是以分号“;”或者“#”开始的。所有的注释语句不管多长都是独占一行直到结束的，在注释符和行结束符之间的所有内容都是被忽略的。</para>
	/// </remarks>
	public class Profile
	{
		#region 枚举定义
		private enum LineType
		{
			Empty,
			Entry,
			Section,
			Comment,
		}
		#endregion

		#region 成员字段
		private readonly string _filePath;
		private readonly ProfileItemCollection _items;
		private ProfileCommentCollection _comments;
		private ProfileSectionCollection _sections;
		#endregion

		#region 构造函数
		public Profile(string filePath = null)
		{
			if(filePath != null)
				_filePath = filePath.Trim();

			_items = new ProfileItemCollection(this);
			_sections = new ProfileSectionCollection(_items);
			_comments = new ProfileCommentCollection(_items);
		}
		#endregion

		#region 公共属性
		public string FilePath => _filePath;
		public ICollection<ProfileItem> Items => _items;
		public ICollection<ProfileComment> Comments => _comments;
		public IProfileItemCollection<ProfileSection> Sections => _sections;
		#endregion

		#region 加载方法
		public static Profile Load(string filePath, ProfileOptions options = null)
		{
			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			using(var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return Load(stream, null, options);
			}
		}

		public static Profile Load(Stream stream, ProfileOptions options = null) => Load(stream, null, options);
		public static Profile Load(Stream stream, Encoding encoding, ProfileOptions options = null)
		{
			if(stream == null)
				throw new ArgumentNullException(nameof(stream));

			ProfileSection section = null;
			Profile profile = new Profile(stream is FileStream fileStream ? fileStream.Name : string.Empty);
			ProfileReadingContext context = new ProfileReadingContext(profile, stream);

			using(var reader = new StreamReader(stream, encoding ?? Encoding.UTF8))
			{
				string text;
				context.LineNumber = 0;

				while((text = reader.ReadLine()) != null)
				{
					//解析读取到的行文本
					switch(ParseLine(text, out var content))
					{
						case LineType.Section:
							var parts = content.Split(' ', '\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

							if(parts == null || parts.Length == 0)
								section = null;
							else
							{
								var sections = (ProfileSectionCollection)profile.Sections;

								for(int i = 0; i < parts.Length; i++)
								{
									if(!sections.TryGetValue(parts[i], out section))
										section = sections.Add(parts[i], context.LineNumber);

									sections = (ProfileSectionCollection)section.Sections;
								}
							}

							break;
						case LineType.Entry:
							var index = content.IndexOf('=');

							if(section == null)
							{
								if(index < 0)
									profile.Items.Add(new ProfileEntry(context.LineNumber, content));
								else
									profile.Items.Add(new ProfileEntry(context.LineNumber, content[..index], content[(index + 1)..]));
							}
							else
							{
								if(index < 0)
									((ProfileEntryCollection)section.Entries).Add(context.LineNumber, content);
								else
									((ProfileEntryCollection)section.Entries).Add(context.LineNumber, content[..index], content[(index + 1)..]);
							}

							break;
						case LineType.Comment:
							var comment = ProfileComment.GetComment(content, context.LineNumber);

							if(section == null)
								profile._items.Add(comment);
							else
								section.Items.Add(comment);

							//如果是指令项则调用指令的读方法
							if(comment is ProfileDirective directive)
								context.OnRead(options, directive.Name, directive.Argument);

							break;
					}

					//递增行号
					context.LineNumber++;
				}
			}

			return profile;
		}
		#endregion

		#region 保存方法
		public void Save(ProfileOptions options = null)
		{
			if(string.IsNullOrWhiteSpace(_filePath))
				throw new InvalidOperationException();

			this.Save(_filePath, options);
		}

		public void Save(string filePath, ProfileOptions options = null) => this.Save(filePath, null, options);
		public void Save(string filePath, Encoding encoding, ProfileOptions options = null)
		{
			filePath ??= _filePath;

			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentException();

			using(var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				this.Save(stream, encoding, options);
			}
		}

		public void Save(Stream stream, ProfileOptions options = null) => this.Save(stream, null, options);
		public void Save(Stream stream, Encoding encoding, ProfileOptions options = null)
		{
			if(stream == null)
				throw new ArgumentNullException(nameof(stream));

			using(var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8))
			{
				this.Save(writer, options);
			}
		}

		public void Save(TextWriter writer, ProfileOptions options = null)
		{
			if(writer == null)
				throw new ArgumentNullException(nameof(writer));

			var context = new ProfileWritingContext(this, writer);

			foreach(var item in _items)
			{
				//忽略非本配置文件的条码
				if(item == null || item.Profile != this)
					continue;

				switch(item.ItemType)
				{
					case ProfileItemType.Entry:
						WriteEntry(writer, (ProfileEntry)item);
						break;
					case ProfileItemType.Section:
						WriteSection(writer, (ProfileSection)item);
						break;
					case ProfileItemType.Comment:
						WriteComment(writer, (ProfileComment)item);

						//如果是指令项则调用指令的写方法
						if(item is ProfileDirective directive)
							context.OnWrite(options, directive.Name, directive.Argument);

						break;
				}
			}
		}
		#endregion

		#region 操作方法
		/// <summary>获取指定路径的配置数据。</summary>
		/// <param name="path">指定的配置项路径，路径是以“/”斜杠分隔的文本。</param>
		/// <returns>如果找到则返回配置结果，否则返回空(null)。</returns>
		/// <remarks>
		///		<para>如果<paramref name="path"/>参数指定的配置路径以“/”斜杠结尾则将返回指定配置段的所有条目集；否则返回指定的配置条目的值。</para>
		/// </remarks>
		public object GetOptionValue(string path)
		{
			if(!this.ParsePath(path, false, out var section, out var name, out var isSectionPath))
				return null;

			if(section == null)
			{
				if(this.Sections.TryGetValue(name, out section))
					return section.Entries;
				else
					return null;
			}

			if(isSectionPath)
			{
				if(section.Sections.TryGetValue(name, out section))
					return section.Entries;
				else
					return null;
			}

			if(section.Entries.TryGetValue(name, out var entry))
				return entry.Value;

			return null;
		}

		public void SetOptionValue(string path, object optionObject)
		{
			if(!this.ParsePath(path, true, out var section, out var name, out var isSectionPath))
				return;

			if(section == null)
			{
				if(!_sections.TryGetValue(name, out var child))
					child = _sections.Add(name);

				UpdateEntries(child, optionObject);
			}
			else
			{
				if(isSectionPath)
				{
					if(!section.Sections.TryGetValue(name, out var child))
						child = ((ProfileSectionCollection)section.Sections).Add(name);

					UpdateEntries(child, optionObject);
				}
				else
				{
					section.SetEntryValue(name, Zongsoft.Common.Convert.ConvertValue<string>(optionObject));
				}
			}
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.FilePath;
		#endregion

		#region 私有方法
		private bool ParsePath(string path, bool createSectionOnNotExists, out ProfileSection section, out string name, out bool isSectionPath)
		{
			section = null;
			name = null;
			isSectionPath = false;

			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			var parts = path.Split('/');
			int index = -1;

			for(int i = parts.Length - 1; i >= 0; i--)
			{
				if(!string.IsNullOrWhiteSpace(parts[i]))
				{
					index = i;
					break;
				}
			}

			if(index < 0)
				return false;

			name = parts[index];
			isSectionPath = string.IsNullOrWhiteSpace(parts[parts.Length - 1]);

			var sections = (ProfileSectionCollection)this.Sections;

			for(int i = 0; i < index; i++)
			{
				if(string.IsNullOrWhiteSpace(parts[i]))
					continue;

				if(!sections.TryGetValue(parts[i], out section))
				{
					if(!createSectionOnNotExists)
						return false;

					section = sections.Add(parts[i]);
				}

				sections = (ProfileSectionCollection)section.Sections;
			}

			return true;
		}

		private static void UpdateEntries(ProfileSection section, object value)
		{
			if(section == null || value == null)
				return;

			if(value is IEnumerable<ProfileEntry> entries)
			{
				foreach(var entry in entries)
				{
					section.SetEntryValue(entry.Name, entry.Value);
				}

				return;
			}

			if(value is IDictionary dictionary)
			{
				foreach(DictionaryEntry entry in dictionary)
				{
					section.SetEntryValue(
						Zongsoft.Common.Convert.ConvertValue<string>(entry.Key),
						Zongsoft.Common.Convert.ConvertValue<string>(entry.Value));
				}

				return;
			}

			if(Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(IDictionary<,>), value.GetType()))
			{
				foreach(var entry in (IEnumerable)value)
				{
					var entryKey = entry.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance).GetValue(entry);
					var entryValue = entry.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).GetValue(entry);

					section.SetEntryValue(
						Zongsoft.Common.Convert.ConvertValue<string>(entryKey),
						Zongsoft.Common.Convert.ConvertValue<string>(entryValue));
				}
			}
		}

		private static void WriteEntry(TextWriter writer, ProfileEntry entry)
		{
			if(string.IsNullOrWhiteSpace(entry.Value))
				writer.WriteLine(entry.Name);
			else
				writer.WriteLine(entry.Name + "=" + entry.Value);
		}

		private static void WriteComment(TextWriter writer, ProfileComment comment)
		{
			foreach(var line in comment.Lines)
				writer.WriteLine($"#{line}");
		}

		private static void WriteSection(TextWriter writer, ProfileSection section)
		{
			var sections = new List<ProfileSection>();

			if(section.Entries.Count > 0 || section.Comments.Count > 0)
			{
				writer.WriteLine();
				writer.WriteLine($"[{section.FullName}]");
			}

			foreach(var item in section.Items)
			{
				switch(item.ItemType)
				{
					case ProfileItemType.Section:
						sections.Add((ProfileSection)item);
						break;
					case ProfileItemType.Entry:
						WriteEntry(writer, (ProfileEntry)item);
						break;
					case ProfileItemType.Comment:
						WriteComment(writer, (ProfileComment)item);
						break;
				}
			}

			if(sections.Count > 0)
			{
				foreach(var child in sections)
					WriteSection(writer, child);
			}
		}

		private static LineType ParseLine(string text, out string result)
		{
			result = null;

			if(string.IsNullOrWhiteSpace(text))
				return LineType.Empty;

			text = text.Trim();

			if(text[0] == ';' || text[0] == '#')
			{
				result = text[1..];
				return LineType.Comment;
			}

			if(text[0] == '[' && text[^1] == ']')
			{
				result = text[1..^1];
				return LineType.Section;
			}

			if(text[0] == '=')
				throw new ProfileException("Invalid format.");

			result = text;
			return LineType.Entry;
		}
		#endregion
	}
}
