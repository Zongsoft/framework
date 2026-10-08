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

/// <summary>管理一次根加载及递归读取的选项、指令快照和活动来源。</summary>
internal sealed class ProfileReadSession
{
	#region 成员字段
	private readonly HashSet<string> _active = new(ProfileUtility.PathComparer);
	private readonly List<string> _paths = [];
	private readonly Dictionary<string, ProfileDirectiveBase> _directives;
	#endregion

	#region 构造函数
	public ProfileReadSession(ProfileOptions options)
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
			throw this.CreateException(Properties.Resources.Profiles_MaximumDepth_Message, path, referer, lineNumber);

		//只检测活动链，已经完成的来源可以重复读取。
		if(identity != null && !_active.Add(identity))
			throw this.CreateException(Properties.Resources.Profiles_CircularImport_Message, path, referer, lineNumber);

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
	}

	private ProfileException CreateException(string message, string path, Profile referer, int lineNumber) => new(string.Format(
		message, string.Join(" -> ", _paths) + " -> " + path, referer?.FilePath, lineNumber));
	#endregion
}
