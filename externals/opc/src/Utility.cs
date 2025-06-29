﻿/*
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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Opc.Ua;

using Zongsoft.Services;

namespace Zongsoft.Externals.Opc;

internal static class Utility
{
	public static MessageSecurityMode GetSecurityMode(this OpcSecurityMode mode) => mode switch
	{
		OpcSecurityMode.None => MessageSecurityMode.None,
		OpcSecurityMode.Sign => MessageSecurityMode.Sign,
		OpcSecurityMode.SignAndEncrypt => MessageSecurityMode.SignAndEncrypt,
		_ => MessageSecurityMode.Invalid,
	};

	public static NodeId GetDataType(this Type type, out int rank)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		if(type.IsArray)
		{
			rank = type.GetArrayRank();
			return GetDataType(type.GetElementType());
		}

		rank = ValueRanks.Scalar;
		return GetDataType(type);

		static NodeId GetDataType(Type type)
		{
			if(Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType))
				type = underlyingType;

			if(type.IsEnum)
				return DataTypeIds.Enumeration;

			if(type == typeof(Guid))
				return DataTypeIds.Guid;

			return Type.GetTypeCode(type) switch
			{
				TypeCode.Boolean => DataTypeIds.Boolean,
				TypeCode.Byte => DataTypeIds.Byte,
				TypeCode.SByte => DataTypeIds.SByte,
				TypeCode.Int16 => DataTypeIds.Int16,
				TypeCode.Int32 => DataTypeIds.Int32,
				TypeCode.Int64 => DataTypeIds.Int64,
				TypeCode.UInt16 => DataTypeIds.UInt16,
				TypeCode.UInt32 => DataTypeIds.UInt32,
				TypeCode.UInt64 => DataTypeIds.UInt64,
				TypeCode.Single => DataTypeIds.Float,
				TypeCode.Double => DataTypeIds.Double,
				TypeCode.Decimal => DataTypeIds.Decimal,
				TypeCode.DateTime => DataTypeIds.DateTime,
				TypeCode.String => DataTypeIds.String,
				TypeCode.Object => DataTypeIds.ObjectTypeNode,
				_ => DataTypeIds.ObjectTypeNode,
			};
		}
	}

	public static Type GetDataType(this DataValue data)
	{
		if(data == null || data.Value == null)
			return null;

		if(data.WrappedValue.TypeInfo.BuiltInType == BuiltInType.NodeId)
		{
			var node = data.GetValue<NodeId>(null);

			if(node != null)
			{
				var type = Common.Convert.ConvertValue(node.Identifier, BuiltInType.Null);
				return GetDataType(type, data.WrappedValue.TypeInfo.ValueRank);
			}
		}

		return null;
	}

	public static Type GetDataType(this BuiltInType type, int rank)
	{
		if(type == BuiltInType.Null)
			return null;

		if(rank == ValueRanks.Scalar)
			return type switch
			{
				BuiltInType.Boolean => typeof(bool),
				BuiltInType.Byte => typeof(byte),
				BuiltInType.SByte => typeof(sbyte),
				BuiltInType.Int16 => typeof(short),
				BuiltInType.Int32 => typeof(int),
				BuiltInType.Int64 => typeof(long),
				BuiltInType.UInt16 => typeof(ushort),
				BuiltInType.UInt32 => typeof(uint),
				BuiltInType.UInt64 => typeof(ulong),
				BuiltInType.Float => typeof(float),
				BuiltInType.Double => typeof(double),
				BuiltInType.Integer => typeof(int),
				BuiltInType.UInteger => typeof(uint),
				BuiltInType.Number => typeof(double),
				BuiltInType.String => typeof(string),
				BuiltInType.ByteString => typeof(byte[]),
				BuiltInType.LocalizedText => typeof(string),
				BuiltInType.DateTime => typeof(DateTime),
				BuiltInType.Guid => typeof(Guid),
				BuiltInType.Enumeration => typeof(Enum),
				BuiltInType.ExtensionObject => typeof(object),
				BuiltInType.StatusCode => typeof(string),
				BuiltInType.XmlElement => typeof(System.Xml.XmlElement),
				_ => typeof(object),
			};

		if(rank == ValueRanks.OneDimension)
			return type switch
			{
				BuiltInType.Boolean => typeof(bool[]),
				BuiltInType.Byte => typeof(byte[]),
				BuiltInType.SByte => typeof(sbyte[]),
				BuiltInType.Int16 => typeof(short[]),
				BuiltInType.Int32 => typeof(int[]),
				BuiltInType.Int64 => typeof(long[]),
				BuiltInType.UInt16 => typeof(ushort[]),
				BuiltInType.UInt32 => typeof(uint[]),
				BuiltInType.UInt64 => typeof(ulong[]),
				BuiltInType.Float => typeof(float[]),
				BuiltInType.Double => typeof(double[]),
				BuiltInType.Integer => typeof(int[]),
				BuiltInType.UInteger => typeof(uint[]),
				BuiltInType.Number => typeof(double[]),
				BuiltInType.String => typeof(string[]),
				BuiltInType.ByteString => typeof(byte[][]),
				BuiltInType.LocalizedText => typeof(string[]),
				BuiltInType.DateTime => typeof(DateTime[]),
				BuiltInType.Guid => typeof(Guid[]),
				BuiltInType.Enumeration => typeof(Enum[]),
				BuiltInType.ExtensionObject => typeof(object[]),
				BuiltInType.StatusCode => typeof(string[]),
				BuiltInType.XmlElement => typeof(System.Xml.XmlElement[]),
				_ => typeof(Array),
			};

		if(rank >= ValueRanks.TwoDimensions)
			return type switch
			{
				BuiltInType.Boolean => typeof(bool).MakeArrayType(rank),
				BuiltInType.Byte => typeof(byte).MakeArrayType(rank),
				BuiltInType.SByte => typeof(sbyte).MakeArrayType(rank),
				BuiltInType.Int16 => typeof(short).MakeArrayType(rank),
				BuiltInType.Int32 => typeof(int).MakeArrayType(rank),
				BuiltInType.Int64 => typeof(long).MakeArrayType(rank),
				BuiltInType.UInt16 => typeof(ushort).MakeArrayType(rank),
				BuiltInType.UInt32 => typeof(uint).MakeArrayType(rank),
				BuiltInType.UInt64 => typeof(ulong).MakeArrayType(rank),
				BuiltInType.Float => typeof(float).MakeArrayType(rank),
				BuiltInType.Double => typeof(double).MakeArrayType(rank),
				BuiltInType.Integer => typeof(int).MakeArrayType(rank),
				BuiltInType.UInteger => typeof(uint).MakeArrayType(rank),
				BuiltInType.Number => typeof(double).MakeArrayType(rank),
				BuiltInType.String => typeof(string).MakeArrayType(rank),
				BuiltInType.ByteString => typeof(byte[]).MakeArrayType(rank),
				BuiltInType.LocalizedText => typeof(string).MakeArrayType(rank),
				BuiltInType.DateTime => typeof(DateTime).MakeArrayType(rank),
				BuiltInType.Guid => typeof(Guid).MakeArrayType(rank),
				BuiltInType.Enumeration => typeof(Enum).MakeArrayType(rank),
				BuiltInType.ExtensionObject => typeof(object).MakeArrayType(rank),
				BuiltInType.StatusCode => typeof(string).MakeArrayType(rank),
				BuiltInType.XmlElement => typeof(System.Xml.XmlElement).MakeArrayType(rank),
				_ => typeof(object).MakeArrayType(rank),
			};

		return null;
	}

	public static Type GetDataType(this NodeId id, int rank)
	{
		if(id == null || id.IsNullNodeId)
			return null;

		if(id == DataTypeIds.Byte)
			return GetType(typeof(byte), rank);
		if(id == DataTypeIds.SByte)
			return GetType(typeof(sbyte), rank);
		if(id == DataTypeIds.Boolean)
			return GetType(typeof(bool), rank);
		if(id == DataTypeIds.Int16)
			return GetType(typeof(Int16), rank);
		if(id == DataTypeIds.Int32 || id == DataTypeIds.Integer)
			return GetType(typeof(Int32), rank);
		if(id == DataTypeIds.Int64)
			return GetType(typeof(Int64), rank);
		if(id == DataTypeIds.UInt16)
			return GetType(typeof(UInt16), rank);
		if(id == DataTypeIds.UInt32 || id == DataTypeIds.UInteger)
			return GetType(typeof(UInt32), rank);
		if(id == DataTypeIds.UInt64)
			return GetType(typeof(UInt64), rank);
		if(id == DataTypeIds.Float)
			return GetType(typeof(float), rank);
		if(id == DataTypeIds.Double)
			return GetType(typeof(double), rank);
		if(id == DataTypeIds.Decimal)
			return GetType(typeof(decimal), rank);
		if(id == DataTypeIds.Number)
			return GetType(typeof(double), rank);
		if(id == DataTypeIds.String)
			return GetType(typeof(string), rank);
		if(id == DataTypeIds.ByteString)
			return GetType(typeof(byte[]), rank);
		if(id == DataTypeIds.LocalizedText)
			return GetType(typeof(string), rank);
		if(id == DataTypeIds.DateTime)
			return GetType(typeof(DateTime), rank);
		if(id == DataTypeIds.UtcTime)
			return GetType(typeof(DateTimeOffset), rank);
		if(id == DataTypeIds.Duration)
			return GetType(typeof(TimeSpan), rank);
		if(id == DataTypeIds.DurationString)
			return GetType(typeof(string), rank);
		if(id == DataTypeIds.Guid)
			return GetType(typeof(Guid), rank);
		if(id == DataTypeIds.Enumeration)
			return GetType(typeof(Enum), rank);
		if(id == DataTypeIds.StatusCode)
			return GetType(typeof(string), rank);
		if(id == DataTypeIds.XmlElement)
			return GetType(typeof(System.Xml.XmlElement), rank);

		if(rank == ValueRanks.Scalar)
			return typeof(object);
		if(rank == ValueRanks.OneDimension)
			return typeof(object[]);
		if(rank >= ValueRanks.TwoDimensions)
			typeof(object).MakeArrayType(rank);

		return null;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static Type GetType(Type type, int rank)
	{
		if(type == null)
			return type;

		if(rank == ValueRanks.Scalar)
			return type;
		if(rank == ValueRanks.OneDimension)
			return type.MakeArrayType(1);
		if(rank >= ValueRanks.TwoDimensions)
			return type.MakeArrayType(rank);

		return type;
	}

	public static IUserIdentity GetIdentity(this Configuration.OpcConnectionSettings settings)
	{
		if(settings == null)
			return null;

		if(!string.IsNullOrEmpty(settings.UserName))
			return new UserIdentity(settings.UserName, settings.Password ?? string.Empty);

		if(!string.IsNullOrEmpty(settings.Certificate))
			return new UserIdentity(GetCertificate(settings.Certificate, settings.CertificateSecret));

		return null;
	}

	private static X509Certificate2 GetCertificate(string text, string secret = null)
	{
		if(string.IsNullOrEmpty(text))
			return null;

		//适用竖线来表达证书存储器名称和位置
		var index = text.IndexOf('|');

		if(index > 0 && index < text.Length)
		{
			using var store = GetX509Store(text.AsSpan()[..index]);
			store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
			var result = store.Certificates.Find(X509FindType.FindBySerialNumber, text[(index + 1)..], false);

			if(result == null || result.Count == 0)
				throw new InvalidOperationException($"Unable to find “{text[(index + 1)..]}” certificate in “{store.Name}@{store.Location}” store.");

			return result[0];
		}

		//如果证书文本包含冒号则表示采用虚拟文件路径
		if(text.Contains(':'))
		{
			using var stream = Zongsoft.IO.FileSystem.File.Open(text, FileMode.Open, FileAccess.Read);
			return LoadX509(stream, secret);
		}

		//解析相对文件路径
		if(!Path.IsPathRooted(text))
		{
			text = Path.Combine(typeof(Utility).Assembly.Location, "certificates", text);
			if(!File.Exists(text))
				text = Path.Combine(ApplicationContext.Current.ApplicationPath, "certificates", text);
		}

		using var file = File.OpenRead(text);
		return LoadX509(file, secret);

		static X509Store GetX509Store(ReadOnlySpan<char> identifier)
		{
			if(identifier.IsEmpty)
				return new X509Store();

			var index = identifier.IndexOf('@');

			if(index < 0)
				return new(identifier.ToString(), StoreLocation.CurrentUser);
			else
				return new(identifier[..index].ToString(), StoreLocation.CurrentUser);
		}

		static X509Certificate2 LoadX509(Stream stream, string secret)
		{
			var data = new byte[stream.Length];

			#if NET9_0_OR_GREATER
			stream.ReadExactly(data, 0, data.Length);

			//获取证书类型
			var type = X509Certificate2.GetCertContentType(data);

			//返回加载的证书
			return type == X509ContentType.Pfx || type == X509ContentType.Pkcs12 || type == X509ContentType.Pkcs7 ?
				X509CertificateLoader.LoadPkcs12(data, secret, X509KeyStorageFlags.Exportable) :
				X509CertificateLoader.LoadCertificate([.. data]);
			#else
			stream.Read(data, 0, data.Length);
			return string.IsNullOrEmpty(secret) ?
				new X509Certificate2(data) :
				new X509Certificate2(data, secret);
			#endif
		}
	}
}
