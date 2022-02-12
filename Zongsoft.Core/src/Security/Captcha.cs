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

namespace Zongsoft.Security
{
	/// <summary>
	/// 表示人机识别的模型结构。
	/// </summary>
	[Obsolete]
	public struct Captcha
	{
		#region 构造函数
		public Captcha(string scheme, string token, string data = null, string extra = null)
		{
			this.Scheme = scheme;
			this.Token = token;
			this.Data = data;
			this.Extra = extra;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置人机识别程序的标识。</summary>
		public string Scheme { get; set; }

		/// <summary>获取或设置人机识别的会话令牌。</summary>
		public string Token { get; set; }

		/// <summary>获取或设置人机识别的数据信息。</summary>
		public string Data { get; set; }

		/// <summary>获取或设置人机识别的附加信息。</summary>
		public string Extra { get; set; }

		/// <summary>获取一个值，指示是否为空结构。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool IsEmpty { get => string.IsNullOrEmpty(this.Scheme); }
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return string.IsNullOrEmpty(this.Scheme) ? string.Empty : $"{this.Scheme}:{this.Token}={this.Data}?{this.Extra}";
		}
		#endregion

		#region 解析方法
		/// <summary>
		/// 从字符串文本解析<see cref="Captcha"/>模型。
		/// </summary>
		/// <param name="text">待解析的字符串，其格式为：<c>scheme:token=data?extra</c>。</param>
		/// <returns>返回解析成功的<see cref="Captcha"/>模型，如果失败则抛出异常。</returns>
		public static Captcha Parse(string text)
		{
			if(string.IsNullOrEmpty(text))
				throw new ArgumentNullException(nameof(text));

			var (captcha, error) = ParseCore(text);

			if(error != null)
				throw error;

			return captcha.Value;
		}

		/// <summary>
		/// 尝试从字符串文本解析<see cref="Captcha"/>模型。
		/// </summary>
		/// <param name="text">待解析的字符串，其格式为：<c>scheme:token=data?extra</c>。</param>
		/// <param name="result">输出参数，表示解析成功后的<see cref="Captcha"/>模型。</param>
		/// <returns>返回一个值，指示是否解析成功。</returns>
		public static bool TryParse(string text, out Captcha result)
		{
			if(string.IsNullOrEmpty(text))
			{
				result = default;
				return false;
			}

			var (captcha, error) = ParseCore(text);
			result = captcha ?? default;
			return error == null && captcha.HasValue && !string.IsNullOrEmpty(captcha.Value.Scheme);
		}

		private static (Captcha? captcha, Exception error) ParseCore(string text)
		{
			if(string.IsNullOrEmpty(text))
				return default;

			var context = new Context(text);

			//状态驱动
			for(int i = 0; i < context.Length; i++)
			{
				context.Move(i);

				switch(context.State)
				{
					case State.None:
						if(!DoNone(ref context))
							return (null, context.GetError());

						break;
					case State.Scheme:
						if(!DoScheme(ref context))
							return (null, context.GetError());

						break;
					case State.Token:
						if(!DoToken(ref context))
							return (null, context.GetError());

						break;
					case State.Data:
						if(!DoData(ref context))
							return (null, context.GetError());

						break;
					case State.Extra:
						if(!DoExtra(ref context))
							return (null, context.GetError());

						break;
				}
			}

			//执行完成
			context.Final();

			return (context.Result, null);
		}
		#endregion

		#region 私有方法
		private static bool DoNone(ref Context context)
		{
			if(char.IsWhiteSpace(context.Character))
			{
				context.Accept();
				return true;
			}

			if(char.IsLetterOrDigit(context.Character) || context.Character == '_')
			{
				context.State = State.Scheme;
				return true;
			}

			context.Error($"Illegal character '{context.Character}' in the captcha scheme.");
			return false;
		}

		private static bool DoScheme(ref Context context)
		{
			if(context.Character == ':')
			{
				context.Result.Scheme = context.Accept(State.Token);
				return true;
			}

			if(char.IsLetterOrDigit(context.Character) || context.Character == '_')
				return true;

			context.Error($"Illegal character '{context.Character}' in the captcha scheme.");
			return false;
		}

		private static bool DoToken(ref Context context)
		{
			if(context.Character == '=')
			{
				context.Result.Token = context.Accept(State.Data);
				return true;
			}

			if(context.Character == '?')
			{
				context.Result.Token = context.Accept(State.Extra);
				return true;
			}

			return true;
		}

		private static bool DoData(ref Context context)
		{
			if(context.Character == '?')
			{
				context.Result.Data = context.Accept(State.Extra);
				return true;
			}

			return true;
		}

		private static bool DoExtra(ref Context context)
		{
			return true;
		}
		#endregion

		#region 嵌套结构
		private enum State
		{
			None,
			Scheme,
			Token,
			Data,
			Extra,
		}

		private ref struct Context
		{
			#region 私有变量
			private int _last;
			private int _current;
			private ReadOnlySpan<char> _text;
			private string _errorMessage;
			#endregion

			#region 公共字段
			public State State;
			public Captcha Result;
			public char Character;
			public int Length { get => _text.Length; }
			#endregion

			#region 构造函数
			public Context(string text)
			{
				_last = 0;
				_text = text.AsSpan();
				_current = 0;
				_errorMessage = null;

				this.Result = new Captcha();
				this.Character = text[0];
				this.State = State.None;
			}
			#endregion

			#region 公共方法
			public char Move(int index) => this.Character = _text[_current = index];
			public void Error(string message) => _errorMessage = message;
			public Exception GetError() => new ArgumentException(_errorMessage);

			public string Accept(State? state = null)
			{
				if(state.HasValue)
					this.State = state.Value;

				if(_last < _current)
				{
					var result = _current >= _text.Length ? _text.Slice(_last).ToString() : _text.Slice(_last, _current - _last).ToString();
					_last = _current + 1;
					return result;
				}

				return null;
			}

			public void Final()
			{
				if(_last < _current && _last < _text.Length)
				{
					var part = _text.Slice(_last).ToString();

					switch(this.State)
					{
						case State.Scheme:
							this.Result.Scheme = part;
							break;
						case State.Token:
							this.Result.Token = part;
							break;
						case State.Data:
							this.Result.Data = part;
							break;
						case State.Extra:
							this.Result.Extra = part;
							break;
					}
				}
			}
			#endregion
		}
		#endregion
	}
}
