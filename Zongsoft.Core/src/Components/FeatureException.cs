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

namespace Zongsoft.Components;

[Serializable]
public class FeatureException : ApplicationException
{
	#region 构造函数
	public FeatureException() { }
	public FeatureException(string message) : this(null, message, null) { }
	public FeatureException(string message, Exception innerException) : this(null, message, innerException) { }
	public FeatureException(string identifier, string message) : this(identifier, message, null) { }
	public FeatureException(string identifier, string message, Exception innerException) : base(message, innerException)
	{
		this.Identifier = identifier;
	}
	#endregion

	#region 公共属性
	/// <summary>获取功能的标识。</summary>
	public string Identifier { get; }
	#endregion
}
