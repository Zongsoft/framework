/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Common
{
	public static class StringExtension
	{
		public delegate bool TryParser<T>(string text, out T value);

		public static string RemoveAny(this string text, params char[] characters)
		{
			return RemoveAny(text, characters, 0, -1);
		}

		public static string RemoveAny(this string text, char[] characters, int startIndex)
		{
			return RemoveAny(text, characters, startIndex, -1);
		}

		public static string RemoveAny(this string text, char[] characters, int startIndex, int count)
		{
			if(string.IsNullOrEmpty(text) || characters == null || characters.Length < 1)
				return text;

			if(startIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if(count < 1)
				count = characters.Length - startIndex;

			if(startIndex + count > characters.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			string result = text;

			for(int i = startIndex; i < startIndex + count; i++)
				result = result.Replace(characters[i].ToString(), string.Empty);

			return result;
		}

		public static string Trim(this string text, string trimString)
		{
			return Trim(text, trimString, StringComparison.OrdinalIgnoreCase);
		}

		public static string Trim(this string text, string trimString, StringComparison comparisonType)
		{
			return TrimEnd(
				TrimStart(text, trimString, comparisonType),
				trimString,
				comparisonType);
		}

		public static string Trim(this string text, string prefix, string suffix)
		{
			return Trim(text, prefix, suffix, StringComparison.OrdinalIgnoreCase);
		}

		public static string Trim(this string text, string prefix, string suffix, StringComparison comparisonType)
		{
			return text
					.TrimStart(prefix, comparisonType)
					.TrimEnd(suffix, comparisonType);
		}

		public static string TrimEnd(this string text, string trimString)
		{
			return TrimEnd(text, trimString, StringComparison.OrdinalIgnoreCase);
		}

		public static string TrimEnd(this string text, string trimString, StringComparison comparisonType)
		{
			if(string.IsNullOrEmpty(text) || string.IsNullOrEmpty(trimString))
				return text;

			while(text.EndsWith(trimString, comparisonType))
				text = text.Remove(text.Length - trimString.Length);

			return text;
		}

		public static string TrimStart(this string text, string trimString)
		{
			return TrimStart(text, trimString, StringComparison.OrdinalIgnoreCase);
		}

		public static string TrimStart(this string text, string trimString, StringComparison comparisonType)
		{
			if(string.IsNullOrEmpty(text) || string.IsNullOrEmpty(trimString))
				return text;

			while(text.StartsWith(trimString, comparisonType))
				text = text.Remove(0, trimString.Length);

			return text;
		}

		public static bool In(this string text, IEnumerable<string> collection, StringComparison comparisonType)
		{
			if(collection == null)
				return false;

			return collection.Any(item => string.Equals(item, text, comparisonType));
		}

		public static bool IsDigits(this string text)
		{
			return IsDigits(text, out _);
		}

		public static bool IsDigits(this string text, out string digits)
		{
			digits = null;

			if(text == null || text.Length == 0)
				return false;

			int start = -1, count = 0;
			var isDivied = false;

			for(int i = 0; i < text.Length; i++)
			{
				if(char.IsWhiteSpace(text, i))
				{
					isDivied = start >= 0;
					continue;
				}

				if(text[i] >= '0' && text[i] <= '9')
				{
					if(start < 0)
						start = i;
					else if(isDivied)
						return false;

					count++;
				}
				else
				{
					return false;
				}
			}

			if(start >= 0)
			{
				digits = text.Substring(start, count);
				return true;
			}

			return false;
		}

		public static string Mask(this string text, int prefix, int suffix, int start = 0, int count = 0)
		{
			if(string.IsNullOrEmpty(text))
				return text;

			if(start < 0 || start > text.Length - 1)
				throw new ArgumentOutOfRangeException(nameof(start));

			if(count <= 0)
				count = text.Length - start;
			else if(start + count > text.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			prefix = Math.Max(0, prefix);

			if(prefix > count)
				prefix = count;

			if(suffix < 0)
				suffix = Math.Min(-suffix, (count - prefix) / 2);
			else if(suffix > count)
				suffix = count;

			suffix = Math.Min(suffix, count - prefix);

			var characters = new char[text.Length];

			for(int i = 0; i < text.Length; i++)
			{
				if(i < start || i > start + count)
				{
					characters[i] = text[i];
					continue;
				}

				if(i < start + prefix || i >= start + count - suffix)
					characters[i] = text[i];
				else
					characters[i] = '*';
			}

			return new string(characters);
		}

		public static IEnumerable<string> Slice(this string text, char separator)
		{
			return Slice<string>(text, 0, 0, chr => chr == separator, null);
		}

		public static IEnumerable<string> Slice(this string text, int start, int count, char separator)
		{
			return Slice<string>(text, start, count, chr => chr == separator, null);
		}

		public static IEnumerable<string> Slice(this string text, params char[] separators)
		{
			if(separators == null || separators.Length == 0)
				return null;

			return Slice<string>(text, 0, 0, chr => separators.Contains(chr), null);
		}

		public static IEnumerable<string> Slice(this string text, int start, int count, params char[] separators)
		{
			if(separators == null || separators.Length == 0)
				return null;

			return Slice<string>(text, start, count, chr => separators.Contains(chr), null);
		}

		public static IEnumerable<string> Slice(this string text, Func<char, bool> separator)
		{
			return Slice<string>(text, 0, 0, separator, null);
		}

		public static IEnumerable<string> Slice(this string text, int start, int count, Func<char, bool> separator)
		{
			return Slice<string>(text, start, count, separator, null);
		}

		public static IEnumerable<T> Slice<T>(this string text, char separator, TryParser<T> parser)
		{
			return Slice(text, 0, 0, chr => chr == separator, parser);
		}

		public static IEnumerable<T> Slice<T>(this string text, int start, int count, char separator, TryParser<T> parser)
		{
			return Slice(text, start, count, chr => chr == separator, parser);
		}

		public static IEnumerable<T> Slice<T>(this string text, char[] separators, TryParser<T> parser)
		{
			if(separators == null || separators.Length == 0)
				return null;

			return Slice(text, 0, 0, chr => separators.Contains(chr), parser);
		}

		public static IEnumerable<T> Slice<T>(this string text, int start, int count, char[] separators, TryParser<T> parser)
		{
			if(separators == null || separators.Length == 0)
				return null;

			return Slice(text, start, count, chr => separators.Contains(chr), parser);
		}

		public static IEnumerable<T> Slice<T>(this string text, Func<char, bool> separator, TryParser<T> parser)
		{
			return Slice<T>(text, 0, 0, separator, parser);
		}

		public static IEnumerable<T> Slice<T>(this string text, int start, int count, Func<char, bool> separator, TryParser<T> parser)
		{
			if(separator == null)
				throw new ArgumentNullException(nameof(separator));

			if(parser == null && typeof(T) != typeof(string))
				throw new ArgumentNullException(nameof(parser));

			if(string.IsNullOrEmpty(text))
				yield break;

			if(start < 0 || start >= text.Length)
				throw new ArgumentOutOfRangeException(nameof(start));

			if(count > 0 && start + count > text.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			int length = count < 1 ? text.Length : start + count;
			int index = start - 1; //分隔符的位置
			int tails = 0;         //尾巴空白字符数
			string part;
			T value;

			for(int i = start; i < length; i++)
			{
				if(char.IsWhiteSpace(text, i))
				{
					if(i == index + 1)
						index++;
					else
						tails++;

					continue;
				}

				if(separator(text[i]))
				{
					part = text.Substring(++index, i - index - tails);
					index = i;

					if(part.Length > 0)
					{
						if(parser == null)
							yield return (T)(object)part;
						else if(parser(part, out value))
							yield return value;
					}
				}

				tails = 0;
			}

			if(index < text.Length - 1)
			{
				index = index < 0 ? start : index + 1;
				part = text.Substring(index, text.Length - index - tails);

				if(parser == null)
					yield return (T)(object)part;
				else if(parser(part, out value))
					yield return value;
			}
		}
	}
}
