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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Configuration;

namespace Zongsoft.Messaging;

/// <summary>为具有强类型连接设置的独立消息存储提供参数校验和模板方法。</summary>
/// <typeparam name="TSettings">指定的连接设置类型。</typeparam>
public abstract class MessageStorageBase<TSettings> : IMessageStorage where TSettings : IConnectionSettings
{
	#region 成员字段
	private TSettings _settings;
	#endregion

	#region 构造函数
	/// <summary>初始化消息存储基类。</summary>
	/// <param name="settings">指定的连接设置。</param>
	/// <exception cref="ArgumentNullException"><paramref name="settings"/> 为空。</exception>
	protected MessageStorageBase(TSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));
	#endregion

	#region 公共属性
	/// <summary>获取存储器的实现名称。</summary>
	/// <value>返回非空的存储实现名称。</value>
	public abstract string Name { get; }

	/// <summary>获取或设置当前存储器的强类型连接设置。</summary>
	/// <value>当前存储器独占的连接设置，不能为空。</value>
	/// <exception cref="ArgumentNullException">设置值为空。</exception>
	public virtual TSettings Settings
	{
		get => _settings;
		set => _settings = value ?? throw new ArgumentNullException(nameof(value));
	}

	IConnectionSettings IMessageStorage.Settings
	{
		get => this.Settings;
		set
		{
			if(value == null)
				throw new ArgumentNullException(nameof(value));
			if(value is not TSettings settings)
				throw new ArgumentException(string.Format(Properties.Resources.Messaging_StorageSettingsInvalid_Message, value.GetType().Name, typeof(TSettings).Name), nameof(value));

			this.Settings = settings;
		}
	}
	#endregion

	#region 公共方法
	/// <summary>新增或更新指定的消息。</summary>
	/// <param name="message">指定要保存的消息。</param>
	/// <param name="expiry">指定消息的生存时长，小于或等于零表示永久保存。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步保存操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="message"/> 没有有效标识。</exception>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>派生实现必须在返回的任务完成前持有消息快照；小于或等于零的 <paramref name="expiry"/> 表示永久保存。</remarks>
	public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(message.Identifier))
			throw new ArgumentNullException(nameof(message));

		cancellation.ThrowIfCancellationRequested();
		return this.OnSetAsync(message, expiry, cancellation);
	}

	/// <summary>删除指定的消息。</summary>
	/// <param name="identifier">指定要删除的消息标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步删除操作的任务；如果找到并删除了指定消息则其结果为真，否则为假。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="identifier"/> 为空或空白字符串。</exception>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(identifier))
			throw new ArgumentNullException(nameof(identifier));

		cancellation.ThrowIfCancellationRequested();
		return this.OnRemoveAsync(identifier, cancellation);
	}

	/// <summary>获取当前存储器中的消息。</summary>
	/// <param name="cancellation">指定的异步枚举取消标记。</param>
	/// <returns>返回当前存储器中尚未过期的消息异步序列。</returns>
	/// <exception cref="OperationCanceledException">异步枚举已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>派生实现返回的消息必须是独立快照且不得包含确认回调。</remarks>
	public IAsyncEnumerable<Message> GetAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		return this.OnGetAsync(cancellation);
	}
	#endregion

	#region 抽象方法
	/// <summary>由派生类新增或更新指定的消息。</summary>
	/// <param name="message">指定要保存的消息。</param>
	/// <param name="expiry">指定消息的生存时长，小于或等于零表示永久保存。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步保存操作的任务。</returns>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>实现必须在返回的任务完成前持有消息快照；小于或等于零的 <paramref name="expiry"/> 表示永久保存。</remarks>
	protected abstract ValueTask OnSetAsync(Message message, TimeSpan expiry, CancellationToken cancellation);

	/// <summary>由派生类删除指定的消息。</summary>
	/// <param name="identifier">指定要删除的消息标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步删除操作的任务；如果找到并删除了指定消息则其结果为真，否则为假。</returns>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	protected abstract ValueTask<bool> OnRemoveAsync(string identifier, CancellationToken cancellation);

	/// <summary>由派生类获取当前存储器中的消息。</summary>
	/// <param name="cancellation">指定的异步枚举取消标记。</param>
	/// <returns>返回当前存储器中尚未过期的消息异步序列。</returns>
	/// <exception cref="OperationCanceledException">异步枚举已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>实现返回的消息必须是独立快照且不得包含确认回调；枚举顺序由实现定义。</remarks>
	protected abstract IAsyncEnumerable<Message> OnGetAsync(CancellationToken cancellation);
	#endregion
}
