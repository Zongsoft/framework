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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Web.Routing;

namespace Zongsoft.Web.OpenApi;

internal static class Utility
{
	public static bool IsBody(this ParameterModel parameter)
	{
		var source = parameter.GetParameterSource();
		return source == BindingSource.Body || source == BindingSource.Form || source == BindingSource.FormFile;
	}

	public static ParameterLocation? GetLocation(ParameterModel parameter)
	{
		var source = parameter.GetParameterSource();

		if(source == BindingSource.Path)
			return ParameterLocation.Path;
		if(source == BindingSource.Query)
			return ParameterLocation.Query;
		if(source == BindingSource.Header)
			return ParameterLocation.Header;

		return null;
	}

	public static bool IsDictionaryEntry(this Type type) =>
		type == typeof(System.Collections.DictionaryEntry) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>));

	public static bool IsScalarType(this Type type)
	{
		if(type == null)
			return false;

		type = Nullable.GetUnderlyingType(type) ?? type;

		var result = type.IsPrimitive ||
			   type == typeof(string) ||
			   type == typeof(decimal) ||
			   type == typeof(DateTime) ||
			   type == typeof(DateTimeOffset) ||
			   type == typeof(TimeSpan) ||
			   type == typeof(Guid);

		if(result)
			return true;

		var converter = Zongsoft.Common.Convert.GetTypeConverter(type);
		return converter != null && converter.CanConvertFrom(typeof(string));
	}
}
