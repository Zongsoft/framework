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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
