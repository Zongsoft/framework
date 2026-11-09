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
using System.Text.Json;

namespace Zongsoft.Serialization.Json;

internal static class NamingConvention
{
	public static readonly JsonNamingPolicy Camel = new LetterCaseNamingPolicy(chr => char.ToLowerInvariant(chr));
	public static readonly JsonNamingPolicy Pascal = new LetterCaseNamingPolicy(chr => char.ToUpperInvariant(chr));

	private class LetterCaseNamingPolicy : JsonNamingPolicy
	{
		#region 成员字段
		private readonly Func<char, char> _converter;
		#endregion

		#region 构造函数
		public LetterCaseNamingPolicy(Func<char, char> converter)
		{
			_converter = converter ?? throw new ArgumentNullException(nameof(converter));
		}
		#endregion

		#region 公共方法
		public override string ConvertName(string name)
		{
			if(string.IsNullOrEmpty(name))
				return name;

			char[] chars = name.ToCharArray();
			FixCasing(chars, _converter);
			return new string(chars);
		}
		#endregion

		#region 私有方法
		private static void FixCasing(Span<char> chars, Func<char, char> converter)
		{
			var initial = true;

			for(int i = 0; i < chars.Length; i++)
			{
				if(initial)
				{
					if(char.IsLetter(chars[i]))
					{
						chars[i] = converter(chars[i]);
						initial = false;
					}
				}
				else
				{
					if(!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
						initial = true;
				}
			}
		}
		#endregion
	}
}