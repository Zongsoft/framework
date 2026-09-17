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

internal sealed class ProfileReader
{
	#region 成员字段
	private readonly HashSet<string> _active = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
	private readonly List<string> _paths = [];
	#endregion

	#region 构造函数
	public ProfileReader(ProfileOptions options)
	{
		//所有递归导入共享此快照，不受调用方随后修改原选项的影响。
		this.Options = options?.Clone() ?? new ProfileOptions(false);
	}
	#endregion

	#region 公共属性
	public ProfileOptions Options { get; }
	#endregion

	#region 读取方法
	public Profile Read(string path) => this.ReadFile(path, null);
	public Profile Read(Stream stream, Encoding encoding) => this.ReadCore(stream, encoding, stream is FileStream file ? file.Name : string.Empty, null);
	#endregion

	#region 私有方法
	private Profile ReadFile(string path, Context context)
	{
		if(string.IsNullOrWhiteSpace(path))
			throw new ArgumentNullException(nameof(path));

		if(context != null && !Path.IsPathFullyQualified(path))
		{
			if(string.IsNullOrEmpty(context.Profile.FilePath))
				throw new ProfileException(string.Format(Properties.Resources.Profiles_RelativeImportRequiresFile_Message, path, context.LineNumber + 1));

			path = Path.Combine(Path.GetDirectoryName(context.Profile.FilePath), path);
		}

		path = Path.GetFullPath(path);
		FileStream stream;

		//仅在打开阶段忽略可选导入的缺失文件；解析和回调抛出的同类异常必须传播。
		try
		{
			stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
		catch(FileNotFoundException) when(context != null)
		{
			return null;
		}
		catch(DirectoryNotFoundException) when(context != null)
		{
			return null;
		}

		return this.ReadCore(stream, null, path, context);
	}

	private Profile ReadCore(Stream stream, Encoding encoding, string path, Context context)
	{
		using(stream)
		{
			var identity = string.IsNullOrEmpty(path) ? null : ProfileUtility.GetIdentity(path);

			//入栈前检查层数；根文件也占一层，被拒绝的文件不触发导入通知。
			if(_paths.Count >= this.Options.MaximumDepth)
				throw this.CreateException(Properties.Resources.Profiles_MaximumDepth_Message, path, context);

			//只检测当前活动链，允许已经退出活动链的文件再次导入。
			if(identity != null && !_active.Add(identity))
				throw this.CreateException(Properties.Resources.Profiles_CircularImport_Message, path, context);

			_paths.Add(path);

			try
			{
				//根读取没有引用者；前后上下文分别创建，保留的前置上下文不会被更新。
				if(context != null)
					this.Options.Importing?.Invoke(new ProfileContext(path, _paths.Count, context.Profile));

				var profile = this.Parse(stream, encoding);

				if(context != null)
				{
					//先合并并登记来源，再通知完成；回调失败不回滚已发生的合并。
					context.Profile.Import(profile);
					this.Options.Imported?.Invoke(new ProfileContext(path, _paths.Count, context.Profile, profile));
				}

				return profile;
			}
			finally
			{
				//解析、合并或通知失败均须退出活动链，使同一读取器能够重试。
				_paths.RemoveAt(_paths.Count - 1);

				if(identity != null)
					_active.Remove(identity);
			}
		}
	}

	private Profile Parse(Stream stream, Encoding encoding)
	{
		List<int> blanks = [];
		Profile profile = new Profile(stream is FileStream fileStream ? fileStream.Name : string.Empty);
		var context = new Context(profile);
		var options = this.Options;

		profile.BeginRead();

		using(var reader = new StreamReader(stream, encoding ?? Encoding.UTF8))
		{
			string text;
			context.LineNumber = 0;

			while((text = reader.ReadLine()) != null)
			{
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

						if(ProfileUtility.TryGetImport(content, out var argument))
						{
							foreach(var path in argument.Split([' ', '\t', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
								this.ReadFile(path, context);
						}

						break;
				}

				//递增行号
				context.LineNumber++;
			}
		}

		//更新配置文件中的空行集
		profile.CompleteRead([.. blanks]);

		//返回加载成功的配置文件
		return profile;
	}

	private ProfileException CreateException(string message, string path, Context context) => new(string.Format(
		message, string.Join(" -> ", _paths) + " -> " + path, context?.Profile.FilePath, (context?.LineNumber ?? -1) + 1));
	#endregion

	#region 嵌套类型
	private sealed class Context(Profile profile)
	{
		public Profile Profile { get; } = profile;
		public int LineNumber { get; set; }
		public ProfileSection Section { get; set; }
	}
	#endregion
}
