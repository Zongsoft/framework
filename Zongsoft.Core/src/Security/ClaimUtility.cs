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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Security.Claims;

namespace Zongsoft.Security;

public static class ClaimUtility
{
	public static object GetValue(this Claim claim) => GetValue(claim, out _);
	public static object GetValue(this Claim claim, out Type type)
	{
		if(claim == null)
			throw new ArgumentNullException(nameof(claim));

		type = claim.GetValueType();
		return Common.Convert.ConvertValue(claim.Value, type);
	}

	public static bool TryGetValue(this Claim claim, out object value) => TryGetValue(claim, out _, out value);
	public static bool TryGetValue(this Claim claim, out Type type, out object value)
	{
		if(claim == null)
			throw new ArgumentNullException(nameof(claim));

		type = claim.GetValueType();
		return Common.Convert.TryConvertValue(claim.Value, type, out value);
	}

	public static Type GetValueType(this Claim claim) => claim.ValueType switch
	{
		ClaimValueTypes.Boolean => typeof(bool),
		ClaimValueTypes.Date => typeof(DateOnly),
		ClaimValueTypes.DateTime => typeof(DateTime),
		ClaimValueTypes.Time => typeof(TimeOnly),
		ClaimValueTypes.Integer or ClaimValueTypes.Integer32 => typeof(int),
		ClaimValueTypes.Integer64 => typeof(long),
		ClaimValueTypes.UInteger32 => typeof(uint),
		ClaimValueTypes.UInteger64 => typeof(ulong),
		ClaimValueTypes.Double => typeof(double),
		ClaimValueTypes.Fqbn or
		ClaimValueTypes.Email or
		ClaimValueTypes.String or
		ClaimValueTypes.DnsName => typeof(string),
		_ => Common.TypeAlias.TryParse(claim.ValueType, out var type) ? type : typeof(string),
	};
}
