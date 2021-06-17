/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.IO
{
	/// <summary>
	/// 表示不依赖操作系统的路径。
	/// </summary>
	/// <remarks>
	///		<para>路径格式分为<seealso cref="PathToken.Scheme"/>和<seealso cref="PathToken.FullPath"/>这两个部分，中间使用冒号(:)分隔，路径各层级间使用正斜杠(/)进行分隔。如果是目录路径则以正斜杠(/)结尾。</para>
	///		<para>其中<seealso cref="PathToken.Scheme"/>可以省略，如果为目录路径，则<see cref="PathToken.FileName"/>属性为空或空字符串("")。常用路径示例如下：</para>
	///		<list type="bullet">
	///			<item>
	///				<term>某个文件的<see cref="Url"/>：zfs:/data/attachments/2014/07/file-name.ext</term>
	///			</item>
	///			<item>
	///				<term>某个本地文件的<see cref="Url"/>：zfs.local:/data/attachments/2014/07/file-name.ext</term>
	///			</item>
	///			<item>
	///				<term>某个分布式文件的<see cref="Url"/>：zfs.distributed:/data/attachments/file-name.ext</term>
	///			</item>
	///			<item>
	///				<term>某个目录的<see cref="Url"/>：zfs:/data/attachments/2014/07/</term>
	///			</item>
	///			<item>
	///				<term>未指定模式(Scheme)的<see cref="Url"/>：/data/attachements/images/</term>
	///			</item>
	///		</list>
	/// </remarks>
	public struct PathToken
	{
		#region 成员字段
		private string _scheme;
		private string[] _segments;
		private PathAnchor _anchor;
		#endregion

		#region 私有构造
		private PathToken(string scheme, PathAnchor anchor, string[] segments)
		{
			_scheme = scheme;
			_segments = segments;
			_anchor = anchor;
		}
		#endregion

		#region 公共属性
		/// <summary>获取路径的文件系统<see cref="IFileSystem.Scheme"/>方案。</summary>
		public string Scheme { get => _scheme; }

		/// <summary>获取路径的锚点，即路径的起始点。</summary>
		public PathAnchor Anchor { get => _anchor; }

		/// <summary>获取路径中的文件名，有关路径中文件名的定义请参考备注说明。</summary>
		/// <remarks>
		///		<para>路径如果以斜杠(/)结尾，则表示该路径为「目录路径」，即<see cref="FileName"/>属性为空(null)或空字符串("")；否则文件名则为<see cref="Segments"/>路径节数组中的最后一个节的内容。</para>
		/// </remarks>
		public string FileName { get => _segments != null && _segments.Length > 0 ? _segments[^1] : null; }

		/// <summary>获取路径的完整路径（注：不含<see cref="Scheme"/>部分）。</summary>
		public string FullPath { get => GetAnchorString(_anchor, true) + string.Join("/", _segments); }

		/// <summary>
		/// 获取路径的完整URL，该属性值包含<see cref="Scheme"/>和<see cref="FullPath"/>。
		/// </summary>
		/// <remarks>
		///		<para>如果<see cref="Scheme"/>为空(null)或空字符串("")，则<see cref="Url"/>与<see cref="FullPath"/>属性值相同。</para>
		/// </remarks>
		public string Url { get => (string.IsNullOrEmpty(_scheme) ? LocalFileSystem.Instance.Scheme : _scheme) + ":" + this.FullPath; }

		/// <summary>
		/// 获取路径中各节点数组，更多内容请参考备注说明。
		/// </summary>
		/// <remarks>
		///		<para>如果当前路径是一个「文件路径」，即<see cref="IsFile"/>属性为真(True)，则该数组的最后一个元素内容就是<see cref="FileName"/>的值，亦文件路径的<see cref="Segments"/>不可能为空数组，因为它至少包含一个为文件名的元素。</para>
		///		<para>如果当前路径是一个「目录路径」，即<see cref="IsDirectory"/>属性为真(True)，并且不是空目录，则该数组的最后一个元素值为空(null)或空字符串("")。所谓“空目录”的示例如下：</para>
		///		<list type="bullet">
		///			<item>空目录：scheme:/</item>
		///			<item>空目录：scheme:./</item>
		///			<item>空目录：scheme:../</item>
		///			<item>非空目录：scheme:root/</item>
		///			<item>非空目录：scheme:root/directory/</item>
		///			<item>非空目录：scheme:/root/</item>
		///			<item>非空目录：scheme:/root/directory/</item>
		///			<item>非空目录：scheme:./root/</item>
		///			<item>非空目录：scheme:./root/directory/</item>
		///			<item>非空目录：scheme:../root/</item>
		///			<item>非空目录：scheme:../root/directory/</item>
		///		</list>
		/// </remarks>
		public string[] Segments { get => _segments; }

		/// <summary>
		/// 获取一个值，指示当前路径是否为文件路径。如果返回真(True)，即表示<see cref="FileName"/>有值。
		/// </summary>
		/// <remarks>
		///		<para>路径如果不是以斜杠(/)结尾，则表示该路径为「文件路径」，文件路径中的<see cref="FileName"/>即为<see cref="Segments"/>数组中最后一个元素的值。</para>
		/// </remarks>
		public bool IsFile { get => _segments != null && _segments.Length > 0 && !string.IsNullOrEmpty(_segments[^1]); }

		/// <summary>
		/// 获取一个值，指示当前路径是否为目录路径。有关「目录路径」定义请参考备注说明。
		/// </summary>
		/// <remarks>
		///		<para>路径如果以斜杠(/)结尾，则表示该路径为「目录路径」，即<see cref="FileName"/>属性为空(null)或空字符串("")；否则文件名则为<see cref="Segments"/>路径节数组中的最后一个节的内容。</para>
		/// </remarks>
		public bool IsDirectory { get => _segments == null || _segments.Length == 0 || string.IsNullOrEmpty(_segments[^1]); }
		#endregion

		#region 重写方法
		public bool Equals(PathToken path)
		{
			return string.Equals(path.Scheme, _scheme, StringComparison.OrdinalIgnoreCase) &&
				_anchor == path.Anchor &&
				(
					((_segments == null || _segments.Length == 0) && (path._segments == null || path._segments.Length == 0)) ||
					_segments.Length == path._segments.Length &&
					_segments.SequenceEqual(path._segments)
				);
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return string.Equals(this.Url, ((PathToken)obj).Url);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_scheme, _anchor, string.Join('/', _segments));
		}

		public override string ToString()
		{
			return this.Url;
		}
		#endregion

		#region 静态方法
		/// <summary>
		/// 解析路径。
		/// </summary>
		/// <param name="text">要解析的路径文本。</param>
		/// <returns>返回解析成功的<see cref="PathToken"/>路径对象。</returns>
		/// <exception cref="PathException">当<paramref name="text"/>参数为无效的路径格式。</exception>
		public static PathToken Parse(string text)
		{
			if(string.IsNullOrEmpty(text))
				throw new ArgumentNullException(text);

			//解析路径文本，由特定参数指定解析失败是否抛出异常
			if(ParseCore(text, true, out string scheme, out string[] segments, out PathAnchor anchor))
				return new PathToken(scheme, anchor, segments);

			return default;
		}

		/// <summary>
		/// 尝试解析路径。
		/// </summary>
		/// <param name="text">要解析的路径文本。</param>
		/// <param name="path">解析成功的<see cref="PathToken"/>路径对象。</param>
		/// <returns>如果解析成功则返回真(True)，否则返回假(False)。</returns>
		public static bool TryParse(string text, out PathToken path)
		{
			if(!string.IsNullOrEmpty(text) && ParseCore(text, false, out var scheme, out var segments, out var anchor))
			{
				path = new PathToken(scheme, anchor, segments);
				return true;
			}

			path = default;
			return false;
		}

		/// <summary>
		/// 解析路径。
		/// </summary>
		/// <param name="text">指定要解析的路径文本。</param>
		/// <param name="throwException">指定无效的路径文本是否激发异常。</param>
		/// <param name="scheme">返回解析成功的路径对应的文件系统<see cref="IFileSystem.Scheme"/>方案。</param>
		/// <param name="segments">返回解析成功的路径节点数组，更多信息请参考<see cref="PathToken.Segments"/>属性文档。</param>
		/// <param name="anchor">返回解析成功的路径锚点。</param>
		/// <returns>如果解析成功则返回真(True)，否则返回假(False)。</returns>
		private static bool ParseCore(string text, bool throwException, out string scheme, out string[] segments, out PathAnchor anchor)
		{
			const int PATH_NONE_STATE = 0;      //状态机：初始态
			const int PATH_SLASH_STATE = 1;     //状态机：斜杠态（路径分隔符）
			const int PATH_ANCHOR_STATE = 2;    //状态机：锚点态
			const int PATH_SEGMENT_STATE = 3;   //状态机：内容态

			scheme = null;
			segments = null;
			anchor = PathAnchor.None;

			if(string.IsNullOrEmpty(text))
			{
				if(throwException)
					throw new PathException("The path text is null or empty.");

				return false;
			}

			var state = 0;
			var spaces = 0;
			var part = string.Empty;
			var parts = new List<string>();

			for(int i = 0; i < text.Length; i++)
			{
				var chr = text[i];

				switch(chr)
				{
					case ' ':
						if(state == PATH_ANCHOR_STATE && anchor == PathAnchor.Current)
						{
							if(throwException)
								throw new PathException("");

							return false;
						}

						if(part.Length > 0)
							spaces++;

						break;
					case '\t':
					case '\n':
					case '\r':
						break;
					case ':':
						//注意：当首次遇到冒号时，其为Scheme定语；否则即为普通字符
						if(parts.Count == 0)
						{
							if(string.IsNullOrEmpty(part))
							{
								if(throwException)
									throw new PathException("The scheme of path is empty.");

								return false;
							}

							//设置路径方案
							scheme = part;

							//重置空格计数器
							spaces = 0;

							//重置内容文本
							part = string.Empty;

							//设置当前状态为初始态
							state = PATH_NONE_STATE;
						}
						else
						{
							//跳转到默认分支，即做普通字符处理
							goto default;
						}

						break;
					case '.':
						switch(state)
						{
							case PATH_NONE_STATE:
								anchor = PathAnchor.Current;
								break;
							case PATH_ANCHOR_STATE:
								if(anchor == PathAnchor.Current)
								{
									anchor = PathAnchor.Parent;
								}
								else
								{
									if(throwException)
										throw new PathException("Invalid anchor of path.");

									return false;
								}

								break;
							default:
								goto TEXT_LABEL;
						}

						state = PATH_ANCHOR_STATE;

						break;
					case '/':
					case '\\':
						switch(state)
						{
							case PATH_NONE_STATE:
								anchor = PathAnchor.Root;
								break;
							case PATH_SLASH_STATE:
								if(throwException)
									throw new PathException("Invalid path text, it contains repeated slash character.");

								return false;
							case PATH_SEGMENT_STATE:
								if(string.IsNullOrEmpty(part))
								{
									if(throwException)
										throw new PathException("Error occurred, The path parser internal error.");

									return false;
								}

								parts.Add(part);

								break;
						}

						spaces = 0;
						part = string.Empty;
						state = PATH_SLASH_STATE;

						break;
					//注意：忽略对“?”、“*”字符的检验处理，因为需要支持对通配符模式路径的链接。
					//case '?':
					//case '*':
					case '"':
					case '|':
					case '<':
					case '>':
						if(throwException)
							throw new ArgumentException(string.Format("Invalid path, it contains '{0}' illegal character(s).", chr));

						return false;
					default:
					TEXT_LABEL:
						if(spaces > 0)
						{
							part += new string(' ', spaces);
							spaces = 0;
						}

						part += chr;
						state = PATH_SEGMENT_STATE;

						break;
				}
			}

			if(state == PATH_SEGMENT_STATE && part.Length > 0)
				parts.Add(part);

			if(parts.Count == 0 && anchor == PathAnchor.None)
			{
				if(throwException)
					throw new PathException("The path text is all whitespaces.");

				return false;
			}

			segments = new string[parts.Count + (state == PATH_SLASH_STATE && parts.Count > 0 ? 1 : 0)];

			for(var i = 0; i < parts.Count; i++)
			{
				segments[i] = parts[i];
			}

			return true;
		}

		/// <summary>
		/// 将字符串数组组合成一个路径。
		/// </summary>
		/// <param name="paths">由路径的各部分构成的数组。</param>
		/// <returns>组合后的路径。</returns>
		/// <remarks>
		///		<para>该方法支持连接字符串中相对路径的解析处理，并自动忽略每个路径节两边的空白字符。如下代码所示：</para>
		///		<code><![CDATA[
		///		PathToken.Combine(@"D:\data\images\", "avatars/001.jpg");                            // D:/data/images/avatars/001.jpg
		///		PathToken.Combine(@"D:\data\images\", "./avatars / 001.jpg");                        // D:/data/images/avatars/001.jpg
		///		PathToken.Combine(@"D:\data\images\", ".. /avatars / 001.jpg");                      // D:/data/avatars/001.jpg
		///		PathToken.Combine(@"D:\data\images\", "/avatars/001.jpg");                           // /avatars/001.jpg
		///		PathToken.Combine(@"D:\data\images\", "avatars / 001.jpg", " / final.ext");          // /final.ext
		///		PathToken.Combine(@"D:\data\images\", "avatars / 001.jpg", " / final.ext", " tail")  // /final.ext/tail
		///		PathToken.Combine(@"zfs.local:/data/images/", "./bin");                              // zfs.local:/data/images/bin
		///		PathToken.Combine(@"zfs.local:/data/images/", "../bin/Debug");                       // zfs.local:/data/bin/Debug
		///		PathToken.Combine(@"zfs.local:/data/images/", "./bin", "../bin/Debug");              // zfs.local:/data/images/bin/Debug
		///		PathToken.Combine(@"zfs.local:/data/images/", "/root");                              // /root
		///		PathToken.Combine(@"zfs.local:/data/images/", "./bin", "../bin/Debug", "/root/");    // /root/
		///		]]></code>
		/// </remarks>
		public static string Combine(params string[] paths)
		{
			if(paths == null)
				throw new ArgumentNullException(nameof(paths));

			var slashed = false;
			var segments = new List<string>();

			for(int i = 0; i < paths.Length; i++)
			{
				if(string.IsNullOrEmpty(paths[i]))
					continue;

				var segment = string.Empty;
				var spaces = 0;

				foreach(var chr in paths[i])
				{
					switch(chr)
					{
						case ' ':
							if(segment.Length > 0)
								spaces++;
							break;
						case '\t':
						case '\n':
						case '\r':
							break;
						case '/':
						case '\\':
							spaces = 0;
							slashed = true;

							switch(segment)
							{
								case "":
									segments.Clear();
									segments.Add("");
									break;
								case ".":
									break;
								case "..":
									if(segments.Count > 0)
										segments.RemoveAt(segments.Count - 1);
									break;
								default:
									if(segment.Contains(":"))
										segments.Clear();

									segments.Add(segment);
									break;
							}

							segment = string.Empty;
							break;
						//注意：忽略对“?”、“*”字符的检验处理，因为需要支持对通配符模式路径的链接。
						//case '?':
						//case '*':
						case '"':
						case '|':
						case '<':
						case '>':
							throw new ArgumentException("Invalid path, it contains a illegal character.");
						default:
							if(spaces > 0)
							{
								segment += new string(' ', spaces);
								spaces = 0;
							}

							segment += chr;
							slashed = false;
							break;
					}
				}

				if(segment.Length > 0)
				{
					switch(segment)
					{
						case ".":
							break;
						case "..":
							if(segments.Count > 0)
								segments.RemoveAt(segments.Count - 1);
							break;
						default:
							if(segment.Contains(":"))
								segments.Clear();

							segments.Add(segment);
							break;
					}
				}
			}

			return (segments.Count == 0 ? string.Empty : string.Join("/", segments)) + (slashed ? "/" : "");
		}
		#endregion

		#region 私有方法
		private static string GetAnchorString(PathAnchor anchor, bool slashed)
		{
			switch(anchor)
			{
				case PathAnchor.Root:
					return "/";
				case PathAnchor.Current:
					return slashed ? "./" : ".";
				case PathAnchor.Parent:
					return slashed ? "../" : "..";
				default:
					return string.Empty;
			}
		}

		private static (PathToken? path, string message) ParseCore(string text)
		{
			PathAnchor anchor = PathAnchor.None;
			string scheme = null;
			IList<string> segments = null;

			var context = new PathContext(text.AsSpan());

			for(int i = 0; i < text.Length; i++)
			{
				context.Move(i);

				switch(context.State)
				{
					case PathState.Error:
						return (null, context.ErrorMessage);
					case PathState.None:
						DoNone(context, out anchor, out scheme, out string part);

						if(!string.IsNullOrEmpty(part))
						{
							if(segments == null)
								segments = new List<string>();

							segments.Add(part);
						}

						break;
					case PathState.Anchor:
						DoAnchor(context, out anchor);
						break;
					case PathState.Scheme:
						DoScheme(context, out anchor);
						break;
					case PathState.Segment:
						DoSegment(context, out var segment);

						if(segment != null && segment.Length > 0)
							segments.Add(segment);

						break;
				}
			}

			return (new PathToken(scheme, anchor, segments.ToArray()), null);
		}

		private static void DoNone(PathContext context, out PathAnchor anchor, out string scheme, out string segment)
		{
			anchor = PathAnchor.None;
			scheme = null;
			segment = null;

			ReadOnlySpan<char> part;

			switch(context.Character)
			{
				case '/':
				case '\\':
					context.Reset(PathState.Segment, out part);

					if(part.IsEmpty)
						anchor = PathAnchor.Root;
					else if(part == ".")
						anchor = PathAnchor.Current;
					else if(part == "..")
						anchor = PathAnchor.Parent;
					else
						segment = part.ToString();

					return;
				case ':':
					context.Reset(PathState.Scheme, out part);

					if(part.IsEmpty)
						context.Error($"");
					else
						scheme = part.ToString();
					return;
				case '.':
					part = context.Get();

					if(part.IsEmpty)
						context.Accept(PathState.Anchor);

					return;
				default:
					if(!context.IsLetterOrDigit && context.Character != '_' && context.Character != '-')
						context.Error($"");

					return;
			}
		}

		private static void DoAnchor(PathContext context, out PathAnchor anchor)
		{
			switch(context.Character)
			{
				case '.':
					anchor = PathAnchor.Parent;
					context.Accept(PathState.Anchor, out var count);

					if(count > 2)
						context.Error($"");

					return;
				case '/':
				case '\\':
					context.Reset(PathState.Segment, out var part);
					anchor = part.Length switch { 1 => PathAnchor.Current, 2 => PathAnchor.Parent, _ => PathAnchor.None };
					return;
				default:
					anchor = PathAnchor.None;
					context.Error($"");
					return;
			}
		}

		private static void DoScheme(PathContext context, out PathAnchor anchor)
		{
			anchor = PathAnchor.None;

			switch(context.Character)
			{
				case '/':
				case '\\':
					anchor = PathAnchor.Root;
					context.Reset(PathState.Segment);
					return;
				case '.':
					context.Accept(PathState.Anchor);
					return;
				default:
					if(context.IsLetterOrDigit || context.Character == '_' || context.Character == '-')
						context.Accept(PathState.Segment);
					else
						context.Error($"");
					return;
			}
		}

		private static void DoSegment(PathContext context, out string segment)
		{
			segment = null;

			switch(context.Character)
			{
				case '/':
				case '\\':
					context.Reset(PathState.Segment, out var part);

					if(part.IsEmpty)
						context.Error($"");

					segment = part.ToString();
					break;
				default:
					if(context.IsInvalid)
						context.Error($"");
					else
						context.Accept(PathState.Segment);
					break;
			}
		}
		#endregion

		private ref struct PathContext
		{
			#region 私有字段
			private readonly ReadOnlySpan<char> _text;
			private PathState _state;
			private char _character;
			private int _index;
			private int _count;
			private int _whitespaces;
			private string _errorMessage;
			#endregion

			#region 构造函数
			public PathContext(ReadOnlySpan<char> text)
			{
				_text = text;
				_state = PathState.None;
				_index = 0;
				_character = '\0';
				_count = 0;
				_whitespaces = 0;
				_errorMessage = null;
			}
			#endregion

			#region 公共属性
			public PathState State { get => _state; }
			public int Index { get => _index; }
			public char Character { get => _character; }
			public string ErrorMessage { get => _errorMessage; }
			public bool IsWhitespace { get => char.IsWhiteSpace(_character); }
			public bool IsLetter { get => char.IsLetter(_character); }
			public bool IsDigit { get => char.IsDigit(_character); }
			public bool IsLetterOrDigit { get => char.IsLetterOrDigit(_character); }
			public bool IsInvalid { get => System.IO.Path.GetInvalidPathChars().Contains(_character); }
			#endregion

			#region 公共方法
			public bool Move(int index)
			{
				if(index >= 0 && index < _text.Length)
				{
					_index = index;
					_character = _text[index];
					return true;
				}

				_index = _text.Length;
				_character = '\0';
				return false;
			}

			public void Error(string message)
			{
				_state = PathState.Error;
				_errorMessage = message;
			}

			public ReadOnlySpan<char> Get()
			{
				return _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;
			}

			public void Reset(PathState state)
			{
				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Reset(PathState state, out ReadOnlySpan<char> value)
			{
				value = _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;

				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Reset(out ReadOnlySpan<char> value)
			{
				value = _count > 0 ? _text.Slice(_index - _count - _whitespaces, _count) : ReadOnlySpan<char>.Empty;

				_count = 0;
				_whitespaces = 0;
			}

			public void Accept(PathState state)
			{
				this.Accept(state, out _);
			}

			public void Accept(PathState state, out int count)
			{
				if(char.IsWhiteSpace(_character))
				{
					if(_count > 0)
						_whitespaces++;
				}
				else
				{
					_count += _whitespaces + 1;
					_whitespaces = 0;
				}

				_state = state;
				count = _count;
			}
			#endregion
		}

		private enum PathState
		{
			None,
			Error,
			Anchor,
			Scheme,
			Segment,
		}
	}
}
