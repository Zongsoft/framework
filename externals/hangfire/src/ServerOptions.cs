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

namespace Zongsoft.Externals.Hangfire;

public class ServerOptions
{
	internal const string PATH = "Externals/Hangfire/Server";

	public string[] Queues { get; set; }
	public int WorkerCount { get; set; }
	public TimeSpan StopTimeout { get; set; }
	public TimeSpan ShutdownTimeout { get; set; }
	public TimeSpan ScheduleInterval { get; set; }
	public TimeSpan HeartbeatInterval { get; set; }
	public TimeSpan CheckInterval { get; set; }
	public TimeSpan ServerTimeout { get; set; }
}
