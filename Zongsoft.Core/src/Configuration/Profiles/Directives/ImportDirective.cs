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

namespace Zongsoft.Configuration.Profiles.Directives;

public class ImportDirective : IProfileDirective
{
	#region 单例字段
	public static readonly ImportDirective Default = new();
	#endregion

	#region 成员字段
	private readonly int _maximumDepth;
	private readonly Action<string> _importing;
	private readonly Action<Profile> _imported;
	#endregion

	#region 构造函数
	/// <summary>初始化导入指令。</summary>
	/// <param name="maximumDepth">同时加载的最大文件数，根文件计为第一层。</param>
	/// <param name="importing">文件打开且递归检查通过后、解析前的回调，参数为绝对加载路径。</param>
	/// <param name="imported">子文件解析及递归导入成功并合并后执行的回调，根文件不触发。</param>
	/// <remarks>回调异常终止加载，已发生的通知和合并不回滚。并发共享的回调应保证线程安全。</remarks>
	public ImportDirective(int maximumDepth = 64, Action<string> importing = null, Action<Profile> imported = null)
	{
		if(maximumDepth <= 0)
			throw new ArgumentOutOfRangeException(nameof(maximumDepth));

		_maximumDepth = maximumDepth;
		_importing = importing;
		_imported = imported;
	}
	#endregion

	#region 公共属性
	public string Name => "Import";
	#endregion

	#region 公共方法
	public void OnRead(ProfileReadingContext context, string argument)
	{
		if(string.IsNullOrEmpty(argument))
			return;

		var paths = argument.Split([' ', '\t', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < paths.Length; i++)
		{
			context.Reader.Read(paths[i], _maximumDepth, optional: true,
				loading: _importing,
				loaded: profile => this.Merge(context.Profile, profile),
				context: context);
		}
	}

	public void OnWrite(ProfileWritingContext context, string argument) { }
	#endregion

	#region 私有方法
	private void Merge(Profile target, Profile profile)
	{
		foreach(var item in profile.GetItems())
		{
			switch(item)
			{
				case ProfileEntry entry:
					SetEntry(target.Entries, entry);
					break;
				case ProfileSection section:
					SetSection(target.Sections, section);
					break;
			}
		}

		_imported?.Invoke(profile);
	}

	private static void SetEntry(ProfileEntryCollection entries, ProfileEntry entry)
	{
		if(entries == null || entry == null)
			return;

		if(entries.TryGetValue(entry.Name, out var found))
			found.Value = entry.Value;
		else
			entries.Add(entry);
	}

	private static void SetSection(ProfileSectionCollection sections, ProfileSection section)
	{
		if(sections == null || section == null)
			return;

		if(!sections.TryGetValue(section.Name, out var found))
			found = sections.Add(section.Name);

		foreach(var entry in section.Entries)
			SetEntry(found.Entries, entry);

		foreach(var child in section.Sections)
			SetSection(found.Sections, child);
	}
	#endregion
}
