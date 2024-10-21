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
	///		<para>路径格式分为<seealso cref="Path.Scheme"/>和<seealso cref="Path.FullPath"/>这两个部分，中间使用冒号(:)分隔，路径各层级间使用正斜杠(/)进行分隔。如果是目录路径则以正斜杠(/)结尾。</para>
	///		<para>其中<seealso cref="Path.Scheme"/>可以省略，如果为目录路径，则<see cref="Path.FileName"/>属性为空或空字符串("")。常用路径示例如下：</para>
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
	public readonly struct Path : IEquatable<Path>
	{
		#region 成员字段
		private readonly string _scheme;
		private readonly string[] _segments;
		private readonly PathAnchor _anchor;
		#endregion

		#region 私有构造
		private Path(string scheme, PathAnchor anchor, string[] segments)
		{
			_scheme = scheme;
			_segments = segments;
			_anchor = anchor;
		}
		#endregion

		#region 公共属性
		/// <summary>获取路径的文件系统<see cref="IFileSystem.Scheme"/>方案。</summary>
		public string Scheme => _scheme;

		/// <summary>获取路径的锚点，即路径的起始点。</summary>
		public PathAnchor Anchor => _anchor;

		/// <summary>获取路径中的文件名，有关路径中文件名的定义请参考备注说明。</summary>
		/// <remarks>
		///		<para>路径如果以斜杠(/)结尾，则表示该路径为「目录路径」，即<see cref="FileName"/>属性为空(null)或空字符串("")；否则文件名则为<see cref="Segments"/>路径节数组中的最后一个节的内容。</para>
		/// </remarks>
		public string FileName => _segments != null && _segments.Length > 0 ? _segments[^1] : null;

		/// <summary>获取路径的完整路径（注：不含<see cref="Scheme"/>部分）。</summary>
		public string FullPath => GetAnchorString(_anchor, true) + (_segments == null || _segments.Length == 0 ? null : string.Join('/', _segments));

		/// <summary>获取路径的完整URL，该属性值包含<see cref="Scheme"/>和<see cref="FullPath"/>。</summary>
		/// <remarks>
		///		<para>如果<see cref="Scheme"/>为空(null)或空字符串("")，则<see cref="Url"/>与<see cref="FullPath"/>属性值相同。</para>
		/// </remarks>
		public string Url => string.IsNullOrEmpty(_scheme) ? this.FullPath : _scheme + ':' + this.FullPath;

		/// <summary>获取一个值，指示是否含有<see cref="Segments"/>路径节。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Zongsoft.Serialization.SerializationMember(Ignored = true)]
		public bool HasSegments => _segments != null && _segments.Length > 0;

		/// <summary>获取路径中各节点数组，更多内容请参考备注说明。</summary>
		/// <remarks>
		///		<para>如果当前路径是一个「文件路径」，即<see cref="IsFile"/>属性为真(True)，则该数组的最后一个元素内容就是<see cref="FileName"/>的值，亦文件路径的<see cref="Segments"/>不可能为空数组，因为它至少包含一个为文件名的元素。</para>
		///		<para>如果当前路径是一个「目录路径」，即<see cref="IsDirectory"/>属性为真(True)，并且不是空目录，则该数组的最后一个元素值为空(null)或空字符串("")。所谓“空目录”的示例如下：</para>
		/// </remarks>
		[System.Text.Json.Serialization.JsonIgnore]
		[Zongsoft.Serialization.SerializationMember(Ignored = true)]
		public string[] Segments => _segments;

		/// <summary>获取一个值，指示当前路径是否为文件路径。如果返回真(True)，即表示<see cref="FileName"/>有值。</summary>
		/// <remarks>
		///		<para>路径如果不是以斜杠(/)结尾，则表示该路径为「文件路径」，文件路径中的<see cref="FileName"/>即为<see cref="Segments"/>数组中最后一个元素的值。</para>
		/// </remarks>
		public bool IsFile => _segments != null && _segments.Length > 0 && !string.IsNullOrEmpty(_segments[^1]);

		/// <summary>获取一个值，指示当前路径是否为目录路径。有关「目录路径」定义请参考备注说明。</summary>
		/// <remarks>
		///		<para>路径如果以斜杠(/)结尾，则表示该路径为「目录路径」，即<see cref="FileName"/>属性为空(null)或空字符串("")；否则文件名则为<see cref="Segments"/>路径节数组中的最后一个节的内容。</para>
		/// </remarks>
		public bool IsDirectory => _segments == null || _segments.Length == 0 || string.IsNullOrEmpty(_segments[^1]);
		#endregion

		#region 公共方法
		/// <summary>获取路径的完整URL，包含 <see cref="Scheme"/> 部分。</summary>
		/// <remarks>
		///		<para>如果<see cref="Scheme"/>为空(null)或空字符串("")，则<see cref="Url"/>与<see cref="FullPath"/>属性值相同。</para>
		/// </remarks>
		public string GetUrl() => string.IsNullOrEmpty(_scheme) ? this.FullPath : _scheme + ':' + this.FullPath;

		/// <summary>获取路径的完整路径（注：不含<see cref="Scheme"/>部分）。</summary>
		public string GetFullPath() => GetAnchorString(_anchor, true) + (_segments == null || _segments.Length == 0 ? null : string.Join('/', _segments));

		/// <summary>获取路径的目录地址（注：不含<see cref="Scheme"/>部分）。</summary>
		public string GetDirectory() => GetAnchorString(_anchor, true) +
		(
			this.IsFile ?
			string.Join('/', _segments, 0, _segments.Length - 1) + "/" :
			(_segments == null ? string.Empty : string.Join('/', _segments))
		);

		/// <summary>获取目录地址URL，包含 <see cref="Scheme"/> 部分。</summary>
		public string GetDirectoryUrl() => string.IsNullOrEmpty(_scheme) ? this.GetDirectory() : _scheme + ':' + this.GetDirectory();
		#endregion

		#region 重写方法
		public bool Equals(Path path)
		{
			if(_anchor == path._anchor && string.Equals(_scheme, path._scheme, StringComparison.OrdinalIgnoreCase))
			{
				if(_segments == null || _segments.Length == 0)
					return path._segments == null || path._segments.Length == 0;

				return _segments.Length == path._segments.Length && _segments.SequenceEqual(path._segments);
			}

			return false;
		}

		public override bool Equals(object obj) => obj is Path path && this.Equals(path);
		public override int GetHashCode() => HashCode.Combine(string.IsNullOrEmpty(_scheme) ? null : _scheme.ToLowerInvariant(), _anchor, string.Join('/', _segments));
		public override string ToString() => this.Url;
		#endregion

		#region 符号重写
		public static bool operator ==(Path left, Path right) => left.Equals(right);
		public static bool operator !=(Path left, Path right) => !(left == right);
		#endregion

		#region 静态方法
		/// <summary>解析路径。</summary>
		/// <param name="text">要解析的路径文本。</param>
		/// <returns>返回解析成功的<see cref="Path"/>路径对象。</returns>
		/// <exception cref="PathException">当<paramref name="text"/>参数为无效的路径格式。</exception>
		public static Path Parse(string text)
		{
			if(string.IsNullOrEmpty(text))
				throw new ArgumentNullException(text);

			var (path, message) = ParseCore(text);

			if(path.HasValue)
				return path.Value;

			throw new PathException(message);
		}

		/// <summary>尝试解析路径。</summary>
		/// <param name="text">要解析的路径文本。</param>
		/// <param name="path">解析成功的<see cref="Path"/>路径对象。</param>
		/// <returns>如果解析成功则返回真(True)，否则返回假(False)。</returns>
		public static bool TryParse(string text, out Path path)
		{
			if(!string.IsNullOrEmpty(text))
			{
				var (token, _) = ParseCore(text);

				if(token.HasValue)
				{
					path = token.Value;
					return true;
				}
			}

			path = default;
			return false;
		}

		/// <summary>将字符串数组组合成一个路径。</summary>
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
									//if(segment.Contains(':'))
									//	segments.Clear();

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
							//if(segment.Contains(':'))
							//	segments.Clear();

							segments.Add(segment);
							break;
					}
				}
			}

			return (segments.Count == 0 ? string.Empty : string.Join("/", segments)) + (slashed ? "/" : "");
		}
		#endregion

		#region 私有方法
		private static string GetAnchorString(PathAnchor anchor, bool slashed) => anchor switch
		{
			PathAnchor.Root => "/",
			PathAnchor.Current => slashed ? "./" : ".",
			PathAnchor.Parent => slashed ? "../" : "..",
			PathAnchor.Application => (Services.ApplicationContext.Current?.ApplicationPath ?? "~") + (slashed ? "/" : null),
			_ => string.Empty,
		};
		#endregion

		#region 解析方法
		private static (Path? path, string message) ParseCore(string text)
		{
			PathAnchor anchor = PathAnchor.None, anchorValue;
			string scheme = null;
			IList<string> segments = null;

			var context = new PathContext(text.AsSpan());

			while(context.Move())
			{
				switch(context.State)
				{
					case PathState.None:
						if(DoNone(ref context, out anchorValue))
							anchor = anchorValue;

						break;
					case PathState.First:
						if(DoFirst(ref context, out var first))
						{
							if(first.Type == First.FirstType.Scheme)
							{
								scheme = first.Text;

								if(first.Letter != '\0')
								{
									if(segments == null)
										segments = new List<string>();

									anchor = PathAnchor.Root;
									segments.Add(first.Letter.ToString());
								}
							}
							else
							{
								if(segments == null)
									segments = new List<string>();

								segments.Add(first.Text);
							}
						}

						break;
					case PathState.Slash:
						DoSlash(ref context);
						break;
					case PathState.Anchor:
						if(DoAnchor(ref context, out anchorValue))
							anchor = anchorValue;

						break;
					case PathState.Origin:
						if(DoOrigin(ref context, out anchorValue))
							anchor = anchorValue;

						break;
					case PathState.Segment:
						if(DoSegment(ref context, out var segment))
						{
							if(segments == null)
								segments = new List<string>();

							segments.Add(segment);
						}

						break;
				}

				if(context.HasError(out var message))
					return new(null, message);
			}

			if(context.HasTail(out var tail))
			{
				if(segments == null)
					segments = new List<string>();

				segments.Add(tail.ToString());
			}

			return (new Path(scheme, anchor, segments?.ToArray()), null);
		}

		private static bool DoNone(ref PathContext context, out PathAnchor anchor)
		{
			switch(context.Character)
			{
				case '/':
				case '\\':
					anchor = PathAnchor.Root;
					context.Reset(PathState.Segment);
					return true;
				case '.':
					anchor = PathAnchor.Current;
					context.Accept(PathState.Anchor);
					return true;
				case '~':
					anchor = PathAnchor.Application;
					context.Accept(PathState.Anchor);
					return true;
				default:
					if(context.IsLetterOrDigit || context.Character == '_')
						context.Accept(PathState.First);
					else
						context.Error($"The first character must be a letter, number or underscore.");

					anchor = PathAnchor.None;
					return false;
			}
		}

		private static bool DoAnchor(ref PathContext context, out PathAnchor anchor)
		{
			switch(context.Character)
			{
				case '.':
					anchor = PathAnchor.Parent;
					context.Accept(PathState.Anchor, out var count);

					if(count > 2)
						context.Error($"Invalid path anchor.");

					return true;
				case '/':
				case '\\':
					context.Reset(PathState.Segment, out var part);

					anchor = part.Length switch
					{
						1 => part[0] switch
						{
							'.' => PathAnchor.Current,
							'~' => PathAnchor.Application,
							_ => PathAnchor.None,
						},
						2 => PathAnchor.Parent,
						_ => PathAnchor.None,
					};

					return true;
				default:
					anchor = PathAnchor.None;
					context.Error($"The path separator must be followed by the path anchor.");
					return false;
			}
		}

		private static bool DoFirst(ref PathContext context, out First result)
		{
			ReadOnlySpan<char> part;

			switch(context.Character)
			{
				case ':':
					context.TryGet(out part);

					//如果首节只有一个字符则当作本地文件系统的盘符处理
					if(part.Length == 1)
						result = new First(LocalFileSystem.Instance.Scheme, First.FirstType.Scheme, part[0]);
					else
						result = new First(part.ToString(), First.FirstType.Scheme);

					context.Reset(PathState.Origin);
					return true;
				case '/':
				case '\\':
					context.Reset(PathState.Segment, out part);
					result = new First(part.ToString(), First.FirstType.Segment);
					return true;
				default:
					if(context.IsLetterOrDigit || context.Character == '_' || context.Character == '-' || context.Character == '.')
						context.Accept();
					else
						context.Error($"The illegal character ‘{context.Character}’ is located at the {context.Index} character in the path.");

					result = default;
					return false;
			}
		}

		private static void DoSlash(ref PathContext context)
		{
			if(context.IsWhitespace || context.IsSlash)
				return;

			if(context.IsInvalid)
				context.Error($"The illegal character ‘{context.Character}’ is located at the {context.Index} character in the path.");
			else
				context.Accept(PathState.Segment);
		}

		private static bool DoOrigin(ref PathContext context, out PathAnchor anchor)
		{
			switch(context.Character)
			{
				case '/':
				case '\\':
					anchor = PathAnchor.Root;
					context.Reset(PathState.Segment);
					return true;
				case '.':
					anchor = PathAnchor.Current;
					context.Accept(PathState.Anchor);
					return false;
				default:
					if(context.IsInvalid)
						context.Error($"The illegal character ‘{context.Character}’ is located at the {context.Index} character in the path.");
					else if(!context.IsWhitespace)
						context.Accept(PathState.Segment);

					anchor = PathAnchor.None;
					return false;
			}
		}

		private static bool DoSegment(ref PathContext context, out string segment)
		{
			switch(context.Character)
			{
				case '/':
				case '\\':
					context.Reset(PathState.Slash, out var part);
					segment = part.ToString();
					return true;
				default:
					if(context.IsInvalid)
						context.Error($"The illegal character ‘{context.Character}’ is located at the {context.Index} character in the path.");
					else
						context.Accept();

					segment = null;
					return false;
			}
		}
		#endregion

		#region 嵌套结构
		private enum PathState
		{
			None,
			First,
			Slash,
			Origin,
			Anchor,
			Segment,
		}

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
			public bool IsLetter { get => char.IsLetter(_character); }
			public bool IsDigit { get => char.IsDigit(_character); }
			public bool IsLetterOrDigit { get => char.IsLetterOrDigit(_character); }
			public bool IsWhitespace { get => char.IsWhiteSpace(_character); }
			public bool IsSlash { get => _character == '/' || _character == '\\'; }
			public bool IsInvalid { get => System.IO.Path.GetInvalidPathChars().Contains(_character); }
			#endregion

			#region 公共方法
			public bool Move()
			{
				if(_index < _text.Length)
				{
					_character = _text[_index++];
					return true;
				}

				_character = '\0';
				return false;
			}

			public void Error(string message)
			{
				_errorMessage = message;
			}

			public bool HasError(out string message)
			{
				message = _errorMessage;
				return message != null;
			}

			public bool HasTail(out ReadOnlySpan<char> value)
			{
				if(_state == PathState.Slash)
				{
					value = ReadOnlySpan<char>.Empty;
					return true;
				}

				if(_count > 0 && (_state == PathState.First || _state == PathState.Segment))
				{
					value = _text.Slice(_index - _count - _whitespaces, _count);
					return true;
				}

				value = ReadOnlySpan<char>.Empty;
				return false;
			}

			public bool TryGet(out ReadOnlySpan<char> value)
			{
				if(_count > 0)
				{
					value = _text.Slice(_index - _count - _whitespaces - 1, _count);
					return true;
				}

				value = ReadOnlySpan<char>.Empty;
				return false;
			}

			public void Reset(PathState state)
			{
				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Reset(PathState state, out ReadOnlySpan<char> value)
			{
				value = _count > 0 ? _text.Slice(_index - _count - _whitespaces - 1, _count) : ReadOnlySpan<char>.Empty;

				_count = 0;
				_whitespaces = 0;
				_state = state;
			}

			public void Accept(PathState? state = null)
			{
				this.Accept(state, out _);
			}

			public void Accept(PathState? state, out int count)
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

				if(state.HasValue)
					_state = state.Value;

				count = _count;
			}
			#endregion
		}

		private ref struct First
		{
			public readonly FirstType Type;
			public readonly string Text;
			public readonly char Letter;

			public First(string text, FirstType type, char letter = '\0')
			{
				this.Text = text;
				this.Type = type;
				this.Letter = letter;
			}

			public enum FirstType
			{
				Scheme,
				Segment,
			}
		}
		#endregion
	}
}
