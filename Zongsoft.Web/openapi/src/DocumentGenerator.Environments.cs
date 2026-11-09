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

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static void GenerateEnvironments(this DocumentContext context)
	{
		var environments = context.Configuration.GetEnvironments();

		if(environments != null && environments.Count > 0)
			context.Document.AddExtension("x-scalar-environments", Extensions.Helper.Object(environments));
	}
}
