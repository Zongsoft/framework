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
using System.Security.Claims;

namespace Zongsoft.Security;

public static class ClaimUtility
{
	#region 静态方法
	public static T GetValue<T>(this Claim claim)
	{
		if(claim == null)
			throw new ArgumentNullException(nameof(claim));

		return Common.Convert.ConvertValue<T>(claim.Value);
	}

	public static bool TryGetValue<T>(this Claim claim, out T value)
	{
		if(claim == null)
		{
			value = default;
			return false;
		}

		return Common.Convert.TryConvertValue(claim.Value, out value);
	}

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
		{
			type = null;
			value = null;
			return false;
		}

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
		ClaimValueTypes.DaytimeDuration or
		ClaimValueTypes.YearMonthDuration => typeof(TimeSpan),
		_ => Common.TypeAlias.TryParse(claim.ValueType, out var type) ? type : typeof(string),
	};

	public static string GetValueType(Type type)
	{
		if(type == null)
			return ClaimValueTypes.String;

		return Type.GetTypeCode(type) switch
		{
			TypeCode.String => ClaimValueTypes.String,
			TypeCode.Boolean => ClaimValueTypes.Boolean,
			TypeCode.DateTime => ClaimValueTypes.DateTime,
			TypeCode.Int32 => ClaimValueTypes.Integer32,
			TypeCode.Int64 => ClaimValueTypes.Integer64,
			TypeCode.UInt32 => ClaimValueTypes.UInteger32,
			TypeCode.UInt64 => ClaimValueTypes.UInteger64,
			TypeCode.Double => ClaimValueTypes.Double,
			_ => Common.TypeAlias.GetAlias(type),
		};
	}

	public static string GetClaimTypeAlias(this Claim claim)
	{
		if(claim == null)
			throw new ArgumentNullException(nameof(claim));

		return GetClaimTypeAlias(claim.Type);
	}

	public static string GetClaimTypeAlias(string claimType)
	{
		if(string.IsNullOrEmpty(claimType))
			return null;

		if(claimType.Contains('/') || claimType.Contains('\\'))
		{
			return claimType switch
			{
				ClaimTypes.NameIdentifier => "Identifier",
				_ => GetName(claimType),
			};
		}

		return claimType;

		static string GetName(string url)
		{
			var lastly = url[^1] == '/' || url[^1] == '\\' ? 1 : 0;
			var index = url.LastIndexOfAny(['/', '\\'], url.Length - lastly - 1);

			if(index >= 0)
				return url[(index + 1)..^lastly];

			return url;
		}
	}
	#endregion
}
