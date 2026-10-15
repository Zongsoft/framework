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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Web.Hosting;

/// <summary>将路由端点约束到指定的逻辑范围及请求类别。</summary>
public sealed class WebEndpointMetadata
{
	/// <summary>初始化端点范围元数据。</summary>
	/// <param name="path">完整的字面基路径，与监听范围忽略大小写及可选尾斜杠关联。</param>
	/// <param name="kind">请求类别，默认普通 HTTP。</param>
	public WebEndpointMetadata(string path, string kind = "http")
	{
		if(!WebEndpointUtility.IsPath(path))
			throw new ArgumentException(Properties.Resources.Path_FormatInvalid_Message, nameof(path));

		this.Path = path.Length > 1 ? path.TrimEnd('/') : path;
		this.Kind = WebEndpointUtility.GetName(kind);
	}

	/// <summary>获取除根路径外不含尾斜杠的字面基路径。</summary>
	public string Path { get; }
	/// <summary>获取请求类别。</summary>
	public string Kind { get; }
}
