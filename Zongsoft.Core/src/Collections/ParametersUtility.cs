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

namespace Zongsoft.Collections;

public static class ParametersUtility
{
	public static Parameters Parameter(this Parameters parameters, string name, object value)
	{
		parameters ??= new Parameters();
		parameters.SetValue(name, value);
		return parameters;
	}

	public static Parameters Parameter(this Parameters parameters, Type type, object value)
	{
		parameters ??= new Parameters();
		parameters.SetValue(type, value);
		return parameters;
	}

	public static Parameters Parameter<T>(this Parameters parameters, object value)
	{
		parameters ??= new Parameters();
		parameters.SetValue<T>(value);
		return parameters;
	}

	public static Parameters Parameter(this Parameters parameters, object value)
	{
		parameters ??= new Parameters();
		parameters.SetValue(value);
		return parameters;
	}
}