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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Wechat
{
	public readonly struct Account : IEquatable<Account>
	{
		#region 构造函数
		public Account(AccountType type, string code, string secret = null, string description = null)
		{
			this.Type = type;
			this.Code = code;
			this.Secret = secret;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		public AccountType Type { get; }
		public string Code { get; }
		public string Secret { get; }
		public string Description { get;  }

		public bool IsEmpty { get => string.IsNullOrEmpty(this.Code); }
		#endregion

		#region 重写方法
		public bool Equals(Account other) => this.Type == other.Type && string.Equals(this.Code, other.Code);
		public override bool Equals(object obj) => obj is Account other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Type, this.Code);
		public override string ToString() => string.IsNullOrEmpty(this.Secret) ? $"{this.Type}:{this.Code}" : $"{this.Type}:{this.Code}({this.Secret})";
		#endregion

		#region 构建方法
		public static Account Applet(string code, string secret = null, string description = null) => new(AccountType.Applet, code, secret, description);
		public static Account Channel(string code, string secret = null, string description = null) => new(AccountType.Channel, code, secret, description);
		#endregion

		#region 符号重写
		public static bool operator ==(Account left, Account right) => left.Equals(right);
		public static bool operator !=(Account left, Account right) => !(left == right);
		#endregion

		#region 解析方法
		/// <summary>
		/// 将格式为<c>type:code=secret?description</c>文本解析为<see cref="Account"/>结构。
		/// </summary>
		/// <param name="text">指定待解析的文本。</param>
		/// <param name="result">输出参数，返回解析成功的<see cref="Account"/>结构。</param>
		/// <returns>如果解析成功则返回真(True)，否则返回假(False)。</returns>
		public static bool TryParse(ReadOnlySpan<char> text, out Account result)
		{
			result = default;
			if(text.IsEmpty)
				return false;

			var state = State.None;
			var index = 0;
			AccountType type = AccountType.Applet;
			string code = null, secret = null, description = null;

			for(int i = 0; i < text.Length; i++)
			{
				switch(state)
				{
					case State.None:
						state = DoNone(text[i]);
						break;
					case State.Type:
						state = DoType(text[i]);

						if(state != State.Error && state != State.Type)
						{
							state = TryGetType(text, i, out type) ? state : State.Error;
							index = i;
						}

						break;
					case State.Code:
						state = DoCode(text[i]);

						if(state != State.Error && state != State.Code)
						{
							code = text.Slice(index + 1, i - index).ToString();
							index = i;
						}

						break;
					case State.Secret:
						state = DoSecret(text[i]);

						if(state != State.Error && state != State.Code)
						{
							secret = text.Slice(index + 1, i - index).ToString();
							index = i;
						}

						break;
					case State.Description:
						state = DoDescription(text[i]);

						if(state != State.Error && state != State.Code)
						{
							description = text.Slice(index + 1, i - index).ToString();
							index = i;
						}

						break;
				}

				if(state == State.Error)
					return false;
			}

			if(index < text.Length - 1)
			{
				switch(state)
				{
					case State.Type:
						if(!Enum.TryParse(text.Slice(index + 1).Trim().ToString(), true, out type))
							return false;
						break;
					case State.Code:
						code = text.Slice(index + 1).ToString();
						break;
					case State.Secret:
						secret = text.Slice(index + 1).ToString();
						break;
					case State.Description:
						description = text.Slice(index + 1).ToString();
						break;
				}
			}

			result = new Account(type, code, secret, description);
			return true;

			static bool TryGetType(ReadOnlySpan<char> text, int index, out AccountType type)
			{
				type = default;

				if(index < 1)
					return false;

				return Enum.TryParse<AccountType>(text.Slice(0, index).Trim().ToString(), true, out type);
			}
		}

		private enum State
		{
			None,
			Type,
			Code,
			Secret,
			Description,
			Error = 99,
		}

		private static State DoNone(char character)
		{
			return char.IsLetter(character) ? State.Type : State.Error;
		}

		private static State DoType(char character)
		{
			return character switch
			{
				':' => State.Code,
				'=' => State.Secret,
				'?' or '(' => State.Description,
				_ => char.IsLetter(character) ? State.Type : State.Error,
			};
		}

		private static State DoCode(char character)
		{
			return character switch
			{
				'=' => State.Secret,
				'?' or '(' => State.Description,
				_ => char.IsLetterOrDigit(character) || character == '_' ? State.Code : State.Error,
			};
		}

		private static State DoSecret(char character)
		{
			return character switch
			{
				'?' or '(' => State.Description,
				_ => char.IsLetterOrDigit(character) || character == '_' ? State.Code : State.Error,
			};
		}

		private static State DoDescription(char character) => State.Description;
		#endregion
	}
}
