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
using System.Collections.Generic;

namespace Zongsoft.Intelligences;

public class ChatSessionOptions
{
	#region 构造函数
	public ChatSessionOptions(string name) : this(name, TimeSpan.FromHours(12)) { }
	public ChatSessionOptions(string name, TimeSpan expiration, params IEnumerable<KeyValuePair<object, object>> parameters)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Expiration = expiration;
		this.Parameters = new Collections.Parameters(parameters);
	}
	#endregion

	#region 公共属性
	/// <summary>获取聊天提供程序名称。</summary>
	public string Name { get; }

	/// <summary>获取或设置会话开场白。</summary>
	public string[] Preludes { get; set; }

	/// <summary>获取或设置会话有效期。</summary>
	public TimeSpan Expiration { get; set; }

	/// <summary>获取或设置会话参数集。</summary>
	public Collections.Parameters Parameters { get; set; }
	#endregion
}
