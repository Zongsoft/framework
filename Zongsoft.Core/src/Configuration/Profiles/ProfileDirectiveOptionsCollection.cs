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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration.Profiles;

/// <summary>表示按指令名称索引的选项集合，名称忽略大小写且不可重复。</summary>
/// <remarks>集合顺序不影响指令执行顺序；未配置的指令采用内置默认设置。</remarks>
public class ProfileDirectiveOptionsCollection : KeyedCollection<string, ProfileDirectiveOptions>
{
	#region 构造函数
	internal ProfileDirectiveOptionsCollection(ProfileOptions options) : base(StringComparer.OrdinalIgnoreCase) => this.Options = options ?? throw new ArgumentNullException(nameof(options));
	#endregion

	#region 公共属性
	/// <summary>获取本集合所属的配置选项。</summary>
	public ProfileOptions Options { get; }
	/// <summary>获取或设置指令处理前的回调，可改写参数或通过 Handled 接管执行。</summary>
	/// <remarks>忽略或禁止的指令不触发此回调。</remarks>
	public Action<ProfileDirectiveContext> Processing { get; set; }
	/// <summary>获取或设置指令成功处理后的回调，包括递归读取与合并。</summary>
	/// <remarks>处理失败时不触发；回调中的参数修改不会重新执行指令。</remarks>
	public Action<ProfileDirectiveContext> Processed { get; set; }
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(ProfileDirectiveOptions item) => item.Name;
	protected override void InsertItem(int index, ProfileDirectiveOptions item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.InsertItem(index, item);
	}

	protected override void SetItem(int index, ProfileDirectiveOptions item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.SetItem(index, item);
	}
	#endregion
}
