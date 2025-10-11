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
using System.Collections.Generic;

namespace Zongsoft.Components;

public static partial class CommandLine
{
	#region 解析方法
	public static bool TryParse(ReadOnlySpan<char> text, out IReadOnlyList<Cmdlet> result)
	{
		result = Parse(text, null);
		return result != null;
	}

	public static IReadOnlyList<Cmdlet> Parse(ReadOnlySpan<char> text) => Parse(text, message => throw new ArgumentException(message));
	private static List<Cmdlet> Parse(ReadOnlySpan<char> text, Action<string> onError)
	{
		if(text.IsEmpty)
			return [];

		var context = new Context(text);
		var result = new List<Cmdlet>();
		Cmdlet cmdlet = null;

		while(context.Move())
		{
			switch(context.State)
			{
				case State.None:
					DoNone(ref context);
					break;
				case State.Command:
					if(DoCommand(ref context, out var name))
						cmdlet = new Cmdlet(name);
					break;
				case State.Argument:
					if(DoArgument(ref context, out var argument))
						cmdlet.Arguments.Add(argument);
					break;
				case State.OptionSign:
					DoOptionSign(ref context);
					break;
				case State.OptionName:
					if(DoOptionName(ref context, out var optionName, out var kind))
						cmdlet.Options.Add(new(kind, optionName));
					break;
				case State.OptionValue:
					if(DoOptionValue(ref context, out var optionValue))
						cmdlet.Options[^1].Value = optionValue;
					break;
				case State.Assigner:
					DoAssigner(ref context);
					break;
				case State.Gapping:
					DoGapping(ref context);
					break;
				case State.Connector:
					DoConnector(ref context);

					if(cmdlet != null)
						result.Add(cmdlet);
					cmdlet = null;

					break;
				case State.Error:
					onError?.Invoke(context.ErrorMessage);
					return null;
			}
		}

		switch(context.State)
		{
			case State.Command:
				cmdlet = new Cmdlet(context.GetValue().ToString());
				break;
			case State.OptionName:
				cmdlet.Options.Add(new(context.HasFlags(Flags.OptionFully) ? CmdletOptionKind.Fully : CmdletOptionKind.Short, context.GetValue().ToString()));
				break;
			case State.OptionValue:
				cmdlet.Options[^1].Value = context.GetValue().ToString();
				break;
			case State.Argument:
				cmdlet.Arguments.Add(context.GetValue().ToString());
				break;
		}

		if(cmdlet != null)
			result.Add(cmdlet);

		return result;
	}
	#endregion

	#region 状态处理
	private static bool DoNone(ref Context context)
	{
		if(context.IsWhitespace)
			return false;

		if(context.IsLetterOrDigitOrUnderscore)
		{
			context.Accept(State.Command);
			return true;
		}

		if(context.Character == '/')
		{
			context.Accept(State.Command, Flags.Slash);
			return true;
		}

		if(context.Character == '.')
		{
			context.Accept(State.Command, Flags.SingleDotted);
			return true;
		}

		context.Error();
		return false;
	}

	private static bool DoCommand(ref Context context, out string name)
	{
		if(context.IsWhitespace)
		{
			name = context.GetValue().ToString();
			context.Reset(State.Gapping);
			return true;
		}

		if(context.IsLetterOrDigitOrUnderscore)
		{
			if(context.HasFlags(Flags.DoubleDotted))
			{
				name = null;
				context.Error();
				return false;
			}

			name = null;
			context.Accept(Flags.None);
			return false;
		}

		if(context.Character == '/')
		{
			if(!context.HasFlags(Flags.Slash))
			{
				name = null;
				context.Accept(Flags.Slash);
				return false;
			}
		}

		if(context.Character == '.')
		{
			if(context.HasFlags(Flags.SingleDotted))
			{
				name = null;
				context.Accept(Flags.DoubleDotted);
				return false;
			}
			else if(context.HasFlags(Flags.DoubleDotted))
			{
				name = null;
				context.Error();
				return false;
			}
			else
			{
				name = null;
				context.Accept(Flags.SingleDotted);
				return false;
			}
		}

		name = null;
		context.Error();
		return false;
	}

	private static bool DoArgument(ref Context context, out string value)
	{
		if(!context.HasFlags())
		{
			if(context.IsWhitespace)
			{
				value = context.GetValue().ToString();
				context.Reset(State.Gapping);
				return true;
			}
		}

		if(context.Accept())
		{
			value = context.GetValue().ToString();
			context.Reset(State.Gapping);
			return true;
		}

		value = null;
		return false;
	}

	private static bool DoOptionSign(ref Context context)
	{
		if(context.Character == '-')
		{
			if(context.HasFlags(Flags.OptionFully))
			{
				context.Error();
				return false;
			}

			//注意：对于双横线必须忽略其横线，因此下面必须使用 Reset 而不能使用 Accept 方法
			context.Reset(State.OptionSign, Flags.OptionFully);
			return false;
		}

		if(context.IsLetterOrDigitOrUnderscore)
		{
			context.Accept(State.OptionName);
			return true;
		}

		context.Error();
		return false;
	}

	private static bool DoOptionName(ref Context context, out string key, out CmdletOptionKind kind)
	{
		if(context.IsWhitespace)
		{
			key = context.GetValue().ToString();
			kind = context.HasFlags(Flags.OptionFully) ? CmdletOptionKind.Fully : CmdletOptionKind.Short;
			context.Reset(State.Gapping, Flags.None);
			return true;
		}

		if(context.Character == ':' || context.Character == '=')
		{
			key = context.GetValue().ToString();
			kind = context.HasFlags(Flags.OptionFully) ? CmdletOptionKind.Fully : CmdletOptionKind.Short;
			if(string.IsNullOrEmpty(key))
				context.Error($"Option name is missing at the '{context.Offset + context.Index}' character position.");

			context.Reset(State.Assigner, Flags.None);
			return true;
		}

		if(context.IsLetterOrDigitOrUnderscore)
		{
			key = null;
			kind = 0;
			context.Accept();
			return false;
		}

		key = null;
		kind = 0;
		context.Error();
		return false;
	}

	private static bool DoOptionValue(ref Context context, out string value)
	{
		if(!context.HasFlags())
		{
			if(context.IsWhitespace)
			{
				value = context.GetValue().ToString();
				context.Reset(State.Gapping);
				return true;
			}
		}

		if(context.Accept())
		{
			value = context.GetValue().ToString();
			context.Reset(State.Gapping);
			return true;
		}

		value = null;
		return false;
	}

	private static bool DoAssigner(ref Context context)
	{
		if(context.IsWhitespace)
			return false;

		switch(context.Character)
		{
			case '"':
				context.Reset(State.OptionValue, Flags.DoubleQuotation);
				return true;
			case '\'':
				context.Reset(State.OptionValue, Flags.SingleQuotation);
				return true;
			case ':':
			case '=':
				context.Error();
				return false;
			default:
				context.Accept(State.OptionValue);
				return true;
		}
	}

	private static bool DoGapping(ref Context context)
	{
		if(context.IsWhitespace)
			return false;

		switch(context.Character)
		{
			case '-':
				context.Reset(State.OptionSign);
				return true;
			case '|':
				context.Reset(State.Connector);
				return true;
			case '"':
				context.Reset(State.Argument, Flags.DoubleQuotation);
				return true;
			case '\'':
				context.Reset(State.Argument, Flags.SingleQuotation);
				return true;
			default:
				context.Accept(State.Argument, Flags.None);
				return true;
		}
	}

	private static bool DoConnector(ref Context context) => DoNone(ref context);
	#endregion

	#region 嵌套结构
	private ref struct Context
	{
		#region 私有字段
		private readonly ReadOnlySpan<char> _text;
		private readonly int _offset;
		private State _state;
		private char _character;
		private int _index;
		private Flags _flags;
		private string _errorMessage;
		private int _whitespaces;
		private char[] _buffer;
		private int _bufferCount;
		#endregion

		#region 构造函数
		public Context(ReadOnlySpan<char> text, int offset = 0)
		{
			_text = text;
			_offset = offset;
			_state = State.None;
			_index = 0;
			_character = '\0';
			_flags = Flags.None;
			_bufferCount = 0;
			_whitespaces = 0;
			_errorMessage = null;
			_buffer = new char[text.Length];
		}
		#endregion

		#region 公共属性
		public readonly State State => _state;
		public readonly int Index => _index;
		public readonly int Offset => _offset;
		public readonly char Character => _character;
		public readonly string ErrorMessage => _errorMessage;
		public readonly bool IsWhitespace => char.IsWhiteSpace(_character);
		public readonly bool IsLetterOrUnderscore =>
			(this.Character >= 'a' && this.Character <= 'z') ||
			(this.Character >= 'A' && this.Character <= 'Z') || this.Character == '_';
		public readonly bool IsLetterOrDigitOrUnderscore =>
			(this.Character >= 'a' && this.Character <= 'z') ||
			(this.Character >= 'A' && this.Character <= 'Z') ||
			(this.Character >= '0' && this.Character <= '9') || this.Character == '_';
		#endregion

		#region 公共方法
		public bool Move()
		{
			if(_index < _text.Length)
			{
				_character = _text[_index++];
				return true;
			}

			_character = '\0';
			return false;
		}

		public void Error(string message = null)
		{
			_state = State.Error;
			_errorMessage = message ?? $"An illegal character '{this.Character}' was found at position {this.Offset + this.Index}.";
		}

		public readonly ReadOnlySpan<char> GetValue() => _bufferCount > 0 ? _buffer.AsSpan(0, _bufferCount) : default;

		public void Reset(State state, Flags flags = Flags.None)
		{
			_state = state;
			_bufferCount = 0;
			_whitespaces = 0;
			_flags = flags;
		}

		public bool Accept(Flags? flags = null) => this.Accept(_state, flags);
		public bool Accept(State state, Flags? flags = null)
		{
			if(_state != state)
				_state = state;

			if(flags.HasValue)
				_flags = flags.Value;

			//如果当前位置处于“单引号”或“双引号”内部
			if(this.HasFlags(Flags.SingleQuotation) || this.HasFlags(Flags.DoubleQuotation))
			{
				if(this.HasFlags(Flags.Escaping))
				{
					_flags &= ~Flags.Escaping;
					_buffer[_bufferCount++] = Escape(_character);
				}
				else
				{
					switch(_character)
					{
						case '\\':
							_flags |= Flags.Escaping;
							return false;
						case '"':
							if(this.HasFlags(Flags.DoubleQuotation))
							{
								_flags &= ~Flags.DoubleQuotation;
								return true;
							}
							break;
						case '\'':
							if(this.HasFlags(Flags.SingleQuotation))
							{
								_flags &= ~Flags.SingleQuotation;
								return true;
							}
							break;
					}

					_buffer[_bufferCount++] = _character;
				}
			}
			else
			{
				if(char.IsWhiteSpace(_character))
				{
					if(_bufferCount > 0)
						_whitespaces++;
				}
				else
				{
					for(int i = 0; i < _whitespaces; i++)
						_buffer[_bufferCount++] = ' ';

					_whitespaces = 0;
					_buffer[_bufferCount++] = _character;
				}
			}

			return false;
		}

		public readonly bool HasFlags() => _flags != Flags.None;
		public readonly bool HasFlags(Flags flags) => (_flags & flags) == flags;
		#endregion

		#region 私有方法
		private static char Escape(char chr) => chr switch
		{
			't' => '\t',
			'n' => '\n',
			'r' => '\r',
			_ => chr,
		};
		#endregion
	}
	#endregion

	#region 枚举定义
	private enum State
	{
		None,
		Command,
		Argument,
		OptionSign,
		OptionName,
		OptionValue,
		Assigner,
		Gapping,
		Connector,
		Error = 99,
	}

	[Flags]
	private enum Flags
	{
		None = 0,
		Slash = 8,           //路径部分：斜线(/)
		SingleDotted = 16,   //路径部分：单个点(.)
		DoubleDotted = 32,   //路径部分：两个点(..)
		OptionFully = 64,    //选项符号：双横线(--)
		Escaping = 4,        //转义符：反斜杠
		SingleQuotation = 1, //单引号
		DoubleQuotation = 2, //双引号
	}
	#endregion

	#region 嵌套子类
	public sealed class Cmdlet
	{
		public Cmdlet(string name)
		{
			this.Name = name;
			this.Options = new List<CmdletOption>();
			this.Arguments = new List<string>();
		}

		public string Name { get; }
		public IList<CmdletOption> Options { get; }
		public IList<string> Arguments { get; }

		public override string ToString() => $"{this.Name}({this.Options.Count},{this.Arguments.Count})";
	}

	public sealed class CmdletOption
	{
		public CmdletOption(CmdletOptionKind kind, string name, string value = null)
		{
			this.Kind = kind;
			this.Name = name;
			this.Value = value;
		}

		public CmdletOptionKind Kind { get; }
		public string Name { get; set; }
		public string Value { get; set; }

		public override string ToString() => this.Kind switch
		{
			CmdletOptionKind.Fully => string.IsNullOrEmpty(this.Value) ? $"--{this.Name}" : $"--{this.Name}:{this.Value}",
			CmdletOptionKind.Short => string.IsNullOrEmpty(this.Value) ? $"-{this.Name}" : $"-{this.Name}:{this.Value}",
			_ => string.IsNullOrEmpty(this.Value) ? this.Name : $"{this.Name}:{this.Value}",
		};
	}

	public enum CmdletOptionKind
	{
		Fully,
		Short,
	}
	#endregion
}
