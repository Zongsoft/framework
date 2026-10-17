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
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Net;

/// <summary>以连接标识管理通道的实时集合；枚举不承诺包含并发新加入的通道。</summary>
/// <typeparam name="T">报文类型。</typeparam>
public sealed class TcpServerChannelManager<T> : IReadOnlyCollection<TcpServerChannel<T>>
{
	#region 事件声明
	/// <summary>表示通道开始关闭；不可取消，事件参数是该时刻的状态快照。</summary>
	public event EventHandler<TcpChannelEventArgs<T>> Closing { add => _notifier.Closing += value; remove => _notifier.Closing -= value; }
	/// <summary>表示处理器、发送及缓冲清理完成，通道已经移除登记。</summary>
	public event EventHandler<TcpChannelEventArgs<T>> Closed { add => _notifier.Closed += value; remove => _notifier.Closed -= value; }
	/// <summary>表示通道结束过程中的首个故障；每条通道最多通知一次，不包含发送容量拒绝。</summary>
	public event EventHandler<TcpChannelEventArgs<T>> Error { add => _notifier.Error += value; remove => _notifier.Error -= value; }
	#endregion

	#region 成员字段
	private readonly TcpChannelNotifier<T> _notifier;
	private readonly ConcurrentDictionary<long, TcpServerChannel<T>> _channels = new();
	#endregion

	#region 构造函数
	internal TcpServerChannelManager() => _notifier = new(this);
	#endregion

	#region 公共属性
	/// <summary>获取尚未完成清理的已登记连接数。</summary>
	public int Count => _channels.Count;
	#endregion

	#region 公共方法
	/// <summary>按唯一标识登记连接，不以远端端点作为身份。</summary>
	/// <param name="channel">需要登记或移除的当前连接。</param>
	/// <returns>是否新增成功。</returns>
	internal bool Add(TcpServerChannel<T> channel) => _channels.TryAdd(channel.Identifier, channel);

	/// <summary>仅移除当前标识对应的连接。</summary>
	/// <param name="channel">需要登记或移除的当前连接。</param>
	/// <returns>是否移除成功。</returns>
	internal bool Remove(TcpServerChannel<T> channel) => _channels.TryRemove(channel.Identifier, out _);

	/// <summary>按稳定标识获取连接。</summary>
	/// <param name="identifier">连接标识。</param>
	/// <param name="channel">找到的通道。</param>
	/// <returns>是否找到连接。</returns>
	public bool TryGetValue(long identifier, out TcpServerChannel<T> channel) => _channels.TryGetValue(identifier, out channel);
	#endregion

	#region 事件通知
	/// <summary>转发开始关闭通知。</summary>
	/// <param name="args">当前通道的状态快照。</param>
	internal void OnClosing(TcpChannelEventArgs<T> args) => _notifier.OnClosing(args);
	/// <summary>转发完整退场通知。</summary>
	/// <param name="args">移除登记后的最终状态快照。</param>
	internal void OnClosed(TcpChannelEventArgs<T> args) => _notifier.OnClosed(args);
	/// <summary>转发并记录通道的首个结束故障。</summary>
	/// <param name="args">包含首个异常的状态快照。</param>
	internal void OnError(TcpChannelEventArgs<T> args) => _notifier.OnError(args);
	#endregion

	#region 枚举遍历
	/// <summary>枚举当前仍登记的连接。</summary>
	/// <returns>连接枚举器，不包含已完成清理的连接。</returns>
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	/// <summary>枚举当前仍登记的连接。</summary>
	/// <returns>连接枚举器，不承诺包含并发新加入的连接。</returns>
	public IEnumerator<TcpServerChannel<T>> GetEnumerator()
	{
		foreach(var entry in _channels)
			yield return entry.Value;
	}
	#endregion
}
