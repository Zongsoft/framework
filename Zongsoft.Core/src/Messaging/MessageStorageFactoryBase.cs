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
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace Zongsoft.Messaging;

/// <summary>表示消息存储器工厂的基类。</summary>
/// <typeparam name="TStorage">指定创建的消息存储器类型。</typeparam>
public abstract class MessageStorageFactoryBase<TStorage> : IMessageStorageFactory where TStorage : IMessageStorage
{
	#region 常量定义
	private const string PARTITION_PREFIX = "Zongsoft.Messaging.Storage";
	#endregion

	#region 成员字段
	private string _identifier;
	#endregion

	#region 保护属性
	/// <summary>获取当前工厂首次使用时冻结的存储标识。</summary>
	/// <remarks>该值来自 <c>ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER</c> 环境变量；未设置时回退到 <see cref="Environment.MachineName"/>。</remarks>
	protected string Identifier
	{
		get
		{
			const string IDENTIFIER_ENVIRONMENT_VARIABLE = "ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER";

			var identifier = Volatile.Read(ref _identifier);
			if(identifier != null)
				return identifier;

			identifier = Environment.GetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE);
			identifier = string.IsNullOrWhiteSpace(identifier) ? Environment.MachineName : identifier.Trim();
			return Interlocked.CompareExchange(ref _identifier, identifier, null) ?? identifier;
		}
	}
	#endregion

	#region 公共方法
	/// <summary>为指定消息代理创建一个独占的消息存储器。</summary>
	/// <param name="name">指定消息代理名称，该名称同时用于查找同名连接设置。</param>
	/// <returns>返回创建的消息存储器，如果创建失败则抛出异常。</returns>
	public TStorage Create(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		return this.OnCreate(name.Trim()) ?? throw Common.OperationException.Unprocessed(string.Format(Properties.Resources.Messaging_StorageCreationFailed_Message, name));
	}
	#endregion

	#region 显式实现
	IMessageStorage IMessageStorageFactory.Create(string name) => this.Create(name);
	#endregion

	#region 保护方法
	/// <summary>由派生工厂创建指定消息代理的存储器。</summary>
	/// <param name="name">经过规范化的消息代理名称。</param>
	/// <returns>返回创建的消息存储器。</returns>
	protected abstract TStorage OnCreate(string name);

	/// <summary>根据规范连接设置名称和存储标识生成存储分区。</summary>
	/// <param name="name">指定规范连接设置名称。</param>
	/// <returns>返回长度不超过128个字符的稳定存储分区。</returns>
	/// <remarks>重写方法必须返回非空、稳定且不超过128个字符的分区。</remarks>
	protected virtual string GetPartition(string name)
	{
		const int PARTITION_LENGTH = 128;

		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		var partition = $"{PARTITION_PREFIX}:{name.Trim()}:{this.Identifier}";
		if(partition.Length <= PARTITION_LENGTH)
			return partition;

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(partition));
		return $"{PARTITION_PREFIX}:sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
	}
	#endregion
}
