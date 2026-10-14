using System;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Communication.Tests;

public class NetworkAddressTest
{
	[Theory]
	[InlineData(" HTTP://*:80 ", "http://0.0.0.0:80/", "http", "0.0.0.0", UriHostNameType.IPv4, AddressFamily.InterNetwork, 80, "/", null, true, false)]
	[InlineData("http://0.0.0.0:4317/opentelemetry", "http://0.0.0.0:4317/opentelemetry", "http", "0.0.0.0", UriHostNameType.IPv4, AddressFamily.InterNetwork, 4317, "/opentelemetry", null, true, false)]
	[InlineData("https://EXAMPLE.COM./Api/", "https://example.com:443/Api/", "https", "example.com", UriHostNameType.Dns, AddressFamily.Unspecified, 443, "/Api/", null, false, false)]
	[InlineData("http://例子.测试/a-._~9", "http://xn--fsqu00a.xn--0zwm56d:80/a-._~9", "http", "xn--fsqu00a.xn--0zwm56d", UriHostNameType.Dns, AddressFamily.Unspecified, 80, "/a-._~9", null, false, false)]
	[InlineData("tcp://LOCALHOST:1", "tcp://localhost:1/", "tcp", "localhost", UriHostNameType.Dns, AddressFamily.Unspecified, 1, "/", null, false, true)]
	[InlineData("udp://127.2.3.4:65535/", "udp://127.2.3.4:65535/", "udp", "127.2.3.4", UriHostNameType.IPv4, AddressFamily.InterNetwork, 65535, "/", null, false, true)]
	[InlineData("http://[::]:4317/", "http://[::]:4317/", "http", "::", UriHostNameType.IPv6, AddressFamily.InterNetworkV6, 4317, "/", null, true, false)]
	[InlineData("https://[0:0:0:0:0:0:0:1]", "https://[::1]:443/", "https", "::1", UriHostNameType.IPv6, AddressFamily.InterNetworkV6, 443, "/", null, false, true)]
	[InlineData("tcp://[FE80:0::1%25Ethernet]:9000", "tcp://[fe80::1%25Ethernet]:9000/", "tcp", "fe80::1", UriHostNameType.IPv6, AddressFamily.InterNetworkV6, 9000, "/", "Ethernet", false, false)]
	[InlineData("tcp://[::ffff:192.0.2.1]:9000", "tcp://[::ffff:192.0.2.1]:9000/", "tcp", "::ffff:192.0.2.1", UriHostNameType.IPv6, AddressFamily.InterNetworkV6, 9000, "/", null, false, false)]
	[InlineData("opc.tcp://localhost:4840/server/../A%2FB", "opc.tcp://localhost:4840/server/../A%2FB", "opc.tcp", "localhost", UriHostNameType.Dns, AddressFamily.Unspecified, 4840, "/server/../A%2FB", null, false, true)]
	public void ParseAddresses(string text, string canonical, string scheme, string host, UriHostNameType type, AddressFamily family, int port, string path, string zone, bool any, bool loopback)
	{
		Assert.True(NetworkAddress.TryParse(text, out var address));
		Assert.Equal(canonical, address.ToString());
		Assert.Equal(scheme, address.Scheme);
		Assert.Equal(host, address.Host);
		Assert.Equal(type, address.HostType);
		Assert.Equal(family, address.AddressFamily);
		Assert.Equal(port, address.Port);
		Assert.Equal(path, address.Path);
		Assert.Equal(zone, address.Zone);
		Assert.Equal(any, address.IsAny);
		Assert.Equal(loopback, address.IsLoopback);
		Assert.Equal(address, NetworkAddress.Parse(text));
		Assert.Equal(address, NetworkAddress.Parse(canonical));
		Assert.Equal(address, NetworkAddress.Parse(text.AsSpan()));
		Assert.True(NetworkAddress.TryParse(text.AsSpan(), out var spanResult));
		Assert.Equal(address, spanResult);
	}

	[Theory]
	[InlineData("OPC.TCP", "opc.tcp")]
	[InlineData("mqtt", "mqtt")]
	[InlineData("custom", "custom")]
	[InlineData("custom+v2-transport.1", "custom+v2-transport.1")]
	[InlineData("ftp", "ftp")]
	[InlineData("grpc", "grpc")]
	public void ParseGenericSchemes(string scheme, string expected)
	{
		var address = NetworkAddress.Parse($"{scheme}://LOCALHOST:9000/service");
		Assert.Equal(expected, address.Scheme);
		Assert.Equal("localhost", address.Host);
		Assert.Equal(9000, address.Port);
		Assert.Equal("/service", address.Path);
		Assert.Equal($"{expected}://localhost:9000/service", address.ToString());
		Assert.Equal(address, NetworkAddress.Parse(address.ToString()));
		Assert.False(NetworkAddress.TryParse($"{scheme}://localhost/service", out var missingPort));
		Assert.Null(missingPort);
	}

	[Theory]
	[InlineData("/metrics/")]
	[InlineData("/%2f/%61")]
	[InlineData("/中文/😀")]
	[InlineData("/a/./../b")]
	[InlineData("/a//b//")]
	[InlineData("/*")]
	[InlineData("/a:@!$&'()*+,;=~._-")]
	public void PreserveGenericPaths(string path)
	{
		foreach(var scheme in new[] { "http", "https", "tcp", "udp", "custom" })
		{
			var text = $"{scheme}://localhost:9000{path}";
			var address = NetworkAddress.Parse(text);

			Assert.Equal(path, address.Path);
			Assert.Equal(text, address.ToString());
			Assert.Equal(address, NetworkAddress.Parse(text.AsSpan()));
		}
	}

	[Theory]
	[InlineData("http://*:80", "HTTP://0.0.0.0/")]
	[InlineData("http://EXAMPLE.COM./", "http://example.com:80/")]
	[InlineData("https://例子.测试", "https://xn--fsqu00a.xn--0zwm56d/")]
	[InlineData("tcp://[FE80:0::1%25Ethernet]:9000", "tcp://[fe80::1%25Ethernet]:9000/")]
	public void NormalizeEquivalentAddresses(string leftText, string rightText)
	{
		var left = NetworkAddress.Parse(leftText);
		var right = NetworkAddress.Parse(rightText);

		Assert.True(left == right);
		Assert.False(left != right);
		Assert.True(left.Equals((object)right));
		Assert.Equal(left.GetHashCode(), right.GetHashCode());
		Assert.Single(new HashSet<NetworkAddress> { left, right });
	}

	[Theory]
	[InlineData("http://*:80/metrics", "http://*:80/metrics/")]
	[InlineData("http://*:80/metrics", "http://*:80/Metrics")]
	[InlineData("http://*:80/", "http://[::]:80/")]
	[InlineData("tcp://192.0.2.1:9000", "tcp://[::ffff:192.0.2.1]:9000")]
	[InlineData("tcp://[fe80::1%25Ethernet]:9000", "tcp://[fe80::1%25ethernet]:9000")]
	[InlineData("tcp://[fe80::1]:9000", "tcp://[fe80::1%251]:9000")]
	[InlineData("http://localhost:80", "http://127.0.0.1:80")]
	[InlineData("http://localhost:80", "https://localhost:80")]
	[InlineData("http://localhost:80", "http://localhost:81")]
	[InlineData("custom://localhost:9000/a/../b", "custom://localhost:9000/b")]
	[InlineData("custom://localhost:9000/%61", "custom://localhost:9000/a")]
	public void PreserveAddressIdentity(string leftText, string rightText)
	{
		var left = NetworkAddress.Parse(leftText);
		var right = NetworkAddress.Parse(rightText);

		Assert.False(left == right);
		Assert.True(left != right);
		Assert.False(left.Equals((object)right));
		Assert.Equal(2, new HashSet<NetworkAddress> { left, right }.Count);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("localhost:80")]
	[InlineData("://localhost:80")]
	[InlineData("unix:///run/service.sock")]
	[InlineData("1custom://localhost:9000")]
	[InlineData("custom_name://localhost:9000")]
	[InlineData("custom%20://localhost:9000")]
	[InlineData("应用://localhost:9000")]
	[InlineData("tcp://localhost")]
	[InlineData("udp://localhost")]
	[InlineData("opc.tcp://localhost/server")]
	[InlineData("http://localhost:")]
	[InlineData("http://localhost:0")]
	[InlineData("http://localhost:-1")]
	[InlineData("http://localhost:+80")]
	[InlineData("http://localhost:65536")]
	[InlineData("http://localhost:999999999999")]
	[InlineData("http://localhost:abc")]
	[InlineData("http://localhost:80:90")]
	[InlineData("http://user:password@localhost")]
	[InlineData("http://localhost/?a=1")]
	[InlineData("http://localhost/#fragment")]
	[InlineData("http://localhost/with space")]
	[InlineData("http://local\thost/")]
	[InlineData("http://localhost/a\0b")]
	[InlineData("http://localhost/a\\b")]
	[InlineData("http://localhost/{name}")]
	[InlineData("http://localhost/%")]
	[InlineData("http://localhost/%2")]
	[InlineData("http://localhost/%gg")]
	[InlineData("custom://localhost:9000/a<b")]
	[InlineData("http://")]
	[InlineData("http://:80/")]
	[InlineData("http://+/path")]
	[InlineData("http://*.example.com/")]
	[InlineData("http://bad_host/")]
	[InlineData("http://bad..host/")]
	[InlineData("http://bad.host../")]
	[InlineData("http://-host/")]
	[InlineData("http://host-/")]
	[InlineData("http://256.1.1.1/")]
	[InlineData("http://::1:80/")]
	[InlineData("http://[127.0.0.1]:80/")]
	[InlineData("http://[::1/path")]
	[InlineData("http://[]:80/")]
	[InlineData("http://[::1]extra/")]
	[InlineData("http://[::1]:/")]
	[InlineData("http://[fe80::1%Ethernet]/")]
	[InlineData("http://[fe80::1%25]/")]
	[InlineData("http://[fe80::1%25Ethernet%252]/")]
	public void RejectInvalidAddresses(string text)
	{
		Assert.False(NetworkAddress.TryParse(text, out var address));
		Assert.Null(address);
		Assert.Throws<FormatException>(() => NetworkAddress.Parse(text));
		Assert.False(NetworkAddress.TryParse(text.AsSpan(), out var spanResult));
		Assert.Null(spanResult);
		Assert.Throws<FormatException>(() => NetworkAddress.Parse(text.AsSpan()));
	}

	[Fact]
	public void ValidateDnsBoundaries()
	{
		var label = new string('a', 63);
		var host = $"{label}.{label}.{label}.{new string('b', 61)}";

		Assert.Equal(253, host.Length);
		Assert.Equal(host, NetworkAddress.Parse($"http://{host}").Host);
		Assert.False(NetworkAddress.TryParse($"http://{host}b", out var address));
		Assert.Null(address);
		Assert.False(NetworkAddress.TryParse($"http://{label}a.example", out address));
		Assert.Null(address);
		Assert.False(NetworkAddress.TryParse("http://\ud800.example", out address));
		Assert.Null(address);
	}

	[Theory]
	[InlineData('\ud800')]
	[InlineData('\udc00')]
	public void RejectMalformedUnicode(char character)
	{
		var text = "http://localhost/" + new string(character, 1);
		Assert.False(NetworkAddress.TryParse(text, out var address));
		Assert.Null(address);
		Assert.False(NetworkAddress.TryParse(text.AsSpan(), out address));
		Assert.Null(address);
		Assert.Throws<FormatException>(() => NetworkAddress.Parse(text));
	}

	[Fact]
	public void PreserveNullSemantics()
	{
		var address = NetworkAddress.Parse("http://localhost");
		NetworkAddress empty = null;

		Assert.True(empty == null);
		Assert.False(empty != null);
		Assert.False(address == null);
		Assert.True(address != null);
		Assert.False(empty == address);
		Assert.True(empty != address);
		Assert.False(address.Equals((NetworkAddress)null));
		Assert.False(address.Equals((object)null));
		Assert.False(address.Equals("http://localhost:80/"));
	}

	[Fact]
	public void AddressStandardInterfaces()
	{
		AssertContract<NetworkAddress>(" CUSTOM://LOCALHOST:9000/路径/%2f ", "custom://localhost:9000/路径/%2f", "custom://localhost:9000/%xy");
	}

	[Fact]
	public void AddressSpanSlicesOwnTheirData()
	{
		const string ADDRESS = "CUSTOM://LOCALHOST:9000/路径/%2f";
		var buffer = ("before" + ADDRESS + "after").ToCharArray();
		var text = buffer.AsSpan(6, ADDRESS.Length);
		var parsed = NetworkAddress.Parse(text);

		Assert.True(NetworkAddress.TryParse(text, out var tried));
		Array.Fill(buffer, '?');
		Assert.Equal("custom://localhost:9000/路径/%2f", parsed.ToString());
		Assert.Equal(parsed, tried);
		Assert.False(NetworkAddress.TryParse(default(ReadOnlySpan<char>), out var empty));
		Assert.Null(empty);
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
