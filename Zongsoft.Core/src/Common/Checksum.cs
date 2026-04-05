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
using System.IO;
using System.Globalization;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace Zongsoft.Common;

/// <summary>表示校验码的结构。</summary>
public readonly partial struct Checksum : IEquatable<Checksum>, IParsable<Checksum>
{
	#region 私有字段
	private readonly int _hashcode;
	#endregion

	#region 构造函数
	public Checksum(byte[] value) : this(null, value) { }
	public Checksum(string name, byte[] value)
	{
		this.Name = (string.IsNullOrEmpty(name) ? Determine(value) : name) ?? string.Empty;
		this.Value = value ?? [];

		var hash = new HashCode();
		hash.Add(this.Name);
		hash.AddBytes(this.Value);
		_hashcode = hash.ToHashCode();
	}
	#endregion

	#region 公共字段
	/// <summary>校验算法名称。</summary>
	public readonly string Name;
	/// <summary>校验码的码值。</summary>
	public readonly byte[] Value;
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示当前验证码是否为空。</summary>
	public bool IsEmpty => string.IsNullOrEmpty(this.Name) || this.Value == null || this.Value.Length == 0;
	#endregion

	#region 公共方法
	public bool Verify(ReadOnlySpan<byte> data) => this.IsEmpty ? data.IsEmpty : this.Equals(Compute(this.Name, data));
	public bool Verify(Stream data) => this.IsEmpty ? data == null : this.Equals(Compute(this.Name, data));
	public async ValueTask<bool> VerifyAsync(Stream data, CancellationToken cancellation = default) => this.IsEmpty ? data == null : this.Equals(await ComputeAsync(this.Name, data, cancellation));
	#endregion

	#region 静态方法
	/// <summary>使用指定哈希算法计算指定数据的校验码。</summary>
	/// <param name="name">指定的哈希算法名称。</param>
	/// <param name="data">指定的待计算的数据。</param>
	/// <returns>返回的校验码。</returns>
	/// <exception cref="InvalidOperationException">指定的 <paramref name="name"/> 参数值不是一个有效的哈希算法名。</exception>
	public static Checksum Compute(string name, ReadOnlySpan<byte> data)
	{
		if(string.IsNullOrEmpty(name))
			return new(HashAlgorithmName.SHA1.Name, SHA1.HashData(data));

		return name.ToUpperInvariant() switch
		{
			"MD5" => new(HashAlgorithmName.MD5.Name, MD5.HashData(data)),
			"SHA1" => new(HashAlgorithmName.SHA1.Name, SHA1.HashData(data)),
			"SHA256" => new(HashAlgorithmName.SHA256.Name, SHA256.HashData(data)),
			"SHA384" => new(HashAlgorithmName.SHA384.Name, SHA384.HashData(data)),
			"SHA512" => new(HashAlgorithmName.SHA512.Name, SHA512.HashData(data)),
			"SHA3-256" => new(HashAlgorithmName.SHA3_256.Name, SHA3_256.HashData(data)),
			"SHA3-384" => new(HashAlgorithmName.SHA3_384.Name, SHA3_384.HashData(data)),
			"SHA3-512" => new(HashAlgorithmName.SHA3_512.Name, SHA3_512.HashData(data)),
			_ => throw new InvalidOperationException($"The specified '{name}' is an invalid hash algorithm."),
		};
	}

	/// <summary>使用指定哈希算法计算指定数据的校验码。</summary>
	/// <param name="name">指定的哈希算法名称。</param>
	/// <param name="data">指定的待计算的数据。</param>
	/// <returns>返回的校验码。</returns>
	/// <exception cref="ArgumentNullException">指定的 <paramref name="data"/> 参数值为空(<c>null</c>)。</exception>
	/// <exception cref="InvalidOperationException">指定的 <paramref name="name"/> 参数值不是一个有效的哈希算法名。</exception>
	public static Checksum Compute(string name, Stream data)
	{
		ArgumentNullException.ThrowIfNull(data);

		if(string.IsNullOrEmpty(name))
			return new(HashAlgorithmName.SHA1.Name, SHA1.HashData(data));

		return name.ToUpperInvariant() switch
		{
			"MD5" => new(HashAlgorithmName.MD5.Name, MD5.HashData(data)),
			"SHA1" => new(HashAlgorithmName.SHA1.Name, SHA1.HashData(data)),
			"SHA256" => new(HashAlgorithmName.SHA256.Name, SHA256.HashData(data)),
			"SHA384" => new(HashAlgorithmName.SHA384.Name, SHA384.HashData(data)),
			"SHA512" => new(HashAlgorithmName.SHA512.Name, SHA512.HashData(data)),
			"SHA3-256" => new(HashAlgorithmName.SHA3_256.Name, SHA3_256.HashData(data)),
			"SHA3-384" => new(HashAlgorithmName.SHA3_384.Name, SHA3_384.HashData(data)),
			"SHA3-512" => new(HashAlgorithmName.SHA3_512.Name, SHA3_512.HashData(data)),
			_ => throw new InvalidOperationException($"The specified '{name}' is an invalid hash algorithm."),
		};
	}

	/// <summary>使用指定哈希算法计算指定数据的校验码。</summary>
	/// <param name="name">指定的哈希算法名称。</param>
	/// <param name="data">指定的待计算的数据。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的校验码。</returns>
	/// <exception cref="ArgumentNullException">指定的 <paramref name="data"/> 参数值为空(<c>null</c>)。</exception>
	/// <exception cref="InvalidOperationException">指定的 <paramref name="name"/> 参数值不是一个有效的哈希算法名。</exception>
	public static async ValueTask<Checksum> ComputeAsync(string name, Stream data, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(data);

		if(string.IsNullOrEmpty(name))
			return new(HashAlgorithmName.SHA1.Name, await SHA1.HashDataAsync(data, cancellation));

		return name.ToUpperInvariant() switch
		{
			"MD5" => new(HashAlgorithmName.MD5.Name, await MD5.HashDataAsync(data, cancellation)),
			"SHA1" => new(HashAlgorithmName.SHA1.Name, await SHA1.HashDataAsync(data, cancellation)),
			"SHA256" => new(HashAlgorithmName.SHA256.Name, await SHA256.HashDataAsync(data, cancellation)),
			"SHA384" => new(HashAlgorithmName.SHA384.Name, await SHA384.HashDataAsync(data, cancellation)),
			"SHA512" => new(HashAlgorithmName.SHA512.Name, await SHA512.HashDataAsync(data, cancellation)),
			"SHA3-256" => new(HashAlgorithmName.SHA3_256.Name, await SHA3_256.HashDataAsync(data, cancellation)),
			"SHA3-384" => new(HashAlgorithmName.SHA3_384.Name, await SHA3_384.HashDataAsync(data, cancellation)),
			"SHA3-512" => new(HashAlgorithmName.SHA3_512.Name, await SHA3_512.HashDataAsync(data, cancellation)),
			_ => throw new InvalidOperationException($"The specified '{name}' is an invalid hash algorithm."),
		};
	}

	public static Checksum Parse(string text, IFormatProvider provider = null) => string.IsNullOrEmpty(text) ? default :
		TryParse(text, out var result) ? result : throw new InvalidOperationException($"Invalid checksum text format.");

	public static bool TryParse(string text, out Checksum result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Checksum result)
	{
		if(string.IsNullOrEmpty(text))
		{
			result = default;
			return false;
		}

		var index = text.IndexOf(':');
		var data = index < 0 ? System.Convert.FromHexString(text) : System.Convert.FromHexString(text.AsSpan()[(index + 1)..]);

		switch(data.Length)
		{
			case MD5.HashSizeInBytes:
				result = new(HashAlgorithmName.MD5.Name, data);
				return index < 0 || string.Equals(text[..index], HashAlgorithmName.MD5.Name, StringComparison.OrdinalIgnoreCase);
			case SHA1.HashSizeInBytes:
				result = new(HashAlgorithmName.SHA1.Name, data);
				return index < 0 || string.Equals(text[..index], HashAlgorithmName.SHA1.Name, StringComparison.OrdinalIgnoreCase);
			case SHA256.HashSizeInBytes:
				result = index < 0 ? new(data) : new(text[..index], data);
				return index < 0 ||
				(
					string.Equals(text[..index], HashAlgorithmName.SHA256.Name, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text[..index], HashAlgorithmName.SHA3_256.Name, StringComparison.OrdinalIgnoreCase)
				);
			case SHA384.HashSizeInBytes:
				result = index < 0 ? new(data) : new(text[..index], data);
				return index < 0 ||
				(
					string.Equals(text[..index], HashAlgorithmName.SHA384.Name, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text[..index], HashAlgorithmName.SHA3_384.Name, StringComparison.OrdinalIgnoreCase)
				);
			case SHA512.HashSizeInBytes:
				result = index < 0 ? new(data) : new(text[..index], data);
				return index < 0 ||
				(
					string.Equals(text[..index], HashAlgorithmName.SHA512.Name, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text[..index], HashAlgorithmName.SHA3_512.Name, StringComparison.OrdinalIgnoreCase)
				);
			default:
				result = default;
				return false;
		}
	}

	private static string Determine(ReadOnlySpan<byte> value)
	{
		if(value.IsEmpty)
			return null;

		return value.Length switch
		{
			MD5.HashSizeInBytes => HashAlgorithmName.MD5.Name,
			SHA1.HashSizeInBytes => HashAlgorithmName.SHA1.Name,
			SHA256.HashSizeInBytes => HashAlgorithmName.SHA256.Name,
			SHA384.HashSizeInBytes => HashAlgorithmName.SHA384.Name,
			SHA512.HashSizeInBytes => HashAlgorithmName.SHA512.Name,
			_ => null,
		};
	}
	#endregion

	#region 重写方法
	public bool Equals(Checksum other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && MemoryExtensions.SequenceEqual(this.Value ?? [], other.Value ?? []);
	public override bool Equals(object obj) => obj is Checksum other && this.Equals(other);
	public override int GetHashCode() => _hashcode;
	public override string ToString() => $"{this.Name}:{System.Convert.ToHexString(this.Value)}";
	#endregion

	#region 符号重写
	public static bool operator ==(Checksum left, Checksum right) => left.Equals(right);
	public static bool operator !=(Checksum left, Checksum right) => !(left == right);
	#endregion
}

[TypeConverter(typeof(TypeConverter))]
[JsonConverter(typeof(JsonConverter))]
partial struct Checksum
{
	private sealed class TypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value == null)
				return default(Checksum);
			if(value is string text)
				return Checksum.Parse(text);

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is Checksum checksum)
				return checksum.IsEmpty ? null : checksum.ToString();

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private sealed class JsonConverter : JsonConverter<Checksum>
	{
		public override Checksum Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => Checksum.Parse(reader.GetString()),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, Checksum value, JsonSerializerOptions options)
		{
			if(value.IsEmpty)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.ToString());
		}
	}
}