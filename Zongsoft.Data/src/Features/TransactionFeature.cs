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

namespace Zongsoft.Data.Features;

/// <summary>声明数据库事务的行为。</summary>
public interface ITransactionFeature
{
	/// <summary>获取一个值，指示是否抑制数据库事务。</summary>
	bool Suppressed { get; }
}

/// <summary>提供数据库事务行为的不可变声明。</summary>
public sealed class TransactionFeature : ITransactionFeature
{
	/// <summary>获取不抑制数据库事务的共享实例。</summary>
	public static readonly TransactionFeature Instance = new();

	/// <summary>初始化数据库事务行为。</summary>
	/// <param name="suppressed">指示是否抑制数据库事务，默认为否。</param>
	public TransactionFeature(bool suppressed = false) => this.Suppressed = suppressed;

	/// <summary>获取一个值，指示是否抑制数据库事务。</summary>
	public bool Suppressed { get; }
}
