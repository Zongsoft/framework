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

/// <summary>表示Web宿主的接口。</summary>
public interface IWebHost
{
	/// <summary>获取宿主名称。</summary>
	string Name { get; }
	/// <summary>获取宿主域名。</summary>
	string Domain { get; }
	/// <summary>获取宿主端口号。</summary>
	ushort Port { get; }
	/// <summary>获取宿主根目录。</summary>
	string RootDirectory { get; }
}
