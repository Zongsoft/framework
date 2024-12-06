using System;
using System.Text.RegularExpressions;

namespace Zongsoft.Components.Samples.Models;

public partial class Failure
{
	#region 构造函数
	public Failure(string message) => this.Message = message;
	public Failure(int code, string message)
	{
		this.Code = code;
		this.Message = message;
	}
	#endregion

	#region 公共属性
	public int Code { get; }
	public string Message { get; }
	#endregion

	#region 解析方法
	private static readonly Regex _regex = new(@"(\[(?<code>\d+)\])?\s*(?<message>.+)", RegexOptions.Compiled | RegexOptions.Singleline);
	public static Failure Parse(string message) => TryParse(message, out var result) ? result : default;
	public static bool TryParse(ReadOnlySpan<char> text, out Failure result)
	{
		const string CODE = "code";
		const string MESSAGE = "message";

		if(!text.IsEmpty)
		{
			var match = _regex.Match(text.ToString());

			if(match.Success)
			{
				if(match.Groups[CODE].Success && int.TryParse(match.Groups[CODE].ValueSpan, out var code))
					result = new Failure(code, match.Groups[MESSAGE].Success ? match.Groups[MESSAGE].Value : string.Empty);
				else
					result = new Failure(match.Groups[MESSAGE].Success ? match.Groups[MESSAGE].Value : string.Empty);

				return true;
			}
		}

		result = default;
		return false;
	}
	#endregion

	#region 重写方法
	public override string ToString() => this.Code == 0 ? this.Message : $"[{this.Code}]{this.Message}";
	#endregion
}