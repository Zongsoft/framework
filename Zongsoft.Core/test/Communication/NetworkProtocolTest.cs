using System;
using System.Linq;
using System.Net.Sockets;
using System.Globalization;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Configuration;
using Zongsoft.Text.Templating;

namespace Zongsoft.Communication.Tests;

public class NetworkProtocolTest
{
	[Theory]
	[InlineData("http", null, "http", 0, ProtocolType.Unspecified)]
	[InlineData(" HTTP1 ", null, "http1", 1, ProtocolType.Tcp)]
	[InlineData("http2", null, "http2", 2, ProtocolType.Tcp)]
	[InlineData("http3", null, "http3", 3, ProtocolType.Udp)]
	[InlineData("tcp", null, "tcp", 0, ProtocolType.Tcp)]
	[InlineData("UDP", null, "udp", 0, ProtocolType.Udp)]
	[InlineData("grpc+http", "grpc", "http", 0, ProtocolType.Unspecified)]
	[InlineData("grpc+http1", "grpc", "http1", 1, ProtocolType.Tcp)]
	[InlineData("grpc+http2", "grpc", "http2", 2, ProtocolType.Tcp)]
	[InlineData(" HTTP2 + GRPC ", "grpc", "http2", 2, ProtocolType.Tcp)]
	[InlineData("grpc+http3", "grpc", "http3", 3, ProtocolType.Udp)]
	[InlineData("http3+grpc", "grpc", "http3", 3, ProtocolType.Udp)]
	[InlineData("grpc+tcp", "grpc", "tcp", 0, ProtocolType.Tcp)]
	[InlineData(" TCP + gRPC ", "grpc", "tcp", 0, ProtocolType.Tcp)]
	[InlineData("protobuf+http", "protobuf", "http", 0, ProtocolType.Unspecified)]
	[InlineData("http+protobuf", "protobuf", "http", 0, ProtocolType.Unspecified)]
	[InlineData("protobuf+http1", "protobuf", "http1", 1, ProtocolType.Tcp)]
	[InlineData("http1+protobuf", "protobuf", "http1", 1, ProtocolType.Tcp)]
	[InlineData("protobuf+http2", "protobuf", "http2", 2, ProtocolType.Tcp)]
	[InlineData("http2+protobuf", "protobuf", "http2", 2, ProtocolType.Tcp)]
	[InlineData("protobuf+http3", "protobuf", "http3", 3, ProtocolType.Udp)]
	[InlineData("http3+protobuf", "protobuf", "http3", 3, ProtocolType.Udp)]
	[InlineData("protobuf+tcp", "protobuf", "tcp", 0, ProtocolType.Tcp)]
	[InlineData("tcp+protobuf", "protobuf", "tcp", 0, ProtocolType.Tcp)]
	[InlineData("OPC+HTTP2", "opc", "http2", 2, ProtocolType.Tcp)]
	[InlineData("http2+opc", "opc", "http2", 2, ProtocolType.Tcp)]
	[InlineData("custom.v2-protocol_1+http3", "custom.v2-protocol_1", "http3", 3, ProtocolType.Udp)]
	[InlineData("http3+custom.v2-protocol_1", "custom.v2-protocol_1", "http3", 3, ProtocolType.Udp)]
	[InlineData("myapp+udp", "myapp", "udp", 0, ProtocolType.Udp)]
	[InlineData("UDP+MyApp", "myapp", "udp", 0, ProtocolType.Udp)]
	public void ParseCanonicalCombinations(string text, string name, string carrier, int version, ProtocolType transport)
	{
		Assert.True(NetworkProtocol.TryParse(text, out var protocol));
		Assert.Equal(name, protocol.Name);
		Assert.Equal(carrier, protocol.Carrier);
		Assert.Equal(version, protocol.HttpVersion);
		Assert.Equal(transport, protocol.Transport);
		Assert.Equal(carrier is "http" or "http1" or "http2" or "http3", protocol.IsHttp);
		Assert.False(protocol.IsEmpty);
		Assert.Equal(name == null ? carrier : name + "+" + carrier, protocol.ToString());
		Assert.Equal(protocol, NetworkProtocol.Parse(protocol.ToString()));
		Assert.Equal(protocol, NetworkProtocol.Parse(text));
		Assert.Equal(protocol, NetworkProtocol.Parse(text.AsSpan()));
		Assert.True(NetworkProtocol.TryParse(text.AsSpan(), out var spanResult));
		Assert.Equal(protocol, spanResult);
	}

	[Theory]
	[InlineData("grpc", "grpc")]
	[InlineData("Protobuf", "protobuf")]
	[InlineData(" OPC ", "opc")]
	[InlineData("custom.v2-protocol_1", "custom.v2-protocol_1")]
	[InlineData("http4", "http4")]
	[InlineData("Http1AndHttp2", "http1andhttp2")]
	[InlineData("opc.tcp", "opc.tcp")]
	public void PreserveUnspecifiedCarriers(string text, string expected)
	{
		var protocol = NetworkProtocol.Parse(text);
		Assert.Equal(expected, protocol.Name);
		Assert.Equal(expected, protocol.ToString());
		Assert.Null(protocol.Carrier);
		Assert.False(protocol.IsEmpty);
		Assert.False(protocol.IsHttp);
		Assert.Equal(0, protocol.HttpVersion);
		Assert.Equal(ProtocolType.Unspecified, protocol.Transport);
		Assert.NotEqual(protocol, NetworkProtocol.Parse(expected + "+http2"));
		Assert.NotEqual(default, protocol);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" \t ")]
	[InlineData("foo+bar")]
	[InlineData("grpc+http4")]
	[InlineData("grpc+grpc")]
	[InlineData("grpc+protobuf")]
	[InlineData("http2+tcp")]
	[InlineData("tcp+http2")]
	[InlineData("tcp+udp")]
	[InlineData("http+http3")]
	[InlineData("http2+http2")]
	[InlineData("protobuf+http2+tcp")]
	[InlineData("grpc+")]
	[InlineData("+grpc")]
	[InlineData("grpc++http2")]
	[InlineData("grpc,http2")]
	[InlineData("gr pc")]
	[InlineData("1app+tcp")]
	[InlineData("_app+tcp")]
	[InlineData("my/app+tcp")]
	[InlineData("my:app+tcp")]
	[InlineData("my@app+tcp")]
	[InlineData("my\0app+tcp")]
	[InlineData("应用+tcp")]
	[InlineData("${option.Protocols}")]
	public void RejectInvalidCombinations(string text)
	{
		Assert.False(NetworkProtocol.TryParse(text, out var result));
		Assert.Equal(default, result);
		Assert.Throws<FormatException>(() => NetworkProtocol.Parse(text));
		Assert.False(NetworkProtocol.TryParse(text.AsSpan(), out var spanResult));
		Assert.Equal(default, spanResult);
		Assert.Throws<FormatException>(() => NetworkProtocol.Parse(text.AsSpan()));
	}

	[Theory]
	[InlineData("grpc+http2,http2+grpc,GRPC+HTTP2", "grpc+http2")]
	[InlineData(" grpc+http2 , HTTP2 + GRPC , http3 ", "grpc+http2,http3")]
	[InlineData("http,http1,http3", "http1,http3")]
	[InlineData("protobuf+http,protobuf+http2", "protobuf+http2")]
	[InlineData("protobuf+http,http2", "http2,protobuf+http")]
	[InlineData("http,grpc+http2", "grpc+http2,http")]
	[InlineData("protobuf+http,protobuf+tcp", "protobuf+http,protobuf+tcp")]
	[InlineData("grpc+tcp,grpc+http3,grpc+http2", "grpc+http2,grpc+http3,grpc+tcp")]
	[InlineData("http3,grpc", "grpc,http3")]
	[InlineData("grpc,grpc+http2,protobuf", "grpc,grpc+http2,protobuf")]
	[InlineData("http2,grpc+http2", "grpc+http2,http2")]
	[InlineData("custom+http,custom+http2,custom+http3,other+http", "custom+http2,custom+http3,other+http")]
	[InlineData("udp,tcp,http", "http,tcp,udp")]
	[InlineData("http,http1,http2,http3,protobuf+http,protobuf+http1,protobuf+http3,grpc+http3,grpc+http2", "grpc+http2,grpc+http3,http1,http2,http3,protobuf+http1,protobuf+http3")]
	public void NormalizeAlternatives(string text, string expected)
	{
		Assert.True(NetworkProtocol.TryParse(text, ',', out var protocols));
		Assert.Equal(expected.Split(','), protocols.Select(protocol => protocol.ToString()));
		Assert.Equal(protocols, NetworkProtocol.Parse(text, ','));
		Assert.Equal(protocols, NetworkProtocol.Parse(string.Join(',', Enumerable.Reverse(text.Split(','))), ','));
		Assert.Equal(protocols, NetworkProtocol.Parse(expected, ','));
		Assert.Equal(protocols, NetworkProtocol.Parse(text.AsSpan(), ','));
		Assert.True(NetworkProtocol.TryParse(text.AsSpan(), ',', out var spanResult));
		Assert.Equal(protocols, spanResult);
	}

	[Theory]
	[InlineData(';')]
	[InlineData('|')]
	[InlineData(',')]
	public void ParseSeparatedLists(char separator)
	{
		var text = $"app+http2{separator} http3+app{separator}APP+HTTP2";
		Assert.Equal(["app+http2", "app+http3"], NetworkProtocol.Parse(text, separator).Select(protocol => protocol.ToString()));
		Assert.True(NetworkProtocol.TryParse(text, separator, out var protocols));
		Assert.Equal(NetworkProtocol.Parse(text.AsSpan(), separator), protocols);
		Assert.True(NetworkProtocol.TryParse("app+http2", out var single));
		Assert.Equal(single, NetworkProtocol.Parse("app+http2"));
		Assert.Equal([single], NetworkProtocol.Parse("app+http2", separator));
	}

	[Theory]
	[InlineData('+')]
	[InlineData('.')]
	[InlineData('-')]
	[InlineData('_')]
	[InlineData('a')]
	[InlineData('1')]
	[InlineData(' ')]
	[InlineData('\t')]
	[InlineData('\0')]
	[InlineData('\ud800')]
	public void RejectInvalidSeparators(char separator)
	{
		Assert.False(NetworkProtocol.TryParse("grpc+http2", separator, out var protocols));
		Assert.Empty(protocols);
		Assert.Throws<FormatException>(() => NetworkProtocol.Parse("grpc+http2", separator));
		Assert.False(NetworkProtocol.TryParse("grpc+http2".AsSpan(), separator, out protocols));
		Assert.Empty(protocols);
		Assert.Throws<FormatException>(() => NetworkProtocol.Parse("grpc+http2".AsSpan(), separator));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(",")]
	[InlineData("grpc,")]
	[InlineData(",grpc")]
	[InlineData("grpc, ,http3")]
	[InlineData("http1,foo+bar,http3")]
	[InlineData("http1;http2")]
	[InlineData("http1|http2")]
	public void RejectInvalidLists(string text)
	{
		Assert.False(NetworkProtocol.TryParse(text, ',', out var protocols));
		Assert.Empty(protocols);
		Assert.Throws<FormatException>(() => NetworkProtocol.Parse(text, ','));
		Assert.False(NetworkProtocol.TryParse(text.AsSpan(), ',', out protocols));
		Assert.Empty(protocols);
		Assert.Throws<FormatException>(() => NetworkProtocol.Parse(text.AsSpan(), ','));
	}

	[Fact]
	public void PreserveValueSemantics()
	{
		var grpc = NetworkProtocol.Parse("grpc+http2");
		var equivalent = NetworkProtocol.Parse("HTTP2+GRPC");

		Assert.True(grpc == equivalent);
		Assert.False(grpc != equivalent);
		Assert.True(grpc.Equals((object)equivalent));
		Assert.Equal(grpc.GetHashCode(), equivalent.GetHashCode());
		Assert.Single(new HashSet<NetworkProtocol> { grpc, equivalent });

		Assert.NotEqual(grpc, NetworkProtocol.Parse("http2"));
		Assert.NotEqual(grpc, NetworkProtocol.Parse("protobuf+http2"));
		Assert.NotEqual(grpc, NetworkProtocol.Parse("grpc+http3"));
		Assert.NotEqual(grpc, NetworkProtocol.Parse("grpc"));
		Assert.False(grpc.Equals(null));
		Assert.False(grpc.Equals("grpc+http2"));
		Assert.True(grpc != default);

		var empty = default(NetworkProtocol);
		Assert.True(empty.IsEmpty);
		Assert.Null(empty.Name);
		Assert.Null(empty.Carrier);
		Assert.False(empty.IsHttp);
		Assert.Equal(0, empty.HttpVersion);
		Assert.Equal(ProtocolType.Unspecified, empty.Transport);
		Assert.Equal(string.Empty, empty.ToString());
		Assert.Equal(default, empty);
	}

	[Fact]
	public void EvaluateProtocolTemplate()
	{
		var evaluator = new TemplateEvaluator();
		evaluator.Providers.Add(new Variables
		{
			["option"] = new
			{
				Protocols = "HTTP2+GRPC, grpc+http3",
				Settings = new Settings("protocols='protobuf+http1,http3+protobuf';"),
			},
		});

		Assert.Equal(["grpc+http2", "grpc+http3"], NetworkProtocol.Parse(evaluator.Evaluate("${option.Protocols}"), ',').Select(protocol => protocol.ToString()));
		Assert.Equal(["protobuf+http1", "protobuf+http3"], NetworkProtocol.Parse(evaluator.Evaluate("${option.Settings['protocols']}"), ',').Select(protocol => protocol.ToString()));
		Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${option.Missing}"));
	}

	[Fact]
	public void ProtocolStandardInterfaces()
	{
		AssertContract<NetworkProtocol>(" HTTP2 + CUSTOM.Protocol ", "custom.protocol+http2", "custom+unknown");
		AssertContract<NetworkProtocol>(" GRPC ", "grpc", "http2+tcp");
		AssertContract<NetworkProtocol>(" UDP ", "udp", "tcp+udp");
	}

	[Fact]
	public void ProtocolSpanSlicesOwnTheirData()
	{
		const string PROTOCOL = " HTTP2 + CUSTOM ";
		var buffer = ("before" + PROTOCOL + "after").ToCharArray();
		var text = buffer.AsSpan(6, PROTOCOL.Length);
		var parsed = NetworkProtocol.Parse(text);

		Assert.True(NetworkProtocol.TryParse(text, out var tried));
		Array.Fill(buffer, '?');
		Assert.Equal("custom+http2", parsed.ToString());
		Assert.Equal(parsed, tried);
		Assert.False(NetworkProtocol.TryParse(default(ReadOnlySpan<char>), out var empty));
		Assert.Equal(default, empty);
	}

	[Fact]
	public void CollectionSpanSlicesOwnTheirData()
	{
		const string PROTOCOLS = " HTTP3+CUSTOM, custom+http2, custom+http ";
		var buffer = ("before" + PROTOCOLS + "after").ToCharArray();
		var text = buffer.AsSpan(6, PROTOCOLS.Length);
		var parsed = NetworkProtocol.Parse(text, ',');

		Assert.True(NetworkProtocol.TryParse(text, ',', out var tried));
		Array.Fill(buffer, '?');
		Assert.Equal(["custom+http2", "custom+http3"], parsed.Select(protocol => protocol.ToString()));
		Assert.Equal(parsed, tried);
		Assert.False(NetworkProtocol.TryParse(default(ReadOnlySpan<char>), ',', out var empty));
		Assert.Empty(empty);
	}

	private static void AssertContract<T>(string input, string expected, string invalid) where T : ISpanParsable<T>
	{
		foreach(var provider in new IFormatProvider[] { null, CultureInfo.GetCultureInfo("tr-TR"), CultureInfo.GetCultureInfo("ar-SA"), new UnusedProvider() })
		{
			var value = ParseString<T>(input, provider);
			Assert.Equal(expected, value.ToString());
			Assert.True(T.TryParse(input, provider, out var stringResult));
			Assert.Equal(value, stringResult);
			Assert.Equal(value, T.Parse(input.AsSpan(), provider));
			Assert.True(T.TryParse(input.AsSpan(), provider, out var spanResult));
			Assert.Equal(value, spanResult);
			Assert.False(T.TryParse(invalid, provider, out stringResult));
			Assert.Equal(default, stringResult);
			Assert.False(T.TryParse(invalid.AsSpan(), provider, out spanResult));
			Assert.Equal(default, spanResult);
			Assert.Throws<FormatException>(() => ParseString<T>(invalid, provider));
			Assert.Throws<FormatException>(() => T.Parse(invalid.AsSpan(), provider));
			Assert.False(T.TryParse((string)null, provider, out stringResult));
			Assert.Equal(default, stringResult);
			Assert.False(T.TryParse(default(ReadOnlySpan<char>), provider, out spanResult));
			Assert.Equal(default, spanResult);
			Assert.Throws<FormatException>(() => ParseString<T>(null, provider));
			Assert.Throws<FormatException>(() => T.Parse(default(ReadOnlySpan<char>), provider));
		}
	}

	private static T ParseString<T>(string text, IFormatProvider provider) where T : IParsable<T> => T.Parse(text, provider);

	private sealed class UnusedProvider : IFormatProvider
	{
		public object GetFormat(Type formatType) => throw new InvalidOperationException();
	}
}
