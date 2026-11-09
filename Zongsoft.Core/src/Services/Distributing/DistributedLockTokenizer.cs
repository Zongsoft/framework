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

namespace Zongsoft.Services.Distributing;

public class DistributedLockTokenizer
{
	#region 单例字段
	public static readonly IDistributedLockTokenizer Guid = new GuidNormalizer();
	public static readonly IDistributedLockTokenizer Random = new RandomNormalizer();
	#endregion

	#region 嵌套子类
	private class GuidNormalizer : IDistributedLockTokenizer
	{
		public string Name => "Guid";
		public byte[] Tokenize() => System.Guid.NewGuid().ToByteArray();
		public string GetString(ReadOnlySpan<byte> value) => value.IsEmpty ? null : new Guid(value).ToString("N");
	}

	private class RandomNormalizer : IDistributedLockTokenizer
	{
		public string Name => "Random";
		public byte[] Tokenize() => BitConverter.GetBytes(Common.Randomizer.GenerateUInt64());
		public string GetString(ReadOnlySpan<byte> value) => value.IsEmpty ? null : BitConverter.ToUInt64(value).ToString("X");
	}
	#endregion
}
