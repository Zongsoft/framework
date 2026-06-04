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

namespace Zongsoft.Versioning;

/// <summary>表示语义化版本的类，包含主版本号、次版本号、修订号、标签和额外信息等属性。</summary>
public partial class Version
{
	#region 构造函数
	public Version(int major, int minor, int patch, string label = null, string extra = null)
	{
		this.Major = major;
		this.Minor = minor;
		this.Patch = patch;
		this.Label = string.IsNullOrEmpty(label) ? null : label.Trim();
		this.Extra = string.IsNullOrEmpty(extra) ? null : extra.Trim();
	}
	#endregion

	#region 公共属性
	/// <summary>获取主版本号。</summary>
	public int Major { get; }
	/// <summary>获取次版本号。</summary>
	public int Minor { get; }
	/// <summary>获取修订号。</summary>
	public int Patch { get; }
	/// <summary>获取标签。</summary>
	public string Label { get; }
	/// <summary>获取额外信息。</summary>
	public string Extra { get; }
	/// <summary>获取一个值，指示当前版本是否为空。</summary>
	public bool IsEmpty => this.Major == 0 && this.Minor == 0 && this.Patch == 0 && string.IsNullOrEmpty(this.Label) && string.IsNullOrEmpty(this.Extra);
	#endregion

	#region 公共方法
	/// <summary>获取标签。</summary>
	/// <param name="label">输出参数，用于接收标签值。</param>
	/// <returns>如果当前版本有标签，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public bool HasLabel(out string label)
	{
		if(string.IsNullOrEmpty(this.Label))
		{
			label = null;
			return false;
		}

		label = this.Label;
		return true;
	}

	/// <summary>获取额外信息。</summary>
	/// <param name="extra">输出参数，用于接收额外信息值。</param>
	/// <returns>如果当前版本有额外信息，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public bool HasExtra(out string extra)
	{
		if(string.IsNullOrEmpty(this.Extra))
		{
			extra = null;
			return false;
		}

		extra = this.Extra;
		return true;
	}
	#endregion
}
