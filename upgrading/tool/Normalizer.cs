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
 * The above copyright notice and this permission notice shall be included in all
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.Upgrading;

public static class Normalizer
{
	#region 常量定义
	//变量解析的正则组名称
	private const string REGEX_VARIABLE_NAME = "name";
	//变量解析的正则表达式（变量包括两种语法：$(variable) 或 %variable%）
	private static readonly Regex _variableRegex = new(@"(?<opt>\$\((?<name>\w+)\))|(?<env>\%(?<name>\w+)\%)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	#endregion

	#region 公共方法
	public static bool TryNormalize(string text, IDictionary<string, string> variables, out string result)
	{
		var normalized = Normalize(text, variables);
		if(normalized)
		{
			result = normalized.Value;
			return true;
		}

		Dumper.UndefinedVariable(normalized.Value, text);
		result = null;
		return false;
	}

	public static Result Normalize(string text, IDictionary<string, string> variables)
	{
		if(string.IsNullOrWhiteSpace(text))
			return Result.Success(string.Empty);

		try
		{
			var value = _variableRegex.Replace(text.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), match =>
			{
				if(match.Success && match.Groups.TryGetValue(REGEX_VARIABLE_NAME, out var group))
				{
					if(variables.TryGetValue(group.Value, out var value))
						return value;

					throw new NormalizerException(group.Value);
				}

				return null;
			});

			return Result.Success(value);
		}
		catch(NormalizerException ex)
		{
			return Result.Failure(ex.Variable);
		}
	}
	#endregion

	#region 嵌套结构
	public readonly struct Result
	{
		private Result(string value, bool succeed)
		{
			this.Value = value;
			this.Succeed = succeed;
		}

		public readonly string Value;
		public readonly bool Succeed;

		public override string ToString() => this.Value;

		public static implicit operator bool(Result result) => result.Succeed;
		public static implicit operator string(Result result) => result.Value;

		public static Result Failure(string value) => new(value, false);
		public static Result Success(string value) => new(value, true);
	}
	#endregion

	#region 私有子类
	private sealed class NormalizerException : Exception
	{
		public NormalizerException(string variable) => this.Variable = variable;
		public string Variable { get; }
	}
	#endregion
}
