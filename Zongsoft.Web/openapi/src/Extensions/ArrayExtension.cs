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
using System.Collections;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi.Extensions;

static partial class Helper
{
	public static IOpenApiExtension Array(IEnumerable values) => new ArrayExtension(values);
}

public class ArrayExtension(IEnumerable values) : IOpenApiExtension
{
	public void Write(IOpenApiWriter writer, OpenApiSpecVersion version)
	{
		if(values == null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartArray();

		foreach(var value in values)
			this.OnWrite(writer, version, value);

		writer.WriteEndArray();
	}

	protected virtual void OnWrite(IOpenApiWriter writer, OpenApiSpecVersion version, object value)
	{
		Helper.Object(value).Write(writer, version);
	}
}