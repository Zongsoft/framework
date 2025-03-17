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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public readonly struct Member : IIdentifiable, IEquatable<Member>
{
	#region 构造函数
	public Member(Identifier memberId, MemberType memberType)
	{
		this.MemberId = memberId;
		this.MemberType = memberType;
	}
	#endregion

	#region 公共属性
	public Identifier MemberId { get; }
	public MemberType MemberType { get; }
	#endregion

	#region 静态方法
	public static Member Create(MemberType memberType, int id) => memberType switch
	{
		MemberType.Role => new(new Identifier(typeof(IRole), id), memberType),
		MemberType.User => new(new Identifier(typeof(IUser), id), memberType),
		_ => throw new ArgumentOutOfRangeException(nameof(memberType), memberType, null)
	};

	public static Member Create(MemberType memberType, uint id) => memberType switch
	{
		MemberType.Role => new(new Identifier(typeof(IRole), id), memberType),
		MemberType.User => new(new Identifier(typeof(IUser), id), memberType),
		_ => throw new ArgumentOutOfRangeException(nameof(memberType), memberType, null)
	};

	public static Member Create(MemberType memberType, string qualifiedName) => memberType switch
	{
		MemberType.Role => new(new Identifier(typeof(IRole), qualifiedName), memberType),
		MemberType.User => new(new Identifier(typeof(IUser), qualifiedName), memberType),
		_ => throw new ArgumentOutOfRangeException(nameof(memberType), memberType, null)
	};

	public static Member Role(int id) => new(new Identifier(typeof(IRole), id), MemberType.Role);
	public static Member Role(uint id) => new(new Identifier(typeof(IRole), id), MemberType.Role);
	public static Member Role(string qualifiedName) => new(new Identifier(typeof(IRole), qualifiedName), MemberType.Role);
	public static Member Role(Identifier role) => new(role, MemberType.Role);

	public static Member User(int id) => new(new Identifier(typeof(IUser), id), MemberType.User);
	public static Member User(uint id) => new(new Identifier(typeof(IUser), id), MemberType.User);
	public static Member User(string qualifiedName) => new(new Identifier(typeof(IUser), qualifiedName), MemberType.User);
	public static Member User(Identifier user) => new(user, MemberType.User);
	#endregion

	#region 解析方法
	public static Member Parse(string text) => Parse(text.AsSpan());
	public static Member Parse(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty)
			throw new ArgumentNullException(nameof(text));

		//成员类型在前，成员标识在后（MemberType:MemberId）
		var index = text.IndexOf(':');

		if(index > 0)
		{
			var type = text[..index];
			var name = text[(index + 1)..];
			return Parse(name, type);
		}

		//成员标识在前，成员类型在后（MemberId@MemberType）
		index = text.IndexOf('@');

		if(index > 0)
		{
			var name = text[..index];
			var type = text[(index + 1)..];
			return Parse(name, type);
		}

		throw new InvalidOperationException($"Invalid format.");

		static Member Parse(ReadOnlySpan<char> name, ReadOnlySpan<char> type)
		{
			if(Enum.TryParse<MemberType>(type, true, out var memberType))
			{
				return memberType switch
				{
					MemberType.Role => new(new Identifier(typeof(IRole), name.ToString()), memberType),
					MemberType.User => new(new Identifier(typeof(IUser), name.ToString()), memberType),
					_ => throw new InvalidOperationException($"The specified '{type}' is an undefined entry of the {typeof(MemberType).Name} enum type."),
				};
			}

			throw new InvalidOperationException($"Cannot resolve '{type}' to {typeof(MemberType).Name} type.");
		}
	}

	public static bool TryParse(string text, out Member result) => TryParse(text.AsSpan(), out result);
	public static bool TryParse(ReadOnlySpan<char> text, out Member result)
	{
		if(text.IsEmpty)
		{
			result = default;
			return false;
		}

		//成员类型在前，成员标识在后（MemberType:MemberId）
		var index = text.IndexOf(':');

		if(index > 0)
		{
			var type = text[..index];
			var name = text[(index + 1)..];
			return TryParse(name, type, out result);
		}

		//成员标识在前，成员类型在后（MemberId@MemberType）
		index = text.IndexOf('@');

		if(index > 0)
		{
			var name = text[..index];
			var type = text[(index + 1)..];
			return TryParse(name, type, out result);
		}

		result = default;
		return false;

		static bool TryParse(ReadOnlySpan<char> name, ReadOnlySpan<char> type, out Member result)
		{
			if(Enum.TryParse<MemberType>(type, true, out var memberType))
			{
				result = memberType switch
				{
					MemberType.Role => new(new Identifier(typeof(IRole), name.ToString()), memberType),
					MemberType.User => new(new Identifier(typeof(IUser), name.ToString()), memberType),
					_ => default,
				};

				return result.MemberId.HasValue;
			}

			result = default;
			return false;
		}
	}
	#endregion

	#region 重写方法
	public readonly bool Equals(Member other) =>
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;
	public override readonly bool Equals(object obj) => obj is Member other && this.Equals(other);
	public override readonly int GetHashCode() => HashCode.Combine(this.MemberId, this.MemberType);
	public override readonly string ToString() => $"{this.MemberId}@{this.MemberType}";
	#endregion

	#region 显式实现
	Identifier IIdentifiable.Identifier => this.MemberId.IsEmpty ? default : new(typeof(Member), this);
	#endregion

	#region 符号重载
	public static implicit operator Identifier(Member member) => ((IIdentifiable)member).Identifier;
	public static bool operator ==(Member left, Member right) => left.Equals(right);
	public static bool operator !=(Member left, Member right) => !(left == right);
	#endregion
}
