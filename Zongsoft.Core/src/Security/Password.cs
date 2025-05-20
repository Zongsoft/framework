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
using System.Security;
using System.Security.Cryptography;

namespace Zongsoft.Security;

/// <summary>表示密码信息的结构。</summary>
/// <remarks>
///		<para>其文本格式：<c>algorithm#exponent:nonce|value</c>，譬如：<c>SHA1:1A2B3C4D5E6F7890|xxxxxx</c> 或 <c>SHA1#10:1A2B3C4D5E6F7890|xxxxxx</c>。</para>
///		<para>其字节格式：</para>
///		<list type="bullet">
///			<item><c>identifier|algorithm|exponent|nonce-length|nonce|value</c></item>
///			<item><c>2 bytes   |1 byte   |1 byte  |1 byte      |..n..|>=16 </c></item>
///		</list>
/// </remarks>
public readonly struct Password : IEquatable<Password>
{
	#region 常量定义
	private const int MINIMUM_LENGTH = 22;

	private const byte ALGORITHM_MD5 = 0;
	private const byte ALGORITHM_SHA1 = 1;
	private const byte ALGORITHM_SHA256 = 2;
	private const byte ALGORITHM_SHA384 = 3;
	private const byte ALGORITHM_SHA512 = 4;

	private const int ALGORITHM = 2;
	private const int EXPONENT = 3;
	private const int NONCE = 5;
	private const int NONCE_LENGTH = 4;
	#endregion

	#region 成员字段
	private readonly byte[] _data;
	#endregion

	#region 私有构造
	private Password(byte[] data)
	{
		if(data == null || data.Length == 0)
			throw new ArgumentNullException(nameof(data));

		_data = data;
	}
	#endregion

	#region 公共属性
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public bool IsEmpty => _data == null || _data.Length == 0;

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public bool HasValue => _data != null && _data.Length > 0;

	public string Algorithm => _data != null && _data.Length >= MINIMUM_LENGTH ? GetAlgorithm(_data[ALGORITHM]) : null;
	public byte Exponent => _data != null && _data.Length >= MINIMUM_LENGTH ? _data[EXPONENT] : (byte)0;
	public ReadOnlySpan<byte> Nonce => _data == null ? default : _data.AsSpan().Slice(NONCE, GetNonceLength(_data));
	public ReadOnlySpan<byte> Value => _data == null ? default : _data.AsSpan().Slice(NONCE_LENGTH + GetNonceLength(_data) + 1, GetValueLength(_data));
	#endregion

	#region 公共方法
	public bool Verify(string password)
	{
		if(string.IsNullOrEmpty(password))
			return this.IsEmpty;

		if(this.IsEmpty)
			return false;

		var data = Generate(password, this.Exponent, this.Nonce, this.Algorithm);
		return this.Value.SequenceEqual(data);
	}
	#endregion

	#region 重写方法
	public bool Equals(Password other) => _data == null || _data.Length == 0 ?
		other._data == null || other._data.Length == 0 :
		_data.AsSpan().SequenceEqual(other._data);

	public override bool Equals(object obj) => obj is Password other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(_data);
	public override string ToString()
	{
		if(_data == null || _data.Length == 0)
			return string.Empty;

		return this.Exponent == 0 ?
			$"{this.Algorithm}:{Convert.ToHexString(this.Nonce)}|{Convert.ToBase64String(this.Value)}" :
			$"{this.Algorithm}#{this.Exponent}:{Convert.ToHexString(this.Nonce)}|{Convert.ToBase64String(this.Value)}";
	}
	#endregion

	#region 符号重写
	public static implicit operator byte[](Password password) => password._data;
	public static bool operator ==(Password left, Password right) => left.Equals(right);
	public static bool operator !=(Password left, Password right) => !(left == right);
	#endregion

	#region 静态方法
	public static bool TryParse(string text, out Password result)
	{
		if(string.IsNullOrEmpty(text))
		{
			result = default;
			return true;
		}

		throw new NotImplementedException();
	}

	public static bool TryParse(byte[] data, out Password result)
	{
		if(data == null || data.Length == 0)
		{
			result = default;
			return true;
		}

		if(data.Length < MINIMUM_LENGTH)
		{
			result = default;
			return false;
		}

		if((data[0] != (byte)'Z' && data[0] != (byte)'z') || (data[1] != (byte)'S' && data[1] != (byte)'s'))
		{
			result = default;
			return false;
		}

		if(string.IsNullOrEmpty(GetAlgorithm(data[ALGORITHM], out var length)))
		{
			result = default;
			return false;
		}

		var nonce = GetNonceLength(data);
		if(nonce == 0)
		{
			result = default;
			return false;
		}

		if(data.Length != nonce + length + 5)
		{
			result = default;
			return false;
		}

		result = new(data);
		return true;
	}

	public static Password Generate(string password, string algorithm = "SHA1") => Generate(password, 10, algorithm);
	public static Password Generate(string password, int exponent, string algorithm = "SHA1")
	{
		if(string.IsNullOrEmpty(password))
			return default;

		//获取哈希算法名及对应的算法代号和哈希值长度
		var name = GetAlgorithm(algorithm, out var code, out var length);

		if(name == default || length < 1)
			throw new ArgumentException(null, nameof(algorithm));

		//获取随机数长度，介于8至16个字节之间
		var size = (byte)Random.Shared.Next(8, 16);
		var data = new byte[length + size + 5];
		var span = data.AsSpan();

		//设置标识符(ZS)
		span[0] = (byte)'Z';
		span[1] = (byte)'S';
		//设置算法标识
		span[2] = code;
		//设置哈希强度(指数)
		span[3] = GetExponent(exponent);
		//设置随机数长度
		span[4] = size;
		//设置随机数内容
		RandomNumberGenerator.Fill(span.Slice(5, size));
		//混淆哈希密码值
		Rfc2898DeriveBytes.Pbkdf2(password.AsSpan(), span.Slice(5, size), span.Slice(5 + size, length), GetIteration(exponent), name);

		//返回密码信息结构
		return new(data);
	}
	#endregion

	#region 私有方法
	private static byte[] Generate(string password, int exponent, ReadOnlySpan<byte> nonce, string algorithm)
	{
		if(nonce.IsEmpty)
			throw new ArgumentNullException(nameof(nonce));

		return Rfc2898DeriveBytes.Pbkdf2(
			password,
			nonce,
			GetIteration(exponent),
			GetAlgorithm(algorithm, out _, out var length),
			length);
	}

	private static byte GetNonceLength(ReadOnlySpan<byte> data) => data.Length >= MINIMUM_LENGTH ? data[NONCE_LENGTH] : (byte)0;
	private static byte GetValueLength(ReadOnlySpan<byte> data) => data.Length >= MINIMUM_LENGTH ? data[ALGORITHM] switch
	{
		0 => (byte)16,
		1 => (byte)20,
		2 => (byte)32,
		3 => (byte)48,
		4 => (byte)64,
		_ => (byte)0,
	} : (byte)0;

	private static string GetAlgorithm(byte code) => code switch
	{
		ALGORITHM_MD5 => HashAlgorithmName.MD5.Name,
		ALGORITHM_SHA1 => HashAlgorithmName.SHA1.Name,
		ALGORITHM_SHA256 => HashAlgorithmName.SHA256.Name,
		ALGORITHM_SHA384 => HashAlgorithmName.SHA384.Name,
		ALGORITHM_SHA512 => HashAlgorithmName.SHA512.Name,
		_ => null,
	};

	private static string GetAlgorithm(byte code, out int length)
	{
		switch(code)
		{
			case ALGORITHM_MD5:
				length = 16;
				return HashAlgorithmName.MD5.Name;
			case ALGORITHM_SHA1:
				length = 20;
				return HashAlgorithmName.SHA1.Name;
			case ALGORITHM_SHA256:
				length = 32;
				return HashAlgorithmName.SHA256.Name;
			case ALGORITHM_SHA384:
				length = 48;
				return HashAlgorithmName.SHA384.Name;
			case ALGORITHM_SHA512:
				length = 64;
				return HashAlgorithmName.SHA512.Name;
			default:
				length = 0;
				return null;
		}
	}

	private static HashAlgorithmName GetAlgorithm(string name, out byte code, out int length)
	{
		if(string.Equals(name, HashAlgorithmName.MD5.Name, StringComparison.OrdinalIgnoreCase))
		{
			length = 16;
			code = ALGORITHM_MD5;
			return HashAlgorithmName.MD5;
		}

		if(string.Equals(name, HashAlgorithmName.SHA1.Name, StringComparison.OrdinalIgnoreCase))
		{
			length = 20;
			code = ALGORITHM_SHA1;
			return HashAlgorithmName.SHA1;
		}

		if(string.Equals(name, HashAlgorithmName.SHA256.Name, StringComparison.OrdinalIgnoreCase))
		{
			length = 32;
			code = ALGORITHM_SHA256;
			return HashAlgorithmName.SHA256;
		}

		if(string.Equals(name, HashAlgorithmName.SHA384.Name, StringComparison.OrdinalIgnoreCase))
		{
			length = 48;
			code = ALGORITHM_SHA384;
			return HashAlgorithmName.SHA384;
		}

		if(string.Equals(name, HashAlgorithmName.SHA512.Name, StringComparison.OrdinalIgnoreCase))
		{
			length = 64;
			code = ALGORITHM_SHA512;
			return HashAlgorithmName.SHA512;
		}

		code = 0;
		length = 0;
		return default;
	}

	private static byte GetExponent(int exponent) => (byte)Math.Clamp(exponent, 0, 31);
	private static int GetIteration(int exponent) => (int)Math.Pow(2, Math.Clamp(exponent, 0, 31));
	#endregion
}
