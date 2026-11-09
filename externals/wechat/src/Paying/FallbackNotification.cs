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
using System.Text.Json;

namespace Zongsoft.Externals.Wechat.Paying;

/// <summary>已通过协议验证的通知，宿主仍须校验业务归属并可靠接收。</summary>
public sealed partial class FallbackNotification
{
	internal FallbackNotification(FallbackKind kind, string identifier, string eventType, string merchant, string subsidiary, string format, JsonElement payload)
	{
		this.Kind = kind;
		this.Identifier = identifier;
		this.EventType = eventType;
		this.Merchant = merchant;
		this.Subsidiary = subsidiary;
		this.Format = format;
		this.Payload = payload.Clone();
	}

	public FallbackKind Kind { get; }
	public string Identifier { get; }
	public string EventType { get; }
	public string Merchant { get; }
	public string Subsidiary { get; }
	public string Format { get; }
	public JsonElement Payload { get; }
}

public enum FallbackKind
{
	Payment,
	Refundment,
	OfflineFaceAgreement,
}

/// <summary>只有业务完成、持久接收或已可靠接收的重复通知才可以确认成功。</summary>
public enum FallbackReceipt
{
	None,
	Completed,
	Accepted,
	Duplicate,
}
