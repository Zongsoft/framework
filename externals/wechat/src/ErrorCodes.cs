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

namespace Zongsoft.Externals.Wechat;

/// <summary>提供了微信平台全局错误码的定义。</summary>
public static class ErrorCodes
{
	/// <summary>成功。</summary>
	public static readonly int Succeed = 0;

	/// <summary>系统繁忙，稍后重试。</summary>
	public static readonly int Busy = -1;

	/// <summary>无效的AppSecret。</summary>
	public static readonly int InvalidSecret = 40001;

	/// <summary>调用接口的IP地址不在白名单中，请在接口IP白名单中进行设置。</summary>
	public static readonly int Blocked = 40164;

	/// <summary>此IP调用需要管理员确认，请联系管理员。</summary>
	public static readonly int Unapproved = 89503;

	/// <summary>此IP正在等待管理员确认，请联系管理员。</summary>
	public static readonly int Approving = 89501;

	/// <summary>1小时内该IP被管理员拒绝调用一次，1小时内不可再使用该IP调用。</summary>
	public static readonly int Rejected = 89507;

	/// <summary>24小时内该IP被管理员拒绝调用两次，24小时内不可再使用该IP调用。</summary>
	public static readonly int Denied = 89506;
}
