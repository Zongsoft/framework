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
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Net;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Common;

public static class Convert
{
	#region 初始化器
	static Convert()
	{
		TypeDescriptor.AddAttributes(typeof(Enum), [new TypeConverterAttribute(typeof(Components.Converters.EnumConverter))]);
		TypeDescriptor.AddAttributes(typeof(Guid), [new TypeConverterAttribute(typeof(Components.Converters.GuidConverter))]);
		TypeDescriptor.AddAttributes(typeof(bool), [new TypeConverterAttribute(typeof(Components.Converters.BooleanConverter))]);
		TypeDescriptor.AddAttributes(typeof(TimeSpan), [new TypeConverterAttribute(typeof(Components.Converters.TimeSpanConverter))]);
		TypeDescriptor.AddAttributes(typeof(Encoding), [new TypeConverterAttribute(typeof(Components.Converters.EncodingConverter))]);
		TypeDescriptor.AddAttributes(typeof(EndPoint), [new TypeConverterAttribute(typeof(Components.Converters.EndpointConverter))]);
	}
	#endregion

	#region 扩展方法
	/// <summary>获取指定成员的类型转换器。</summary>
	/// <param name="member">指定要获取的成员。</param>
	/// <param name="explicitly">如果为真(<c>True</c>)则只返回显式声明的类型转换器，否则为假(<c>False</c>)。</param>
	/// <returns>返回指定成员声明的类型转换器，如果为空(<c>null</c>)则表示获取失败。</returns>
	public static TypeConverter GetTypeConverter(this MemberInfo member, bool explicitly = false)
	{
		if(member == null)
			return null;

		var attribute = member.GetCustomAttribute<TypeConverterAttribute>(true);

		if(attribute != null && !string.IsNullOrEmpty(attribute.ConverterTypeName))
		{
			var type = Type.GetType(attribute.ConverterTypeName, assemblyName =>
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();

				for(int i = 0; i < assemblies.Length; i++)
				{
					var name = assemblies[i].GetName();

					if(string.Equals(assemblyName.FullName, name.FullName) && assemblyName.Version <= name.Version)
						return assemblies[i];
				}

				return null;
			}, null);

			return Activator.CreateInstance(type) as TypeConverter;
		}

		var memberType = member switch
		{
			PropertyInfo property => property.PropertyType,
			FieldInfo field => field.FieldType,
			MethodInfo method => method.ReturnType,
			_ => null,
		};

		if(memberType == null)
			return null;

		if(TypeExtension.IsNullable(memberType, out var underlyingType))
			memberType = underlyingType;
		else if(TypeExtension.IsCollection(memberType)) //优先使用自定义的集合类型转换器
			return Components.Converters.CollectionConverter.Default;

		//再从成员的类型上去查找类型转换器
		var result = GetTypeConverter(memberType, explicitly);
		if(result != null)
			return result;

		return explicitly ? null : TypeDescriptor.GetConverter(memberType);
	}
	#endregion

	#region 类型转换
	public static bool IsZero(object value)
	{
		if(value is null)
			return false;

		return Type.GetTypeCode(value.GetType()) switch
		{
			TypeCode.Char => ((char)value) == '\0',
			TypeCode.Byte => ((byte)value) == 0,
			TypeCode.SByte => ((sbyte)value) == 0,
			TypeCode.Int16 => ((short)value) == 0,
			TypeCode.Int32 => ((int)value) == 0,
			TypeCode.Int64 => ((long)value) == 0L,
			TypeCode.UInt16 => ((ushort)value) == 0,
			TypeCode.UInt32 => ((uint)value) == 0U,
			TypeCode.UInt64 => ((ulong)value) == 0UL,
			TypeCode.Single => ((float)value) == 0f,
			TypeCode.Double => ((double)value) == 0d,
			TypeCode.Decimal => ((decimal)value) == 0m,
			TypeCode.Boolean => ((bool)value) == false,
			_ => false,
		};
	}

	public static double ToDouble(float number) => (double)(decimal)number;

	public static T ConvertValue<T>(object value) => (T)ConvertValue(value, typeof(T));
	public static T ConvertValue<T>(object value, T defaultValue) => (T)ConvertValue(value, typeof(T), defaultValue);

	public static T ConvertValue<T>(object value, Func<T> defaultValueThunk)
	{
		if(TryConvertValue(value, typeof(T), out var result))
			return (T)result;

		if(defaultValueThunk != null)
			return defaultValueThunk();

		throw new InvalidOperationException($"Unable to convert {value} to {typeof(T)} type.");
	}

	public static T ConvertValue<T>(object value, Func<TypeConverter> converterFactory) => (T)ConvertValue(value, typeof(T), converterFactory);
	public static T ConvertValue<T>(object value, Func<TypeConverter> converterFactory, T defaultValue) => (T)ConvertValue(value, typeof(T), converterFactory, defaultValue);

	public static T ConvertValue<T>(object value, Func<TypeConverter> converterFactory, Func<T> defaultValueThunk)
	{
		if(TryConvertValue(value, typeof(T), converterFactory, out var result))
			return (T)result;

		if(defaultValueThunk != null)
			return defaultValueThunk();

		throw new InvalidOperationException($"Unable to convert {value} to {typeof(T)} type.");
	}

	public static object ConvertValue(object value, Type conversionType)
	{
		if(TryConvertValue(value, conversionType, out var result))
			return result;

		throw new InvalidOperationException($"Unable to convert {value} to {conversionType} type.");
	}

	public static object ConvertValue(object value, Type conversionType, object defaultValue) => TryConvertValue(value, conversionType, out var result) ? result : defaultValue;

	public static object ConvertValue(object value, Type conversionType, Func<object> defaultValueThunk)
	{
		if(TryConvertValue(value, conversionType, out var result))
			return result;

		if(defaultValueThunk != null)
			return defaultValueThunk();

		throw new InvalidOperationException($"Unable to convert {value} to {conversionType} type.");
	}

	public static object ConvertValue(object value, Type conversionType, Func<TypeConverter> converterFactory)
	{
		if(TryConvertValue(value, conversionType, converterFactory, out var result))
			return result;

		throw new InvalidOperationException($"Unable to convert {value} to {conversionType} type.");
	}

	public static object ConvertValue(object value, Type conversionType, Func<TypeConverter> converterFactory, object defaultValue) =>
		TryConvertValue(value, conversionType, converterFactory, out var result) ? result : defaultValue;

	public static object ConvertValue(object value, Type conversionType, Func<TypeConverter> converterFactory, Func<object> defaultValueThunk)
	{
		if(TryConvertValue(value, conversionType, converterFactory, out var result))
			return result;

		if(defaultValueThunk != null)
			return defaultValueThunk();

		throw new InvalidOperationException($"Unable to convert {value} to {conversionType} type.");
	}

	public static bool TryConvertValue<T>(object value, out T result) => TryConvertValue<T>(value, null, out result);
	public static bool TryConvertValue(object value, Type conversionType, out object result) => TryConvertValue(value, conversionType, null, out result);

	public static bool TryConvertValue<T>(object value, Func<TypeConverter> converterFactory, out T result)
	{
		if(TryConvertValue(value, typeof(T), converterFactory, out var obj))
		{
			result = (T)obj;
			return true;
		}

		result = default;
		return false;
	}

	public static bool TryConvertValue(object value, Type conversionType, Func<TypeConverter> converterFactory, out object result)
	{
		//如果转换类型为空或对象类型则无需转换
		if(conversionType == null || conversionType == typeof(object))
		{
			result = value;
			return true;
		}

		//处理待转换值为空的情况
		if(value == null || System.Convert.IsDBNull(value))
		{
			result = conversionType.GetDefaultValue();
			return true;
		}

		//处理可空类型的情况（即处理可空类型的基元类型）
		var type = conversionType.IsNullable(out var underlyingType) ? underlyingType : conversionType;

		//处理类型兼容的情况
		if(type == value.GetType() || type.IsAssignableFrom(value.GetType()))
		{
			result = value;
			return true;
		}

		try
		{
			//使用类型转换器进行转换
			if(TryConvertWithConverter(value, type, converterFactory, out result))
				return true;

			if(value is string)
			{
				var method = type.IsValueType ?
					type.GetMethod("TryParse", [typeof(string), type.MakeByRefType()]) :
					type.GetMethod("TryParse", [typeof(string), type]);

				if(method != null && method.IsStatic)
				{
					var args = new object[] { value, null };
					var invoked = method.Invoke(null, args);

					if(invoked is bool succeed)
					{
						result = succeed ? args[1] : null;
						return succeed;
					}
				}
				else
				{
					method = type.GetMethod("Parse", [typeof(string)]);

					if(method != null && method.IsStatic)
					{
						result = method.Invoke(null, [value]);
						return true;
					}
				}
			}

			result = System.Convert.ChangeType(value, type);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}

	private static bool TryConvertWithConverter(object value, Type conversionType, Func<TypeConverter> converterFactory, out object result)
	{
		TypeConverter converter = converterFactory?.Invoke();

		if(converter == null)
		{
			if(conversionType == typeof(string))
			{
				converter = TypeDescriptor.GetConverter(value.GetType());
				if(converter != null && converter.GetType() != typeof(TypeConverter) && converter.CanConvertTo(conversionType))
				{
					result = converter.ConvertTo(value, conversionType);
					return true;
				}
			}
			else
			{
				converter = TypeDescriptor.GetConverter(conversionType);
				if(converter != null && converter.GetType() != typeof(TypeConverter) && converter.CanConvertFrom(value.GetType()))
				{
					result = converter.ConvertFrom(value);
					return true;
				}

				converter = TypeDescriptor.GetConverter(value.GetType());
				if(converter != null && converter.GetType() != typeof(TypeConverter) && converter.CanConvertTo(conversionType))
				{
					result = converter.ConvertTo(value, conversionType);
					return true;
				}
			}
		}
		else if(converter.GetType() != typeof(TypeConverter))
		{
			if(converter.CanConvertFrom(value.GetType()))
			{
				result = converter.ConvertFrom(value);
				return true;
			}

			if(converter.CanConvertTo(conversionType))
			{
				result = converter.ConvertTo(value, conversionType);
				return true;
			}
		}

		result = null;
		return false;
	}
	#endregion

	#region 字节文本
	/// <summary>将指定的字节数组转换为其用十六进制数字编码的等效字符串表示形式。</summary>
	/// <param name="bytes">一个 8 位无符号字节数组。</param>
	/// <param name="lowerCase">返回的十六进制字符串中是否使用小写字符，默认为大写。</param>
	/// <returns>参数中元素的字符串表示形式，以十六进制文本表示。</returns>
	public static string ToHexString(byte[] bytes, bool lowerCase = false) => ToHexString(bytes, 0, 0, '\0', lowerCase);

	/// <summary>将指定的字节数组转换为其用十六进制数字编码的等效字符串表示形式。</summary>
	/// <param name="bytes">一个 8 位无符号字节数组。</param>
	/// <param name="offset">指定字节数组的起始下标。</param>
	/// <param name="count">指定字节数组的元素个数。</param>
	/// <param name="lowerCase">返回的十六进制字符串中是否使用小写字符，默认为大写。</param>
	/// <returns>参数中元素的字符串表示形式，以十六进制文本表示。</returns>
	public static string ToHexString(byte[] bytes, int offset, int count, bool lowerCase = false) => ToHexString(bytes, offset, count, '\0', lowerCase);

	/// <summary>将指定的字节数组转换为其用十六进制数字编码的等效字符串表示形式。</summary>
	/// <param name="bytes">一个 8 位无符号字节数组。</param>
	/// <param name="separator">每字节对应的十六进制文本中间的分隔符。</param>
	/// <param name="lowerCase">返回的十六进制字符串中是否使用小写字符，默认为大写。</param>
	/// <returns>参数中元素的字符串表示形式，以十六进制文本表示。</returns>
	public static string ToHexString(byte[] bytes, char separator, bool lowerCase = false) => ToHexString(bytes, 0, 0, separator, lowerCase);

	/// <summary>将指定的字节数组转换为其用十六进制数字编码的等效字符串表示形式。</summary>
	/// <param name="bytes">一个 8 位无符号字节数组。</param>
	/// <param name="offset">指定字节数组的起始下标。</param>
	/// <param name="count">指定字节数组的元素个数。</param>
	/// <param name="separator">每字节对应的十六进制文本中间的分隔符。</param>
	/// <param name="lowerCase">返回的十六进制字符串中是否使用小写字符，默认为大写。</param>
	/// <returns>参数中元素的字符串表示形式，以十六进制文本表示。</returns>
	public static string ToHexString(byte[] bytes, int offset, int count, char separator, bool lowerCase = false)
	{
		if(bytes == null || bytes.Length == 0)
			return string.Empty;

		if(offset < 0 || offset >= bytes.Length)
			throw new ArgumentOutOfRangeException(nameof(offset));

		if(count < 1)
			count = (bytes.Length - offset);
		else
			count = Math.Min(bytes.Length - offset, count);

		var alpha = lowerCase ? 'a' : 'A';
		var rank = separator == '\0' ? 2 : 3;
		var characters = new char[count * rank - (rank - 2)];

		for(int i = 0; i < count; i++)
		{
			characters[i * rank] = GetDigit((byte)(bytes[offset + i] / 16), alpha);
			characters[i * rank + 1] = GetDigit((byte)(bytes[offset + i] % 16), alpha);

			if(rank == 3 && i < count - 1)
				characters[i * rank + 2] = separator;
		}

		return new string(characters);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static char GetDigit(byte value, char alpha)
	{
		if(value < 10)
			return (char)('0' + value);

		return (char)(alpha + (value - 10));
	}

	/// <summary>将指定的十六进制格式的字符串转换为等效的字节数组。</summary>
	/// <param name="text">要转换的十六进制格式的字符串。</param>
	/// <returns>与<paramref name="text"/>等效的字节数组。</returns>
	/// <exception cref="System.FormatException"><paramref name="text"/>参数中含有非空白字符。</exception>
	/// <remarks>该方法的实现始终忽略<paramref name="text"/>参数中的空白字符。</remarks>
	public static byte[] FromHexString(string text) => FromHexString(text, '\0', true);

	/// <summary>将指定的十六进制格式的字符串转换为等效的字节数组。</summary>
	/// <param name="text">要转换的十六进制格式的字符串。</param>
	/// <param name="separator">要过滤掉的分隔符字符。</param>
	/// <returns>与<paramref name="text"/>等效的字节数组。</returns>
	/// <exception cref="System.FormatException"><paramref name="text"/>参数中含有非空白字符或非指定的分隔符。</exception>
	/// <remarks>该方法的实现始终忽略<paramref name="text"/>参数中的空白字符。</remarks>
	public static byte[] FromHexString(string text, char separator) => FromHexString(text, separator, true);

	/// <summary>将指定的十六进制格式的字符串转换为等效的字节数组。</summary>
	/// <param name="text">要转换的十六进制格式的字符串。</param>
	/// <param name="separator">要过滤掉的分隔符字符。</param>
	/// <param name="throwExceptionOnFormat">指定当输入文本中含有非法字符时是否抛出<seealso cref="System.FormatException"/>异常。</param>
	/// <returns>与<paramref name="text"/>等效的字节数组。</returns>
	/// <exception cref="System.FormatException">当<paramref name="throwExceptionOnFormat"/>参数为真，并且<paramref name="text"/>参数中含有非空白字符或非指定的分隔符。</exception>
	/// <remarks>该方法的实现始终忽略<paramref name="text"/>参数中的空白字符。</remarks>
	public static byte[] FromHexString(string text, char separator, bool throwExceptionOnFormat)
	{
		if(string.IsNullOrEmpty(text))
			return [];

		var index = 0;
		var buffer = new char[2];
		var result = new List<byte>();

		foreach(char character in text)
		{
			if(char.IsWhiteSpace(character) || character == separator)
				continue;

			buffer[index++] = character;
			if(index == buffer.Length)
			{
				index = 0;
				byte value = 0;

				if(TryParseHex(buffer, out value))
					result.Add(value);
				else
				{
					if(throwExceptionOnFormat)
						throw new FormatException();
					else
						return [];
				}
			}
		}

		return result.ToArray();
	}

	public static bool TryParseHex(char[] characters, out byte value)
	{
		if(TryParseHex(characters, out long number))
		{
			if(number >= byte.MinValue && number <= byte.MaxValue)
			{
				value = (byte)number;
				return true;
			}
		}

		value = 0;
		return false;
	}

	public static bool TryParseHex(char[] characters, out short value)
	{
		if(TryParseHex(characters, out long number))
		{
			if(number >= short.MinValue && number <= short.MaxValue)
			{
				value = (short)number;
				return true;
			}
		}

		value = 0;
		return false;
	}

	public static bool TryParseHex(char[] characters, out int value)
	{
		if(TryParseHex(characters, out long number))
		{
			if(number >= int.MinValue && number <= int.MaxValue)
			{
				value = (int)number;
				return true;
			}
		}

		value = 0;
		return false;
	}

	public static bool TryParseHex(char[] characters, out long value)
	{
		value = 0;

		if(characters == null)
			return false;

		int count = 0;
		byte[] digits = new byte[characters.Length];

		foreach(char character in characters)
		{
			if(char.IsWhiteSpace(character))
				continue;

			if(character >= '0' && character <= '9')
				digits[count++] = (byte)(character - '0');
			else if(character >= 'A' && character <= 'F')
				digits[count++] = (byte)((character - 'A') + 10);
			else if(character >= 'a' && character <= 'f')
				digits[count++] = (byte)((character - 'a') + 10);
			else
				return false;
		}

		long number = 0;

		if(count > 0)
		{
			for(int i = 0; i < count; i++)
			{
				number += digits[i] * (long)Math.Pow(16, count - i - 1);
			}
		}

		value = number;
		return true;
	}
	#endregion
}
