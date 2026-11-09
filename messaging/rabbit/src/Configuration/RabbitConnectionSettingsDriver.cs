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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ.Configuration;

public sealed class RabbitConnectionSettingsDriver : ConnectionSettingsDriver<RabbitConnectionSettings>
{
	#region 常量定义
	internal const string NAME = "RabbitMQ";
	#endregion

	#region 单例字段
	public static readonly RabbitConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private RabbitConnectionSettingsDriver() : base(NAME) { }
	#endregion
}
