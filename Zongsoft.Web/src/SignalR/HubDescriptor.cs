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
using System.Reflection;

using Microsoft.AspNetCore.Mvc;

namespace Zongsoft.Web.SignalR;

public sealed class HubDescriptor : IEquatable<HubDescriptor>, IEquatable<TypeInfo>
{
	#region 常量定义
	private const string HUB_SUFFIX = @"Hub";
	#endregion

	#region 构造函数
	public HubDescriptor(TypeInfo type, string pattern = null)
	{
		this.Type = type ?? throw new ArgumentNullException(nameof(type));

		if(string.IsNullOrEmpty(pattern))
		{
			var attribute = type.GetCustomAttribute<RouteAttribute>(true);

			if(attribute != null)
				pattern = attribute.Template;
			else
			{
				if(type.Name.Length > HUB_SUFFIX.Length && type.Name.EndsWith(HUB_SUFFIX))
					pattern = type.Name[..^HUB_SUFFIX.Length];
				else
					pattern = type.Name;
			}
		}

		this.Pattern = pattern;
	}
	#endregion

	#region 公共属性
	public TypeInfo Type { get; }
	public string Pattern { get; }
	#endregion

	#region 重写方法
	public bool Equals(TypeInfo type) => type is not null && type == this.Type;
	public bool Equals(HubDescriptor other) => other is not null && other.Type == this.Type;
	public override bool Equals(object obj) => obj is HubDescriptor other && this.Equals(other);
	public override int GetHashCode() => this.Type.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Pattern) ? this.Type.FullName : $"[{this.Pattern}]{this.Type.FullName}";
	#endregion
}