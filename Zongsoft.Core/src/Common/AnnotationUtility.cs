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
using System.ComponentModel;

namespace Zongsoft.Common;

public static class AnnotationUtility
{
	public static string GetCategory(MemberInfo member, bool inherit = false)
	{
		if(member == null)
			throw new ArgumentNullException(nameof(member));

		var attribute = member.GetCustomAttribute<CategoryAttribute>(inherit);
		return attribute == null ? null : (Resources.ResourceUtility.GetResourceString(member, attribute.Category) ?? attribute.Category);
	}

	public static string GetDisplayName(MemberInfo member, bool inherit = false)
	{
		if(member == null)
			throw new ArgumentNullException(nameof(member));

		var attribute = member.GetCustomAttribute<DisplayNameAttribute>(inherit);
		return attribute == null ? null : (Resources.ResourceUtility.GetResourceString(member, attribute.DisplayName) ?? attribute.DisplayName);
	}

	public static string GetDescription(MemberInfo member, bool inherit = false)
	{
		if(member == null)
			throw new ArgumentNullException(nameof(member));

		var attribute = member.GetCustomAttribute<DescriptionAttribute>(inherit);
		return attribute == null ? null : (Resources.ResourceUtility.GetResourceString(member, attribute.Description) ?? attribute.Description);
	}
}
