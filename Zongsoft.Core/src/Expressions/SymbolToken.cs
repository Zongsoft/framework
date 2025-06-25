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

namespace Zongsoft.Expressions;

/// <summary>
/// 表示符号词素的类。
/// </summary>
public class SymbolToken : Token
{
	#region 常量定义
	//加号或正号(+)
	private const string PLUS = "+";
	//减号或负号(-)
	private const string MINUS = "-";
	//乘号(*)
	private const string MULTIPLY = "*";
	//除号(/)
	private const string DIVIDE = "/";
	//取模(%)
	private const string MODULO = "%";
	//赋值(=)
	private const string ASSIGN = "=";
	//逻辑非(!)
	private const string NOT = "!";
	//逻辑与(&&)
	private const string ANDALSO = "&&";
	//逻辑或(||)
	private const string ORELSE = "||";
	//等于号(==)
	private const string EQUAL = "==";
	//不等于(!=)
	private const string NOTEQUAL = "!=";
	//小于(<)
	private const string LESSTHAN = "<";
	//小于等于(<=)
	private const string LESSTHANOREQUAL = "<=";
	//大于(>)
	private const string GREATERTHAN = ">";
	//大于等于(>=)
	private const string GREATERTHANOREQUAL = ">=";
	//空连接(??)
	private const string COALESCE = "??";
	//问号(?)
	private const string QUESTION = "?";
	//句点(.)
	private const string DOT = ".";
	//冒号(:)
	private const string COLON = ":";
	//逗号(,)
	private const string COMMA = ",";
	//分号(;)
	private const string SEMICOLON = ";";
	//管道符(|)
	private const string PIPELINE = "|";
	//左小括号：(
	private const string OPENINGPARENTHESIS = "(";
	//右小括号：)
	private const string CLOSINGPARENTHESIS = ")";
	//左中括号：[
	private const string OPENINGBRACKET = "[";
	//右中括号：]
	private const string CLOSINGBRACKET = "]";
	//左大括号：{
	private const string OPENINGBRACE = "{";
	//右大括号：}
	private const string CLOSINGBRACE = "}";
	#endregion

	#region 构造函数
	protected SymbolToken(string symbol) : base(TokenType.Symbol, symbol) { }
	#endregion

	#region 重写方法
	public override string ToString() => this.Value.ToString();
	#endregion

	#region 静态方法
	public static SymbolToken GetToken(string symbol)
	{
		if(string.IsNullOrWhiteSpace(symbol))
			throw new ArgumentNullException(nameof(symbol));

		//var fields = typeof(SymbolToken).GetFields(BindingFlags.Static | BindingFlags.Public);

		//foreach(var field in fields)
		//{
		//	if(field.FieldType != typeof(SymbolToken))
		//		continue;

		//	var token = (SymbolToken)field.GetValue(null);

		//	if(string.Equals((string)token.Value, symbol))
		//		return token;
		//}

		switch(symbol)
		{
			case PLUS:
				return SymbolToken.Plus;
			case MINUS:
				return SymbolToken.Minus;
			case MULTIPLY:
				return SymbolToken.Multiply;
			case DIVIDE:
				return SymbolToken.Divide;
			case MODULO:
				return SymbolToken.Modulo;
			case ASSIGN:
				return SymbolToken.Assign;
			case NOT:
				return SymbolToken.Not;
			case ANDALSO:
				return SymbolToken.AndAlso;
			case ORELSE:
				return SymbolToken.OrElse;
			case EQUAL:
				return SymbolToken.Equal;
			case NOTEQUAL:
				return SymbolToken.NotEqual;
			case LESSTHAN:
				return SymbolToken.LessThan;
			case LESSTHANOREQUAL:
				return SymbolToken.LessThanOrEqual;
			case GREATERTHAN:
				return SymbolToken.GreaterThan;
			case GREATERTHANOREQUAL:
				return SymbolToken.GreaterThanOrEqual;
			case COALESCE:
				return SymbolToken.Coalesce;
			case QUESTION:
				return SymbolToken.Question;
			case DOT:
				return SymbolToken.Dot;
			case COLON:
				return SymbolToken.Colon;
			case COMMA:
				return SymbolToken.Comma;
			case SEMICOLON:
				return SymbolToken.Semicolon;
			case PIPELINE:
				return SymbolToken.Pipeline;
			case OPENINGPARENTHESIS:
				return SymbolToken.OpeningParenthesis;
			case CLOSINGPARENTHESIS:
				return SymbolToken.ClosingParenthesis;
			case OPENINGBRACKET:
				return SymbolToken.OpeningBracket;
			case CLOSINGBRACKET:
				return SymbolToken.ClosingBracket;
			case OPENINGBRACE:
				return SymbolToken.OpeningBrace;
			case CLOSINGBRACE:
				return SymbolToken.ClosingBrace;
		}

		return new SymbolToken(symbol);
	}
	#endregion

	#region 静态字段
	public static readonly string[] Symbols =
	[
		PLUS, MINUS, MULTIPLY, DIVIDE, MODULO,
		ASSIGN, NOT, ANDALSO, ORELSE, EQUAL, NOTEQUAL,
		LESSTHAN, LESSTHANOREQUAL, GREATERTHAN, GREATERTHANOREQUAL,
		COALESCE, QUESTION, DOT, COLON, COMMA, SEMICOLON, PIPELINE,
		OPENINGPARENTHESIS, CLOSINGPARENTHESIS,
		OPENINGBRACKET, CLOSINGBRACKET,
		OPENINGBRACE, CLOSINGBRACE,
	];

	/// <summary>加号或正号(+)</summary>
	public static readonly SymbolToken Plus = new(PLUS);

	/// <summary>减号或负号(-)</summary>
	public static readonly SymbolToken Minus = new(MINUS);

	/// <summary>乘号(*)</summary>
	public static readonly SymbolToken Multiply = new(MULTIPLY);

	/// <summary>除号(/)</summary>
	public static readonly SymbolToken Divide = new(DIVIDE);

	/// <summary>取模(%)</summary>
	public static readonly SymbolToken Modulo = new(MODULO);

	/// <summary>赋值(=)</summary>
	public static readonly SymbolToken Assign = new(ASSIGN);

	/// <summary>逻辑非(!)</summary>
	public static readonly SymbolToken Not = new(NOT);

	/// <summary>逻辑与(&amp;&amp;)</summary>
	public static readonly SymbolToken AndAlso = new(ANDALSO);

	/// <summary>逻辑或(||)</summary>
	public static readonly SymbolToken OrElse = new(ORELSE);

	/// <summary>等于号(==)</summary>
	public static readonly SymbolToken Equal = new(EQUAL);

	/// <summary>不等于(!=)</summary>
	public static readonly SymbolToken NotEqual = new(NOTEQUAL);

	/// <summary>小于(&lt;)</summary>
	public static readonly SymbolToken LessThan = new(LESSTHAN);

	/// <summary>小于等于(&lt;=)</summary>
	public static readonly SymbolToken LessThanOrEqual = new(LESSTHANOREQUAL);

	/// <summary>大于(&gt;)</summary>
	public static readonly SymbolToken GreaterThan = new(GREATERTHAN);

	/// <summary>大于等于(&gt;=)</summary>
	public static readonly SymbolToken GreaterThanOrEqual = new(GREATERTHANOREQUAL);

	/// <summary>空连接(??)</summary>
	public static readonly SymbolToken Coalesce = new(COALESCE);

	/// <summary>问号(?)</summary>
	public static readonly SymbolToken Question = new(QUESTION);

	/// <summary>句点(.)</summary>
	public static readonly SymbolToken Dot = new(DOT);

	/// <summary>冒号(:)</summary>
	public static readonly SymbolToken Colon = new(COLON);

	/// <summary>逗号(,)</summary>
	public static readonly SymbolToken Comma = new(COMMA);

	/// <summary>分号(;)</summary>
	public static readonly SymbolToken Semicolon = new(SEMICOLON);

	/// <summary>管道符(|)</summary>
	public static readonly SymbolToken Pipeline = new(PIPELINE);

	/// <summary>左小括号：(</summary>
	public static readonly SymbolToken OpeningParenthesis = new(OPENINGPARENTHESIS);

	/// <summary>右小括号：)</summary>
	public static readonly SymbolToken ClosingParenthesis = new(CLOSINGPARENTHESIS);

	/// <summary>左中括号：[</summary>
	public static readonly SymbolToken OpeningBracket = new(OPENINGBRACKET);

	/// <summary>右中括号：]</summary>
	public static readonly SymbolToken ClosingBracket = new(CLOSINGBRACKET);

	/// <summary>左大括号：{</summary>
	public static readonly SymbolToken OpeningBrace = new(OPENINGBRACE);

	/// <summary>右大括号：}</summary>
	public static readonly SymbolToken ClosingBrace = new(CLOSINGBRACE);
	#endregion
}
