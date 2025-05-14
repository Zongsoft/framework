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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Opc;

public class SubscriberOptions
{
	internal const int QUEUE_SIZE = 512 * 4;
	internal const int KEEP_ALIVE_COUNT = 512 * 1000;
	internal const int LIFETIME_COUNT = KEEP_ALIVE_COUNT * 3;

	public TimeSpan MinLifetimeInterval { get; set; }
	public TimeSpan PublishingInterval { get; set; }
	public TimeSpan SamplingInterval { get; set; }

	public int QueueSize { get; set; }
	public int KeepAliveCount { get; set; }
	public int LifetimeCount { get; set; }
	public byte Priority { get; set; }
}

public static class SubscriberOptionsUtility
{
	public static int GetSamplingInterval(this SubscriberOptions options) => options == null ? 100 : (int)options.SamplingInterval.TotalMilliseconds;
	public static int GetPublishingInterval(this SubscriberOptions options) => options == null ? 1000 : (int)options.PublishingInterval.TotalMilliseconds;
	public static uint GetMinLifetimeInterval(this SubscriberOptions options) => options == null ? 1000 : (uint)options.MinLifetimeInterval.TotalMilliseconds;

	public static int GetQueueSize(this SubscriberOptions options) => options == null ? 0 : options.QueueSize;
	public static int GetKeepAliveCount(this SubscriberOptions options) => options == null ? SubscriberOptions.KEEP_ALIVE_COUNT : options.KeepAliveCount;
	public static int GetLifetimeCount(this SubscriberOptions options) => options == null ? SubscriberOptions.LIFETIME_COUNT : options.LifetimeCount;
	public static byte GetPriority(this SubscriberOptions options) => options == null ? (byte)0 : options.Priority;
}
