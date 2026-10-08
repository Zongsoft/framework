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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

/// <summary>解析单个来源的 INI 声明，指令交由当前读取会话处理。</summary>
internal sealed class ProfileReader(ProfileReader.Session session)
{
	#region 成员字段
	private readonly Session _session = session;
	#endregion

	#region 读取方法
	public Profile Read(TextReader reader, string path, Profile referer)
	{
		List<int> blanks = [];
		Profile profile = new Profile(path);
		var context = new Context();
		var options = _session.Options;

		profile.BeginRead();

		string text;
		context.LineNumber = 0;

		while((text = reader.ReadLine()) != null)
		{
			if(context.LineNumber == 0 && text.Length > 0 && text[0] == '\uFEFF')
				text = text[1..];

			//解析读取到的行文本
			switch(ProfileUtility.ParseLine(text, out var content))
			{
				case ProfileUtility.LineType.Blank:
					if(options != null && options.PreserveBlanks)
						blanks.Add(context.LineNumber);
					break;
				case ProfileUtility.LineType.Section:
					var parts = content.Split(' ', '\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

					if(parts == null || parts.Length == 0)
						context.Section = null;
					else
					{
						var sections = profile.Sections;

						for(int i = 0; i < parts.Length; i++)
						{
							if(sections.TryGetValue(parts[i], out var section))
								context.Section = section;
							else
								context.Section = sections.GetOrAdd(parts[i], context.LineNumber);

							sections = context.Section.Sections;
						}
					}

					profile.DeclareSection(context.Section, context.LineNumber, true);
					break;
				case ProfileUtility.LineType.Entry:
					var index = content.IndexOf('=');

					if(context.Section == null)
					{
						if(index < 0)
							profile.Entries.AddParsed(context.LineNumber, content);
						else
							profile.Entries.AddParsed(context.LineNumber, content[..index], content[(index + 1)..]);
					}
					else
					{
						if(index < 0)
							context.Section.Entries.AddParsed(context.LineNumber, content);
						else
							context.Section.Entries.AddParsed(context.LineNumber, content[..index], content[(index + 1)..]);
					}

					break;
				case ProfileUtility.LineType.Comment:
					var comments = context.Section == null ? profile.Comments : context.Section.Comments;
					comments.AddParsed(content, context.LineNumber);

					if(ProfileUtility.TryGetDirective(content, out var name, out var argument))
						_session.ProcessDirective(name, argument, profile, referer, context.Section, context.LineNumber + 1);

					break;
			}

			//递增行号
			context.LineNumber++;
		}

		//更新配置文件中的空行集
		profile.CompleteRead([.. blanks]);

		//返回加载成功的配置文件
		return profile;
	}
	#endregion

	#region 嵌套类型
	/// <summary>管理一次根加载及递归读取的选项、指令快照和活动来源。</summary>
	internal sealed class Session
	{
		#region 成员字段
		private readonly List<string> _paths = [];
		private readonly Dictionary<string, ProfileDirectiveBase> _directives;
		private readonly HashSet<string> _active = new(ProfileUtility.PathComparer);
		#endregion

		#region 构造函数
		public Session(ProfileOptions options)
		{
			//在任何用户回调或选项克隆扩展执行之前固定本次指令集合。
			_directives = Profile.Directives.Snapshot();
			this.Options = options?.Clone() ?? new ProfileOptions(false);
		}
		#endregion

		#region 公共属性
		public ProfileOptions Options { get; }
		#endregion

		#region 读取方法
		public Profile Read(string path) => this.Read(new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read), null);
		public Profile Read(TextReader reader) => this.ReadCore(reader, reader is StreamReader { BaseStream: FileStream file } ? file.Name : string.Empty, null, 0, 0);
		public Profile Read(Stream stream, Encoding encoding, Profile referer = null, int lineNumber = 0, int maximumDepth = 0)
		{
			using(stream)
			{
				var path = stream is FileStream file ? file.Name : string.Empty;
				using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8);
				return this.ReadCore(reader, path, referer, lineNumber, maximumDepth);
			}
		}
		#endregion

		#region 指令处理
		public void ProcessDirective(string name, string argument, Profile profile, Profile referer, ProfileSection section, int lineNumber)
		{
			var options = this.Options.Directives.TryGetValue(name, out var configured) ? configured : new ProfileDirectiveOptions(name);

			if(options.Behavior == ProfileDirectiveBehavior.Ignore)
				return;
			if(options.Behavior == ProfileDirectiveBehavior.Suppress)
				throw new ProfileException(string.Format(Properties.Resources.Profiles_DirectiveSuppressed_Message, name, profile.FilePath, lineNumber));

			var context = new ProfileDirectiveContext(this, name, argument, profile, referer, section, lineNumber, _paths.Count, options);
			this.Options.Directives.Processing?.Invoke(context);

			if(!context.Handled)
			{
				if(_directives.TryGetValue(name, out var directive))
				{
					directive.Process(context);
					context.Handled = true;
				}
				else if(context.Behavior == ProfileDirectiveBehavior.Strict)
					throw new ProfileException(string.Format(Properties.Resources.Profiles_DirectiveUnknown_Message, name, profile.FilePath, lineNumber));
			}

			this.Options.Directives.Processed?.Invoke(context);
		}
		#endregion

		#region 私有方法
		private Profile ReadCore(TextReader reader, string path, Profile referer, int lineNumber, int maximumDepth)
		{
			var identity = string.IsNullOrEmpty(path) ? null : ProfileUtility.GetIdentity(path);

			if(maximumDepth > 0 && _paths.Count >= maximumDepth)
				throw CreateException(Properties.Resources.Profiles_MaximumDepth_Message, path, referer, lineNumber, _paths);

			//只检测活动链，已经完成的来源可以重复读取。
			if(identity != null && !_active.Add(identity))
				throw CreateException(Properties.Resources.Profiles_CircularImport_Message, path, referer, lineNumber, _paths);

			_paths.Add(path);

			try
			{
				this.Options.Loading?.Invoke(new ProfileContext(path, _paths.Count, referer));

				var profile = new ProfileReader(this).Read(reader, path, referer);
				referer?.Import(profile);

				this.Options.Loaded?.Invoke(new ProfileContext(path, _paths.Count, referer, profile));
				return profile;
			}
			finally
			{
				_paths.RemoveAt(_paths.Count - 1);

				if(identity != null)
					_active.Remove(identity);
			}

			static ProfileException CreateException(string message, string path, Profile referer, int lineNumber, IEnumerable<string> paths) => new(string.Format(message, string.Join(" -> ", paths) + " -> " + path, referer?.FilePath, lineNumber));
		}
		#endregion
	}

	private sealed class Context
	{
		public int LineNumber { get; set; }
		public ProfileSection Section { get; set; }
	}
	#endregion
}
