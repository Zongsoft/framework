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

namespace Zongsoft.Web;

/// <summary>表示Web应用环境的接口。</summary>
public interface IWebEnvironment
{
	/// <summary>获取Web根目录名，默认为：<c>wwwroot</c>。</summary>
	string RootDirectory { get; }

	/// <summary>获取Web应用的默认站点。</summary>
	IWebSite Site { get; }

	/// <summary>获取Web应用的站点集合。</summary>
	IWebSiteCollection Sites { get; }
}
