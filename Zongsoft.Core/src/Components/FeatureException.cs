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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
