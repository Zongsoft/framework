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

namespace Zongsoft.Configuration.Profiles;

/// <summary>提供一次导入通知的上下文信息。</summary>
/// <remarks>导入前后分别创建上下文；属性引用的配置模型仍可修改。</remarks>
public sealed class ProfileContext
{
	#region 构造函数
	internal ProfileContext(string filePath, int depth, Profile referer, Profile profile = null)
	{
		this.FilePath = filePath;
		this.Depth = depth;
		this.Referer = referer;
		this.Profile = profile;
	}
	#endregion

	#region 公共属性
	/// <summary>获取本次导入文件规范化后的绝对加载路径。</summary>
	public string FilePath { get; }

	/// <summary>获取当前加载层数；根文件为第一层，直接导入为第二层。</summary>
	public int Depth { get; }

	/// <summary>获取包含本次导入声明的直接引用者。</summary>
	public Profile Referer { get; }

	/// <summary>获取本次导入结果；导入前为空(<c>null</c>)，导入后为已解析并合并到引用者的配置。</summary>
	public Profile Profile { get; }
	#endregion
}
