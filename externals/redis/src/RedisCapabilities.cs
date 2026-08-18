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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Redis;

/// <summary>指定 Redis 服务端可供本扩展使用的能力。</summary>
[Flags]
public enum RedisCapabilities
{
	None = 0,
	StreamAutoClaim = 1,
	StreamAcknowledgeAndDelete = 2,
	StreamGroupTrimming = 4,
	StreamIdempotentProducer = 8,
}

/// <summary>提供 Redis 版本与能力之间的保守映射。</summary>
public static class RedisCapabilityMatrix
{
	private static readonly Version VERSION_6_2 = new(6, 2);
	private static readonly Version VERSION_8_2 = new(8, 2);
	private static readonly Version VERSION_8_6 = new(8, 6);

	public static RedisCapabilities GetCapabilities(Version version)
	{
		if(version == null)
			return RedisCapabilities.None;

		var result = RedisCapabilities.None;
		if(version >= VERSION_6_2)
			result |= RedisCapabilities.StreamAutoClaim;
		if(version >= VERSION_8_2)
			result |= RedisCapabilities.StreamAcknowledgeAndDelete | RedisCapabilities.StreamGroupTrimming;
		if(version >= VERSION_8_6)
			result |= RedisCapabilities.StreamIdempotentProducer;
		return result;
	}
}
