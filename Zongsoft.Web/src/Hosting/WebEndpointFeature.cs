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

/// <summary>保存请求进入管线时确定的逻辑端点与原始路径。</summary>
public sealed class WebEndpointFeature
{
	internal WebEndpointFeature(WebEndpoint endpoint, string path, WebEndpointProtocol protocol)
	{
		this.Endpoint = endpoint;
		this.Path = path;
		this.Protocol = protocol;
	}

	/// <summary>获取唯一选定的逻辑端点。</summary>
	public WebEndpoint Endpoint { get; }
	/// <summary>获取原始请求路径，不包含查询字符串，不进行解码或点段消解。</summary>
	public string Path { get; }
	/// <summary>获取本次请求的类别。</summary>
	public string Kind => this.Protocol.Kind;

	internal WebEndpointProtocol Protocol { get; }
}
