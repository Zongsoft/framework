/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Distributing
{
	public class DistributedLockNormalizer
	{
		#region 单例字段
		public static readonly IDistributedLockNormalizer Guid = new GuidNormalizer();
		public static readonly IDistributedLockNormalizer Randon = new RandomNormalizer();
		#endregion

		#region 嵌套子类
		private class GuidNormalizer : IDistributedLockNormalizer
		{
			public string Name { get => "Guid"; }
			public ReadOnlyMemory<byte> Normalize() => System.Guid.NewGuid().ToByteArray();
			public string GetString(ReadOnlySpan<byte> value) => value.IsEmpty ? null : (new Guid(value)).ToString();
		}

		private class RandomNormalizer : IDistributedLockNormalizer
		{
			public string Name { get => "Random"; }
			public ReadOnlyMemory<byte> Normalize() => BitConverter.GetBytes((ulong)Zongsoft.Common.Randomizer.GenerateInt64());
			public string GetString(ReadOnlySpan<byte> value) => value.IsEmpty ? null : BitConverter.ToUInt64(value).ToString();
		}
		#endregion
	}
}
