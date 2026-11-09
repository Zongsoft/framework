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

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

public static class Tags
{
	public static OpenApiTag Tag(string name, params string[] ancestors)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		var tag = new OpenApiTag() { Name = name };

		for(int i = ancestors.Length - 1; i >= 0; i--)
		{
			if(!string.IsNullOrEmpty(ancestors[i]))
			{
				tag.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.OrdinalIgnoreCase);
				tag.Extensions["x-parent"] = Extensions.Helper.String(ancestors[i]);
				break;
			}
		}

		return tag;
	}

	public static OpenApiTag Caption(this OpenApiTag tag, string caption)
	{
		const string EXTENSION_KEY = "x-displayName";

		if(tag == null)
			return tag;

		if(string.IsNullOrEmpty(caption))
			tag.Extensions?.Remove(EXTENSION_KEY);
		else
		{
			if(tag.Extensions == null)
				tag.Extensions = new Dictionary<string, IOpenApiExtension>()
				{
					{ EXTENSION_KEY, Extensions.Helper.String(caption) },
				};
			else
				tag.Extensions.Add(EXTENSION_KEY, Extensions.Helper.String(caption));
		}

		return tag;
	}

	public static OpenApiTag Description(this OpenApiTag tag, string description)
	{
		tag?.Description = string.IsNullOrEmpty(description) ? null : description;
		return tag;
	}
}
