/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
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

namespace Zongsoft.Net;

/// <summary>TCP 连接的缓冲、容量、时间与调度设置。实例构造时复制设置。</summary>
public class TcpConnectionOptions
{
	#region 连接设置
	/// <summary>获取或设置是否采集读取、预算耗尽、发送等待及共享维护扫描诊断，默认关闭。</summary>
	public bool EnableDiagnostics { get; set; }
	/// <summary>获取或设置是否禁用 Nagle 算法，默认为真。</summary>
	public bool NoDelay { get; set; } = true;
	/// <summary>获取或设置是否先进行零字节读取，避免空闲连接预租接收缓冲。</summary>
	public bool ZeroLengthReads { get; set; } = true;
	/// <summary>获取或设置二进制报文正文或无头字节块的最大长度。</summary>
	public int MaximumPacketLength { get; set; } = 64 * 1024;
	/// <summary>获取或设置最大完整线协议帧长度，包含包头。</summary>
	public int MaximumFrameLength { get; set; } = 64 * 1024 + 4;
	/// <summary>获取或设置未完成帧允许保留的最大字节数。</summary>
	public int MaximumBufferedReceiveBytes { get; set; } = 128 * 1024;
	#endregion

	#region 管道设置
	/// <summary>获取或设置发送 Pipe 暂停阈值。</summary>
	public int SendPauseThreshold { get; set; } = 64 * 1024;
	/// <summary>获取或设置发送 Pipe 恢复阈值。</summary>
	public int SendResumeThreshold { get; set; } = 32 * 1024;
	/// <summary>获取或设置接收 Pipe 暂停阈值；实际阈值至少容纳最大合法帧。</summary>
	public int ReceivePauseThreshold { get; set; } = 64 * 1024;
	/// <summary>获取或设置接收 Pipe 恢复阈值。</summary>
	public int ReceiveResumeThreshold { get; set; } = 32 * 1024;
	/// <summary>获取或设置一次连续同步接收处理的最大帧数。</summary>
	public int ReceiveFrameBudget { get; set; } = 64;
	/// <summary>获取或设置一次连续同步接收处理的最大字节数；至少允许处理一帧。</summary>
	public int ReceiveByteBudget { get; set; } = 64 * 1024;
	#endregion

	#region 发送限额
	/// <summary>获取或设置正在等待、编码或 Flush 的最大发送操作数。</summary>
	public int MaximumPendingSends { get; set; } = 64;
	/// <summary>获取或设置预留及尚未提交 Socket 发送的最大线协议字节数。</summary>
	public int MaximumPendingSendBytes { get; set; } = 256 * 1024;
	#endregion

	#region 时间设置
	/// <summary>获取或设置发送停滞期限，默认为三十秒。</summary>
	public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(30);
	/// <summary>获取或设置接收完整帧的期限，从首个残留字节开始计算，后续字节不重置期限；超时以 <see cref="TcpCloseReason.FrameTimeout" /> 关闭。</summary>
	public TimeSpan FrameTimeout { get; set; } = TimeSpan.FromSeconds(30);
	/// <summary>获取或设置无实际接收活动的空闲期限；零表示禁用。</summary>
	public TimeSpan IdleTimeout { get; set; }
	/// <summary>获取或设置停止及发送排空期限。</summary>
	public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(10);
	/// <summary>获取或设置共享维护扫描的周期。</summary>
	public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromSeconds(1);
	/// <summary>获取或设置单调时间与异步期限的提供者。</summary>
	public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
	#endregion

	#region 内部方法
	internal TcpConnectionOptions Snapshot()
	{
		var options = (TcpConnectionOptions)this.MemberwiseClone();
		options.Validate();
		return options;
	}

	internal virtual void Validate()
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.MaximumPacketLength);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.MaximumFrameLength);
		ArgumentOutOfRangeException.ThrowIfLessThan(this.MaximumFrameLength, this.MaximumPacketLength);
		ArgumentOutOfRangeException.ThrowIfLessThan(this.MaximumBufferedReceiveBytes, this.MaximumFrameLength);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.ReceiveResumeThreshold);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.ReceivePauseThreshold, this.ReceiveResumeThreshold);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.SendResumeThreshold);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.SendPauseThreshold, this.SendResumeThreshold);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.ReceiveFrameBudget);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.ReceiveByteBudget);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.MaximumPendingSends);
		ArgumentOutOfRangeException.ThrowIfLessThan(this.MaximumPendingSendBytes, this.MaximumFrameLength);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.SendTimeout, TimeSpan.Zero);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.FrameTimeout, TimeSpan.Zero);
		ArgumentOutOfRangeException.ThrowIfLessThan(this.IdleTimeout, TimeSpan.Zero);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.ShutdownTimeout, TimeSpan.Zero);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.MaintenanceInterval, TimeSpan.Zero);
		ArgumentNullException.ThrowIfNull(this.TimeProvider);
	}
	#endregion
}
