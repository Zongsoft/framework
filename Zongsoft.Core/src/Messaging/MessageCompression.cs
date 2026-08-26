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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Globalization;

namespace Zongsoft.Messaging;

/// <summary>表示消息负载的压缩设置。</summary>
public readonly struct MessageCompression : IEquatable<MessageCompression>, IParsable<MessageCompression>
{
	#region 成员字段
	private readonly string _name;
	private readonly int _value;
	#endregion

	#region 构造函数
	/// <summary>初始化消息压缩设置。</summary>
	/// <param name="name">指定的压缩算法名称。</param>
	/// <param name="value">指定启用压缩的载荷字节数阈值。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或空白。</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> 小于零。</exception>
	public MessageCompression(string name, int value)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(value < 0)
			throw new ArgumentOutOfRangeException(nameof(value));

		_name = name.Trim();
		_value = value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取压缩算法名称。</summary>
	/// <value>压缩算法名称；默认值为空。</value>
	public string Name => _name;

	/// <summary>获取启用压缩的载荷字节数阈值。</summary>
	/// <value>以字节为单位的非负整数阈值；零表示压缩所有非空载荷。</value>
	public int Value => _value;

	/// <summary>获取一个值，指示当前压缩设置是否为空。</summary>
	/// <value>当前设置未指定压缩算法则为真，否则为假。</value>
	public bool IsEmpty => string.IsNullOrEmpty(_name);
	#endregion

	#region 公共方法
	/// <summary>确定指定长度的消息负载是否应当压缩。</summary>
	/// <param name="length">指定的消息负载字节数。</param>
	/// <returns>如果当前设置有效、负载非空且达到压缩阈值则为真，否则为假。</returns>
	public bool CanCompress(int length) => length > 0 && !this.IsEmpty && length >= _value;

	/// <summary>使用当前设置压缩指定的消息负载。</summary>
	/// <param name="data">指定要压缩的消息负载。</param>
	/// <returns>返回压缩后的消息负载。</returns>
	/// <exception cref="Common.OperationException">指定的压缩算法不受支持。</exception>
	public byte[] Compress(ReadOnlySpan<byte> data) => IO.Compression.Compressor.Compress(_name, data.ToArray());

	/// <summary>使用指定算法解压消息负载。</summary>
	/// <param name="name">指定的压缩算法名称。</param>
	/// <param name="data">指定要解压的消息负载。</param>
	/// <returns>返回解压后的消息负载。</returns>
	/// <exception cref="Common.OperationException">指定的压缩算法不受支持。</exception>
	public static byte[] Decompress(string name, ReadOnlySpan<byte> data) => IO.Compression.Compressor.Decompress(name, data.ToArray());

	#endregion

	#region 解析方法
	/// <summary>解析指定的压缩设置文本。</summary>
	/// <param name="text">指定格式为“算法名称:字节数阈值”的文本。</param>
	/// <returns>返回解析成功的压缩设置；空文本返回默认值。</returns>
	/// <exception cref="FormatException"><paramref name="text"/> 不是有效格式。</exception>
	public static MessageCompression Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException();

	/// <summary>尝试解析指定的压缩设置文本。</summary>
	/// <param name="text">指定格式为“算法名称:字节数阈值”的文本。</param>
	/// <param name="result">输出解析成功的压缩设置。</param>
	/// <returns>如果解析成功则为真，否则为假。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, out MessageCompression result)
	{
		text = text.Trim();
		if(text.IsEmpty || text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			result = default;
			return true;
		}

		var separator = text.IndexOf(':');
		if(separator <= 0 || separator == text.Length - 1 || text[(separator + 1)..].IndexOf(':') >= 0 ||
		   !int.TryParse(text[(separator + 1)..].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var threshold) || threshold < 0)
		{
			result = default;
			return false;
		}

		var name = text[..separator].Trim();
		if(name.IsEmpty)
		{
			result = default;
			return false;
		}

		result = new MessageCompression(name.ToString(), threshold);
		return true;
	}

	static MessageCompression IParsable<MessageCompression>.Parse(string text, IFormatProvider provider) => Parse(text);
	static bool IParsable<MessageCompression>.TryParse(string text, IFormatProvider provider, out MessageCompression result) => TryParse(text, out result);
	#endregion

	#region 重写方法
	public bool Equals(MessageCompression other) => string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase) && _value == other._value;
	public override bool Equals(object obj) => obj is MessageCompression other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(_name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(_name), _value);
	public override string ToString() => this.IsEmpty ? string.Empty : $"{_name}:{_value.ToString(CultureInfo.InvariantCulture)}";
	#endregion

	#region 符号重写
	public static bool operator ==(MessageCompression left, MessageCompression right) => left.Equals(right);
	public static bool operator !=(MessageCompression left, MessageCompression right) => !left.Equals(right);
	#endregion
}
