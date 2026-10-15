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
using System.Globalization;
using System.Security.Cryptography;

namespace Zongsoft.Security;

/// <summary>表示密码信息的结构。</summary>
/// <remarks>
/// 	<para>
/// 	文本格式：<c>algorithm#factor:nonce|value</c>，如果 <c>factor</c> 为零，则该部分可省略。<br />
/// 	譬如：<c>SHA256:1A2B3C4D5E6F7890|Base64String</c> 或 <c>SHA256#10:1A2B3C4D5E6F7890|Base64String</c>。
/// 	</para>
/// 	<para>迭代 <see cref="Factor">因子</see> 的取值范围为 <c>0</c>～<c>255</c>，默认值为 <c>10</c>；如果 <see cref="Factor">因子</see> 为零则表示关闭额外迭代，仅执行一次 <seealso href="https://wikipedia.org/wiki/PBKDF2">PBKDF2</seealso>，非零时迭代次数为 <see cref="Factor">因子</see> 与 <see cref="ushort.MaxValue"/> 的乘积。</para>
/// 	<para>字节格式：</para>
/// 	<list type="bullet">
/// 		<item><c>identifier|algorithm|factor|nonce-length|nonce|value</c></item>
/// 		<item><c>2 bytes   |1 byte   |1 byte|1 byte      |  n  |⩾16 </c></item>
/// 	</list>
/// </remarks>
public readonly partial struct Password : IEquatable<Password>, ISpanParsable<Password>
{
	#region 常量定义
	private const int MINIMUM_LENGTH = 22;

	private const byte ALGORITHM_MD5 = 0;
	private const byte ALGORITHM_SHA1 = 1;
	private const byte ALGORITHM_SHA256 = 2;
	private const byte ALGORITHM_SHA384 = 3;
	private const byte ALGORITHM_SHA512 = 4;

	private const int ALGORITHM = 2;
	private const int FACTOR = 3;
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
	/// <summary>获取一个值，指示当前密码信息是否为空。</summary>
	/// <value>密码信息为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</value>
	public bool IsEmpty => _data == null || _data.Length == 0;

	/// <summary>获取一个值，指示当前密码信息是否包含数据。</summary>
	/// <value>密码信息包含数据时为 <see langword="true"/>，否则为 <see langword="false"/>。</value>
	public bool HasValue => _data != null && _data.Length > 0;

	/// <summary>获取密码哈希的算法名称。</summary>
	/// <value><see cref="HashAlgorithmName"/> 对应的算法名称；密码信息为空时为 <see langword="null"/>。</value>
	public string Algorithm => _data != null && _data.Length >= MINIMUM_LENGTH ? GetAlgorithm(_data[ALGORITHM]) : null;

	/// <summary>获取密码哈希的迭代因子。</summary>
	/// <value>介于 <see cref="byte.MinValue"/> 和 <see cref="byte.MaxValue"/> 之间的迭代因子；密码信息为空时为零。</value>
	/// <remarks>因子为零时关闭额外迭代，仅执行一次 <seealso href="https://wikipedia.org/wiki/PBKDF2">PBKDF2</seealso>；非零时迭代次数为此因子与 <see cref="ushort.MaxValue"/> 的乘积。默认生成因子为 <c>10</c>。</remarks>
	public byte Factor => _data != null && _data.Length >= MINIMUM_LENGTH ? _data[FACTOR] : (byte)0;

	/// <summary>获取密码哈希使用的随机盐。</summary>
	/// <value>随机盐的只读字节视图；密码信息为空时为 <see cref="ReadOnlySpan{T}.Empty"/>。</value>
	public ReadOnlySpan<byte> Nonce => _data == null ? default : _data.AsSpan().Slice(NONCE, GetNonceLength(_data));

	/// <summary>获取密码的派生哈希值。</summary>
	/// <value>当密码信息为空时为 <see cref="ReadOnlySpan{T}.Empty"/>。</value>
	public ReadOnlySpan<byte> Value => _data == null ? default : _data.AsSpan().Slice(NONCE_LENGTH + GetNonceLength(_data) + 1, GetValueLength(_data));
	#endregion

	#region 公共方法
	/// <summary>验证指定密码是否与当前密码信息匹配。</summary>
	/// <param name="password">待验证的密码；允许为 <see langword="null"/> 或空字符串。</param>
	/// <returns><paramref name="password"/> 为空时返回 <see cref="IsEmpty"/> 的值；否则，密码匹配时为 <see langword="true"/>，不匹配时为 <see langword="false"/>。</returns>
	/// <remarks>使用 <see cref="Algorithm"/>、<see cref="Factor"/> 和 <see cref="Nonce"/> 重新派生哈希值，并与 <see cref="Value"/> 比较。</remarks>
	public bool Verify(string password)
	{
		if(string.IsNullOrEmpty(password))
			return this.IsEmpty;

		if(this.IsEmpty)
			return false;

		var data = Generate(password, this.Factor, this.Nonce, this.Algorithm);
		return this.Value.SequenceEqual(data);
	}
	#endregion

	#region 重写方法
	public bool Equals(Password other) => _data == null || _data.Length == 0 ?
		other._data == null || other._data.Length == 0 :
		_data.AsSpan().SequenceEqual(other._data);

	public override bool Equals(object obj) => obj is Password other && this.Equals(other);
	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.AddBytes(_data ?? []);
		return hash.ToHashCode();
	}

	public override string ToString()
	{
		if(_data == null || _data.Length == 0)
			return string.Empty;

		return this.Factor == 0 ?
			$"{this.Algorithm}:{Convert.ToHexString(this.Nonce)}|{Convert.ToBase64String(this.Value)}" :
			$"{this.Algorithm}#{this.Factor}:{Convert.ToHexString(this.Nonce)}|{Convert.ToBase64String(this.Value)}";
	}
	#endregion

	#region 符号重写
	public static implicit operator byte[](Password password) => password._data == null ? null : (byte[])password._data.Clone();
	public static bool operator ==(Password left, Password right) => left.Equals(right);
	public static bool operator !=(Password left, Password right) => !(left == right);
	#endregion

	#region 静态方法
	/// <summary>将密码文本解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为 <see langword="null"/> 或空字符串。</param>
	/// <returns>解析得到的密码信息；<paramref name="text"/> 为空时为 <see langword="default"/>。</returns>
	/// <exception cref="FormatException"><paramref name="text"/> 不是有效的密码文本。</exception>
	public static Password Parse(string text) => Parse(text.AsSpan());
	/// <summary>将密码文本的只读视图解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为空。</param>
	/// <returns>解析得到的密码信息；<paramref name="text"/> 为空时为 <see langword="default"/>。</returns>
	/// <exception cref="FormatException"><paramref name="text"/> 不是有效的密码文本。</exception>
	public static Password Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException(Properties.Resources.InvalidFormat_Message);

	/// <summary>尝试将密码文本解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="result">解析成功时为解析得到的密码信息；输入为空或解析失败时为 <see langword="default"/>。</param>
	/// <returns>解析成功或 <paramref name="text"/> 为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	public static bool TryParse(string text, out Password result) => TryParse(text == null ? [] : text.AsSpan(), out result);
	/// <summary>尝试将密码文本的只读视图解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为空。</param>
	/// <param name="result">解析成功时为解析得到的密码信息；输入为空或解析失败时为 <see langword="default"/>。</param>
	/// <returns>解析成功或 <paramref name="text"/> 为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, out Password result)
	{
		if(text.IsEmpty)
		{
			result = default;
			return true;
		}

		var index = text.IndexOf(':');

		if(index <= 0 || index >= text.Length - 1)
		{
			result = default;
			return false;
		}

		var offset = text[..index].LastIndexOf('#');
		var algorithm = ReadOnlySpan<char>.Empty;
		byte factor = 0;

		if(offset > 0)
		{
			algorithm = text[..offset];

			if(!byte.TryParse(text.Slice(offset + 1, index - offset - 1), CultureInfo.InvariantCulture, out factor))
			{
				result = default;
				return false;
			}
		}
		else
		{
			algorithm = text[..index];
		}

		var name = GetAlgorithm(algorithm.ToString(), out var code, out var length);
		if(string.IsNullOrEmpty(name.Name))
		{
			result = default;
			return false;
		}

		offset = IndexOf(text, '|', index);

		if(offset < index)
		{
			result = default;
			return false;
		}

		byte[] nonce;

		try
		{
			nonce = Convert.FromHexString(text.Slice(index + 1, offset - index - 1));
		}
		catch(FormatException)
		{
			result = default;
			return false;
		}

		if(nonce.Length < 1 || nonce.Length > byte.MaxValue)
		{
			result = default;
			return false;
		}

		Span<byte> value = stackalloc byte[length];
		if(!Convert.TryFromBase64Chars(text[(offset + 1)..], value, out var size) || size != length)
		{
			result = default;
			return false;
		}

		var data = new byte[nonce.Length + value.Length + 5];
		data[0] = (byte)'Z';
		data[1] = (byte)'S';
		data[2] = code;
		data[3] = factor;
		data[4] = (byte)nonce.Length;

		nonce.CopyTo(data, 5);
		value.CopyTo(data.AsSpan(5 + nonce.Length));

		result = new(data);
		return true;
	}

	/// <summary>尝试将密码字节数据解析为密码信息。</summary>
	/// <param name="data">符合 <see cref="Password"/> 字节格式的密码信息；允许为 <see langword="null"/> 或空数组。</param>
	/// <param name="result">解析成功时为解析得到的密码信息；输入为空或解析失败时为 <see langword="default"/>。</param>
	/// <returns>解析成功或 <paramref name="data"/> 为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	public static bool TryParse(byte[] data, out Password result) => TryParse(data == null ? [] : data.AsSpan(), out result);
	/// <summary>尝试将密码字节数据的只读视图解析为密码信息。</summary>
	/// <param name="data">符合 <see cref="Password"/> 字节格式的密码信息；允许为空。</param>
	/// <param name="result">解析成功时为解析得到的密码信息；输入为空或解析失败时为 <see langword="default"/>。</param>
	/// <returns>解析成功或 <paramref name="data"/> 为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	public static bool TryParse(ReadOnlySpan<byte> data, out Password result)
	{
		if(data.IsEmpty)
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

		result = new(data.ToArray());
		return true;
	}

	/// <summary>使用默认迭代因子和随机盐生成密码哈希信息。</summary>
	/// <param name="password">待哈希的密码；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="algorithm">哈希算法名称，默认为 <see cref="HashAlgorithmName.SHA256"/>。</param>
	/// <returns>生成的密码信息；<paramref name="password"/> 为空时为 <see langword="default"/>。</returns>
	public static Password Generate(string password, string algorithm = "SHA256") => Generate(password, [], 10, algorithm);
	/// <summary>使用指定迭代因子和随机盐生成密码哈希信息。</summary>
	/// <param name="password">待哈希的密码；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="factor">迭代因子，超出 <see cref="byte.MinValue"/> 至 <see cref="byte.MaxValue"/> 范围时限制为最近的边界值。</param>
	/// <param name="algorithm">哈希算法名称，默认为 <see cref="HashAlgorithmName.SHA256"/>。</param>
	/// <returns>生成的密码信息；<paramref name="password"/> 为空时为 <see langword="default"/>。</returns>
	public static Password Generate(string password, int factor, string algorithm = "SHA256") => Generate(password, [], factor, algorithm);
	/// <summary>使用默认迭代因子和指定盐值生成密码哈希信息。</summary>
	/// <param name="password">待哈希的密码；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="nonce">盐值，非空时长度为 4～255 字节；为空时自动生成随机盐。</param>
	/// <param name="algorithm">哈希算法名称，默认为 <see cref="HashAlgorithmName.SHA256"/>。</param>
	/// <returns>生成的密码信息；<paramref name="password"/> 为空时为 <see langword="default"/>。</returns>
	public static Password Generate(string password, ReadOnlySpan<byte> nonce, string algorithm = "SHA256") => Generate(password, nonce, 10, algorithm);
	/// <summary>使用指定盐值和迭代因子生成密码哈希信息。</summary>
	/// <param name="password">待哈希的密码；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="nonce">盐值，非空时长度为 4～255 字节；为空时自动生成随机盐。</param>
	/// <param name="factor">迭代因子，超出 <see cref="byte.MinValue"/> 至 <see cref="byte.MaxValue"/> 范围时限制为最近的边界值。</param>
	/// <param name="algorithm">哈希算法名称，默认为 <see cref="HashAlgorithmName.SHA256"/>。</param>
	/// <returns>生成的密码信息；<paramref name="password"/> 为空时为 <see langword="default"/>。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="nonce"/> 非空且长度小于 4 或大于 255 字节。</exception>
	/// <exception cref="ArgumentException"><paramref name="algorithm"/> 不受支持。</exception>
	public static Password Generate(string password, ReadOnlySpan<byte> nonce, int factor, string algorithm = "SHA256")
	{
		if(string.IsNullOrEmpty(password))
			return default;

		if(nonce.Length > byte.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(nonce), Properties.Resources.Password_NonceTooLong_Message);

		if(nonce.Length > 0 && nonce.Length < 4)
			throw new ArgumentOutOfRangeException(nameof(nonce), Properties.Resources.Password_NonceTooShort_Message);

		//获取哈希算法名及对应的算法代号和哈希值长度
		var name = GetAlgorithm(algorithm, out var code, out var length);

		if(name == default || length < 1)
			throw new ArgumentException(null, nameof(algorithm));

		//获取随机数长度
		var size = nonce.IsEmpty ? (byte)Random.Shared.Next(8, 16) : (byte)nonce.Length;
		var data = new byte[length + size + 5];
		var span = data.AsSpan();

		//设置标识符(ZS)
		span[0] = (byte)'Z';
		span[1] = (byte)'S';
		//设置算法代号
		span[2] = code;
		//设置哈希迭代因子
		span[3] = GetFactor(factor);
		//设置随机数长度
		span[4] = size;

		//设置随机数内容
		if(nonce.IsEmpty)
			RandomNumberGenerator.Fill(span.Slice(5, size));
		else
			nonce.CopyTo(span.Slice(5, size));

		//混淆哈希密码值
		Rfc2898DeriveBytes.Pbkdf2(
			password.AsSpan(),
			span.Slice(5, size),
			span.Slice(5 + size, length),
			GetIteration(span[FACTOR]),
			name);

		//返回密码信息结构
		return new(data);
	}
	#endregion

	#region 显式实现
	/// <summary>将密码文本解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="provider">格式提供者；密码文本格式固定，此参数被忽略。</param>
	/// <returns>解析得到的密码信息；<paramref name="text"/> 为空时为 <see langword="default"/>。</returns>
	/// <exception cref="FormatException"><paramref name="text"/> 不是有效的密码文本。</exception>
	static Password IParsable<Password>.Parse(string text, IFormatProvider provider) => Parse(text);
	/// <summary>尝试将密码文本解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为 <see langword="null"/> 或空字符串。</param>
	/// <param name="provider">格式提供者；密码文本格式固定，此参数被忽略。</param>
	/// <param name="result">解析成功时为解析得到的密码信息；输入为空或解析失败时为 <see langword="default"/>。</param>
	/// <returns>解析成功或 <paramref name="text"/> 为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	static bool IParsable<Password>.TryParse(string text, IFormatProvider provider, out Password result) => TryParse(text, out result);
	/// <summary>将密码文本的只读视图解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为空。</param>
	/// <param name="provider">格式提供者；密码文本格式固定，此参数被忽略。</param>
	/// <returns>解析得到的密码信息；<paramref name="text"/> 为空时为 <see langword="default"/>。</returns>
	/// <exception cref="FormatException"><paramref name="text"/> 不是有效的密码文本。</exception>
	static Password ISpanParsable<Password>.Parse(ReadOnlySpan<char> text, IFormatProvider provider) => Parse(text);
	/// <summary>尝试将密码文本的只读视图解析为密码信息。</summary>
	/// <param name="text">符合 <see cref="Password"/> 文本格式的密码信息；允许为空。</param>
	/// <param name="provider">格式提供者；密码文本格式固定，此参数被忽略。</param>
	/// <param name="result">解析成功时为解析得到的密码信息；输入为空或解析失败时为 <see langword="default"/>。</param>
	/// <returns>解析成功或 <paramref name="text"/> 为空时为 <see langword="true"/>，否则为 <see langword="false"/>。</returns>
	static bool ISpanParsable<Password>.TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Password result) => TryParse(text, out result);
	#endregion

	#region 私有方法
	private static byte[] Generate(string password, int factor, ReadOnlySpan<byte> nonce, string algorithm)
	{
		if(nonce.IsEmpty)
			throw new ArgumentNullException(nameof(nonce));

		return Rfc2898DeriveBytes.Pbkdf2(
			password,
			nonce,
			GetIteration(factor),
			GetAlgorithm(algorithm, out _, out var length),
			length);
	}

	private static byte GetNonceLength(ReadOnlySpan<byte> data) => data.Length >= MINIMUM_LENGTH ? data[NONCE_LENGTH] : (byte)0;
	private static byte GetValueLength(ReadOnlySpan<byte> data) => data.Length >= MINIMUM_LENGTH ? data[ALGORITHM] switch
	{
		ALGORITHM_MD5 => (byte)16,
		ALGORITHM_SHA1 => (byte)20,
		ALGORITHM_SHA256 => (byte)32,
		ALGORITHM_SHA384 => (byte)48,
		ALGORITHM_SHA512 => (byte)64,
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

	private static byte GetFactor(int factor) => (byte)Math.Clamp(factor, byte.MinValue, byte.MaxValue);
	private static int GetIteration(int factor) => Math.Max(1, GetFactor(factor) * ushort.MaxValue);

	private static int IndexOf<T>(ReadOnlySpan<T> data, T value, int offset) where T : IEquatable<T>
	{
		var position = MemoryExtensions.IndexOf(data[offset..], value);
		return position < 0 ? position : position + offset;
	}
	#endregion
}
