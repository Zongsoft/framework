using System;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Communication;

using Xunit;

namespace Zongsoft.Web.Hosting.Tests;

public class WebEndpointCollectionTest
{
	[Fact]
	public void LogicalEndpointsCopyDeclarationsAndNormalizeWithoutServerConfiguration()
	{
		var protocols = new[] { NetworkProtocol.Parse("custom") };
		var source = new WebEndpoint("api", NetworkAddress.Parse("https://127.0.0.1:9000/api"), protocols);
		var endpoints = new List<WebEndpoint> { source };
		var collection = new WebEndpointCollection(endpoints, [new WebEndpointProtocol("custom", "http2")]);
		protocols[0] = NetworkProtocol.Parse("http1");
		endpoints.Clear();
		var endpoint = Assert.Single(collection);
		Assert.Equal("custom+http2", Assert.Single(endpoint.Protocols).ToString());
		Assert.Equal("custom", Assert.Single(source.Protocols).ToString());
		Assert.Equal("/api", endpoint.Address.Path);
		Assert.True(endpoint.Supports(2));
		Assert.False(endpoint.Supports(1));
		Assert.False(endpoint.Supports(0));
		Assert.Throws<NotSupportedException>(() => ((IList<NetworkProtocol>)endpoint.Protocols).Clear());
	}

	[Theory]
	[InlineData("https://127.0.0.1:9000/a", "http1", "https://127.0.0.1:9000/a", "protobuf+http2")]
	[InlineData("http://127.0.0.1:9000/a", "http2", "http://127.0.0.1:9001/A", "http2")]
	public void ConflictingRequestRangesAreRejectedIndependently(string first, string firstProtocols, string second, string secondProtocols)
	{
		var endpoints = new[]
		{
			new WebEndpoint("a", NetworkAddress.Parse(first), NetworkProtocol.Parse(firstProtocols, ',')),
			new WebEndpoint("b", NetworkAddress.Parse(second), NetworkProtocol.Parse(secondProtocols, ',')),
		};
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(endpoints, [new WebEndpointProtocol("protobuf")]));
	}

	[Fact]
	public void CollectionOrdersLogicalRangesAndRejectsDuplicateNames()
	{
		var first = new WebEndpoint("a", NetworkAddress.Parse("http://127.0.0.1:9000/a"), [NetworkProtocol.Parse("http2")]);
		var second = new WebEndpoint("b", NetworkAddress.Parse("http://127.0.0.1:9000/a/b"), [NetworkProtocol.Parse("http2")]);
		var collection = new WebEndpointCollection([first, second]);
		Assert.Equal(["b", "a"], collection.Select(endpoint => endpoint.Name));
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection([first, first]));
	}
}
