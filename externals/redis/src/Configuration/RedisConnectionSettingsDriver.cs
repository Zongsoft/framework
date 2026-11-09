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

using Zongsoft.Common;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Configuration;

public sealed class RedisConnectionSettingsDriver : ConnectionSettingsDriver<RedisConnectionSettings>
{
	#region 常量定义
	internal const string NAME = "Redis";
	#endregion

	#region 单例字段
	public static readonly RedisConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private RedisConnectionSettingsDriver() : base(NAME) { }
	#endregion
}
