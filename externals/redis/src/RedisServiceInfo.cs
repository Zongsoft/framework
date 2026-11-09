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

namespace Zongsoft.Externals.Redis;

public class RedisServiceInfo
{
	#region 构造函数
	public RedisServiceInfo(string name, string @namespace, int databaseId, Zongsoft.Configuration.IConnectionSettings settings)
	{
		this.Name = name;
		this.Namespace = @namespace;
		this.DatabaseId = databaseId;
		this.Settings = settings;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Namespace { get; }
	public int DatabaseId { get; }
	public Zongsoft.Configuration.IConnectionSettings Settings { get; }
	public RedisServerDescriptor[] Servers { get; internal set; }
	/// <summary>获取所有相关主节点均支持的 Redis 能力。</summary>
	public RedisCapabilities Capabilities { get; internal set; }
	#endregion
}
