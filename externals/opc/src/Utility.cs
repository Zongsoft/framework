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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Opc.Ua;

using Zongsoft.Services;

namespace Zongsoft.Externals.Opc;

internal static class Utility
{
	#region 工具方法
	public static object GetValue(DataValue value)
	{
		var result = value.WrappedValue.AsBoxedObject(Variant.BoxingBehavior.Legacy);
		if(result is not ExtensionObject extension)
			return result;

		if(extension.TryGetValue(out IEncodeable encoded))
			return encoded;
		if(extension.TryGetAsBinary(out var binary))
			return binary;
		if(extension.TryGetAsXml(out var xml))
			return xml;
		if(extension.TryGetAsJson(out var json))
			return json;

		return null;
	}

	public static MessageSecurityMode GetSecurityMode(this OpcSecurityMode mode) => mode switch
	{
		OpcSecurityMode.None => MessageSecurityMode.None,
		OpcSecurityMode.Sign => MessageSecurityMode.Sign,
		OpcSecurityMode.SignAndEncrypt => MessageSecurityMode.SignAndEncrypt,
		_ => MessageSecurityMode.Invalid,
	};

	/// <summary>获取指定类型对应的OPC内置类型的节点编号。</summary>
	/// <param name="type">指定要获取的类型。</param>
	/// <param name="rank">输出参数，表示数组类型的维度。</param>
	/// <returns>如果获取成功则返回对应内置类型的节点编号，否则返回空(<c>null</c>)。</returns>
	/// <exception cref="ArgumentNullException">如果指定的 <paramref name="type"/> 参数为空(<c>null</c>)。</exception>
	public static NodeId GetBuiltinType(this Type type, out int rank)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		return GetBuiltinTypeCore(DiscriminateType(type, out rank));
	}

	internal static NodeId GetBuiltinTypeCore(Type type)
	{
		//注意：枚举类型必须返回空！
		//因为 Type.GetTypeCode(..) 会返回枚举类型的基元类型
		if(type == null || type.IsEnum)
			return default;

		if(type == typeof(Guid))
			return DataTypeIds.Guid;

		if(type == typeof(DateTimeOffset))
			return DataTypeIds.DateTime;

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
			_ => default,
		};
	}

	public static Type GetDataType(this NodeId id, int rank)
	{
		if(id.IsNull)
			return null;

		if(id == DataTypeIds.Byte)
			return GetType(typeof(byte), rank);
		if(id == DataTypeIds.SByte)
			return GetType(typeof(sbyte), rank);
		if(id == DataTypeIds.Boolean)
			return GetType(typeof(bool), rank);
		if(id == DataTypeIds.Int16)
			return GetType(typeof(short), rank);
		if(id == DataTypeIds.Int32 || id == DataTypeIds.Integer)
			return GetType(typeof(int), rank);
		if(id == DataTypeIds.Int64)
			return GetType(typeof(long), rank);
		if(id == DataTypeIds.UInt16)
			return GetType(typeof(ushort), rank);
		if(id == DataTypeIds.UInt32 || id == DataTypeIds.UInteger)
			return GetType(typeof(uint), rank);
		if(id == DataTypeIds.UInt64)
			return GetType(typeof(ulong), rank);
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

		if(rank == 0 || rank == ValueRanks.Scalar)
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

	public static Type DiscriminateType(Type type, out int rank)
	{
		if(type == null)
		{
			rank = 0;
			return null;
		}

		if(type == typeof(string))
		{
			rank = ValueRanks.Scalar;
			return type;
		}

		if(type.IsArray)
		{
			rank = type.GetArrayRank();
			return type.GetElementType();
		}

		if(Common.TypeExtension.IsNullable(type, out var underlyingType))
		{
			rank = ValueRanks.Scalar;
			return underlyingType;
		}

		if(type.IsPrimitive)
		{
			rank = ValueRanks.Scalar;
			return type;
		}

		var elementType = Common.TypeExtension.GetElementType(type);
		if(elementType == null)
		{
			rank = ValueRanks.Scalar;
			return type;
		}

		rank = ValueRanks.OneDimension;
		return Common.TypeExtension.IsNullable(elementType, out underlyingType) ? underlyingType : elementType;
	}

	public static async ValueTask<IUserIdentity> GetIdentityAsync(this Configuration.OpcConnectionSettings settings, CancellationToken cancellation)
	{
		if(settings == null)
			return null;

		if(!string.IsNullOrEmpty(settings.UserName))
			return new UserIdentity(settings.UserName, Encoding.UTF8.GetBytes(settings.Password ?? string.Empty));

		if(!string.IsNullOrEmpty(settings.Certificate))
		{
			using var certificate = await GetCertificateAsync(settings.Certificate, settings.CertificateSecret, cancellation);
			var identifier = new CertificateIdentifier { Thumbprint = certificate.Thumbprint, SubjectName = certificate.Subject };
			return await UserIdentity.CreateAsync(identifier, new CertificatePasswordProvider(settings.CertificateSecret.AsSpan()), new IdentityCertificateProvider(settings.Certificate, settings.CertificateSecret), cancellation);
		}

		return null;
	}

	private static async ValueTask<X509Certificate2> GetCertificateAsync(string text, string secret, CancellationToken cancellation)
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
				throw new InvalidOperationException(string.Format(Properties.Resources.Certificate_NotFound_Message, text[(index + 1)..], store.Name, store.Location));

			return result[0];
		}

		//如果证书文本包含冒号则表示采用虚拟文件路径
		if(text.Contains(':'))
		{
			await using var stream = await Zongsoft.IO.FileSystem.File.OpenAsync(text, FileMode.Open, FileAccess.Read, cancellation: cancellation);
			return await LoadX509Async(stream, secret, cancellation);
		}

		//解析相对文件路径
		if(!Path.IsPathRooted(text))
		{
			text = Path.Combine(Path.GetDirectoryName(typeof(Utility).Assembly.Location), "certificates", text);
			if(!File.Exists(text))
				text = Path.Combine(ApplicationContext.Current.ApplicationPath, "certificates", text);
		}

		await using var file = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
		return await LoadX509Async(file, secret, cancellation);

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

		static async ValueTask<X509Certificate2> LoadX509Async(Stream stream, string secret, CancellationToken cancellation)
		{
			using var buffer = new MemoryStream();
			await stream.CopyToAsync(buffer, cancellation);
			var data = buffer.ToArray();

			#if NET9_0_OR_GREATER

			//获取证书类型
			var type = X509Certificate2.GetCertContentType(data);

			//返回加载的证书
			return type == X509ContentType.Pfx || type == X509ContentType.Pkcs12 || type == X509ContentType.Pkcs7 ?
				X509CertificateLoader.LoadPkcs12(data, secret, X509KeyStorageFlags.Exportable) :
				X509CertificateLoader.LoadCertificate([.. data]);
			#else
			return string.IsNullOrEmpty(secret) ?
				new X509Certificate2(data) :
				new X509Certificate2(data, secret);
			#endif
		}
	}
	#endregion

	#region 证书提供
	private sealed class IdentityCertificateProvider(string path, string secret) : ICertificateProvider
	{
		public global::Opc.Ua.Security.Certificates.Certificate TryGetPrivateKeyCertificate(string thumbprint) => null;
		public async ValueTask<global::Opc.Ua.Security.Certificates.Certificate> GetPrivateKeyCertificateAsync(CertificateIdentifier identifier, ICertificatePasswordProvider passwordProvider = null, string applicationUri = null, CancellationToken ct = default)
		{
			var certificate = await GetCertificateAsync(path, secret, ct);
			return global::Opc.Ua.Security.Certificates.Certificate.From(certificate);
		}
	}
	#endregion
}
