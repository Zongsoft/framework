using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Google.Protobuf;
using Google.Protobuf.Reflection;

using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

using P = Zongsoft.Web.Grpc.Tests.Protocols;
using S = Zongsoft.Web.Grpc.Tests.GrpcRegionServices;

namespace Zongsoft.Web.Grpc.Tests;

public class GrpcRegionTest
{
	[Fact]
	public async Task RegionsKeepImplementationsAndListenersIsolated()
	{
		var first = GrpcRegionHost.Port();
		var second = GrpcRegionHost.Port();

		await using var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha), typeof(S.Beta)], [(first, "/a"), (first, "/mirror"), (second, "/b")]);
		var alpha = new P.Echo.EchoClient(host.Channel(first, "/a"));
		var beta = new P.Echo.EchoClient(host.Channel(second, "/b"));

		Assert.Equal("alpha:hello", (await alpha.UnaryAsync(new P.Frame { Text = "hello" }, deadline: Deadline())).Text);
		Assert.Equal("beta:hello", (await beta.UnaryAsync(new P.Frame { Text = "hello" }, deadline: Deadline())).Text);
		await Error(StatusCode.Unimplemented, () => new P.Echo.EchoClient(host.Channel(first, "/b")).UnaryAsync(new P.Frame(), deadline: Deadline()).ResponseAsync);
		await Error(StatusCode.Unimplemented, () => new P.Echo.EchoClient(host.Channel(second, "/a")).UnaryAsync(new P.Frame(), deadline: Deadline()).ResponseAsync);
	}

	[Theory]
	[InlineData("/", "root")]
	[InlineData("/a/", "trailing")]
	[InlineData("/inherited", "inherited")]
	public async Task ExplicitRootTrailingAndInheritedMountsWork(string path, string name)
	{
		var port = GrpcRegionHost.Port();
		var service = name switch
		{
			"root" => typeof(S.Root),
			"trailing" => typeof(S.Trailing),
			_ => typeof(S.Inherited),
		};

		await using var host = await GrpcRegionHost.StartAsync([service], [(port, path)]);
		var client = new P.Echo.EchoClient(host.Channel(port, path));
		Assert.Equal(name + ":hello", (await client.UnaryAsync(new P.Frame { Text = "hello" }, deadline: Deadline())).Text);
		Assert.Equal(1, host.Probe.Created[name]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TemplateCaseAndTrailingSlashDoNotChangeRpcIdentity(bool trailing)
	{
		var first = GrpcRegionHost.Port();
		var second = GrpcRegionHost.Port();
		var service = trailing ? typeof(S.SlashVariant) : typeof(S.CaseVariant);
		await using var host = await GrpcRegionHost.StartAsync([service], [(first, "/a"), (second, "/A/")]);

		foreach(var port in new[] { first, second })
		{
			var client = new P.Echo.EchoClient(host.Channel(port, "/A/"));
			Assert.Equal((trailing ? "slash" : "case") + ":hello", (await client.UnaryAsync(new P.Frame { Text = "hello" }, deadline: Deadline())).Text);
			await Unknown(host.Channel(port, "/A"), "fixture.echo", "Unary");
			await Unknown(host.Channel(port, "/A"), "fixture.Echo", "unary");
		}
	}

	[Fact]
	public async Task MultipleMountsKeepOneContainerOwnedService()
	{
		var first = GrpcRegionHost.Port();
		var second = GrpcRegionHost.Port();
		var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha)], [(first, "/a"), (first, "/mirror"), (second, "/a")]);

		try
		{
			var identities = new List<string>();
			foreach(var (port, path) in new[] { (first, "/a"), (first, "/mirror"), (second, "/a") })
			{
				var client = new P.Echo.EchoClient(host.Channel(port, path));
				identities.Add((await client.UnaryAsync(new P.Frame(), deadline: Deadline())).Instance);
			}

			Assert.Single(identities.Distinct());
			Assert.NotEmpty(identities[0]);
			Assert.Equal(1, host.Probe.Created["alpha"]);
			Assert.Empty(host.Probe.Disposed);
		}
		finally
		{
			await host.DisposeAsync();
		}

		Assert.Equal(1, host.Probe.Disposed["alpha"]);
	}

	[Fact]
	public async Task CallsPreserveMessagesMetadataStatusAndStreaming()
	{
		var port = GrpcRegionHost.Port();
		await using var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha)], [(port, "/a"), (port, "/mirror")]);
		var client = new P.Echo.EchoClient(host.Channel(port, "/a", "?fixture=preserved"));
		var payload = new byte[1024 * 1024];
		new Random(42).NextBytes(payload);

		using var unary = client.UnaryAsync(new P.Frame { Text = "large", Payload = ByteString.CopyFrom(payload) }, new Metadata { { "request-id", "test-id" } }, Deadline());
		var reply = await unary.ResponseAsync;

		Assert.Equal("alpha:large", reply.Text);
		Assert.Equal(payload, reply.Payload.ToByteArray());
		Assert.Equal("test-id", (await unary.ResponseHeadersAsync).GetValue("request-id"));
		Assert.Equal("preserved", (await unary.ResponseHeadersAsync).GetValue("query"));
		Assert.Equal(reply.Instance, unary.GetTrailers().GetValue("instance"));
		Assert.Equal(StatusCode.OK, unary.GetStatus().StatusCode);

		using var server = client.ServerStream(new P.Frame { Text = "item" }, deadline: Deadline());
		var messages = new List<string>();

		while(await server.ResponseStream.MoveNext(CancellationToken.None))
			messages.Add(server.ResponseStream.Current.Text);

		Assert.Equal(new[] { "alpha:item0", "alpha:item1", "alpha:item2" }, messages);

		using var upload = client.ClientStream(deadline: Deadline());
		await upload.RequestStream.WriteAsync(new P.Frame { Text = "one" });
		await upload.RequestStream.WriteAsync(new P.Frame { Text = "two" });
		await upload.RequestStream.CompleteAsync();
		Assert.Equal("alpha:one,two", (await upload.ResponseAsync).Text);

		using var duplex = client.Duplex(deadline: Deadline());
		await duplex.RequestStream.WriteAsync(new P.Frame { Text = "first" });
		Assert.True(await duplex.ResponseStream.MoveNext(CancellationToken.None));
		Assert.Equal("alpha:first", duplex.ResponseStream.Current.Text);
		await duplex.RequestStream.WriteAsync(new P.Frame { Text = "second" });
		Assert.True(await duplex.ResponseStream.MoveNext(CancellationToken.None));
		Assert.Equal("alpha:second", duplex.ResponseStream.Current.Text);
		await duplex.RequestStream.CompleteAsync();
		Assert.False(await duplex.ResponseStream.MoveNext(CancellationToken.None));

		var failure = await Error(StatusCode.PermissionDenied, () => client.UnaryAsync(new P.Frame { Text = "fail" }, deadline: Deadline()).ResponseAsync);
		Assert.Equal("fixture-denied", failure.Status.Detail);
		Assert.Equal("retained", failure.Trailers.GetValue("failure"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CancellationAndDeadlineReachTheService(bool deadline)
	{
		var port = GrpcRegionHost.Port();
		await using var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha)], [(port, "/a"), (port, "/mirror")]);
		var client = new P.Echo.EchoClient(host.Channel(port, "/a"));
		using var cancellation = new CancellationTokenSource();
		using var call = client.UnaryAsync(new P.Frame { Text = "wait" }, deadline: deadline ? DateTime.UtcNow.AddSeconds(2) : Deadline(), cancellationToken: cancellation.Token);
		await host.Probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));

		if(!deadline)
			cancellation.Cancel();

		await Error(deadline ? StatusCode.DeadlineExceeded : StatusCode.Cancelled, () => call.ResponseAsync);
		await host.Probe.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(1, host.Probe.Created["alpha"]);
	}

	[Theory]
	[InlineData("/a", "fixture.Unknown", "Unary")]
	[InlineData("/a", "fixture.Echo", "Unknown")]
	[InlineData("/a", "fixture.echo", "Unary")]
	[InlineData("/a", "fixture.Echo", "unary")]
	[InlineData("/a", "fixture.Echo", "Unary/")]
	[InlineData("/a/child", "fixture.Echo", "Unary")]
	public async Task UnknownCallsNeverEscapeTheirSelectedRegion(string region, string service, string method)
	{
		var port = GrpcRegionHost.Port();
		await using var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha), typeof(S.Child)], [(port, "/a"), (port, "/mirror"), (port, "/a/child")]);
		await Unknown(host.Channel(port, region), service, method);

		Assert.Equal(HttpStatusCode.OK, Assert.Single(host.Probe.Responses));
		Assert.Empty(host.Probe.Created);
		Assert.Equal("child", (await new P.Other.OtherClient(host.Channel(port, "/a/child")).PingAsync(new P.Frame(), deadline: Deadline())).Text);
	}

	[Fact]
	public async Task ReflectionListsOnlyRegionalServicesAndPreservesFileDescriptors()
	{
		var port = GrpcRegionHost.Port();
		await using var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha), typeof(S.Auxiliary), typeof(S.Third), typeof(S.Other)], [(port, "/a"), (port, "/mirror"), (port, "/c")]);

		var first = host.Channel(port, "/a");
		var other = host.Channel(port, "/c");
		var list = await Reflect(first, new ServerReflectionRequest { ListServices = "" });

		Assert.Equal(new[] { "fixture.Auxiliary", "fixture.Echo", "fixture.Third", "grpc.reflection.v1alpha.ServerReflection" }, list.ListServicesResponse.Service.Select(service => service.Name).Order(StringComparer.Ordinal));

		var otherList = await Reflect(other, new ServerReflectionRequest { ListServices = "" });
		Assert.Equal(new[] { "fixture.Other", "grpc.reflection.v1alpha.ServerReflection" }, otherList.ListServicesResponse.Service.Select(service => service.Name).Order(StringComparer.Ordinal));
		Assert.Equal("auxiliary", (await new P.Auxiliary.AuxiliaryClient(first).PingAsync(new P.Frame(), deadline: Deadline())).Text);
		Assert.Equal("third", (await new P.Third.ThirdClient(first).PingAsync(new P.Frame(), deadline: Deadline())).Text);

		var description = await Reflect(first, new ServerReflectionRequest { FileContainingSymbol = "fixture.Echo" });
		var files = description.FileDescriptorResponse.FileDescriptorProto.Select(FileDescriptorProto.Parser.ParseFrom).ToArray();
		var shared = Assert.Single(files, file => file.Name == "regions.proto");

		Assert.Contains("common.proto", shared.Dependency);
		Assert.Contains(files, file => file.Name == "common.proto" && file.MessageType.Any(message => message.Name == "Frame"));
		Assert.Contains(shared.Service, service => service.Name == "Unmounted");

		var unmounted = await Reflect(first, new ServerReflectionRequest { FileContainingSymbol = "fixture.Unmounted" });
		Assert.NotNull(unmounted.FileDescriptorResponse);
		await Unknown(first, "fixture.Unmounted", "Ping");

		var foreign = await Reflect(first, new ServerReflectionRequest { FileContainingSymbol = "fixture.Other" });
		Assert.Equal((int)StatusCode.NotFound, foreign.ErrorResponse.ErrorCode);
		var foreignFile = await Reflect(first, new ServerReflectionRequest { FileByFilename = "other.proto" });
		Assert.Equal((int)StatusCode.NotFound, foreignFile.ErrorResponse.ErrorCode);
	}

	[Fact]
	public async Task RegionalFallbackRunsThroughAuthorization()
	{
		var port = GrpcRegionHost.Port();
		await using var host = await GrpcRegionHost.StartAsync([typeof(S.Alpha)], [(port, "/a"), (port, "/mirror")], authorize: true);
		var channel = host.Channel(port, "/a");

		await Unknown(channel, "fixture.Unknown", "Unary", expected: StatusCode.Unauthenticated);
		await Unknown(channel, "fixture.Echo", "Unknown", expected: StatusCode.Unauthenticated);
		Assert.Empty(host.Probe.Created);

		var headers = new Metadata { { "authorization", "Bearer fixture" } };
		await Unknown(channel, "fixture.Unknown", "Unary", headers);
		await Unknown(channel, "fixture.Echo", "Unknown", headers);
		Assert.Equal("alpha:authorized", (await new P.Echo.EchoClient(channel).UnaryAsync(new P.Frame { Text = "authorized" }, headers, Deadline())).Text);
		Assert.Equal(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK }, host.Probe.Responses);
	}

	[Theory]
	[InlineData("handler")]
	[InlineData("endpoint")]
	[InlineData("configuration")]
	[InlineData("empty")]
	[InlineData("duplicate")]
	[InlineData("path")]
	[InlineData("order")]
	[InlineData("type")]
	[InlineData("global-fallback")]
	[InlineData("service-fallback")]
	[InlineData("service-fallback-late")]
	public async Task InvalidMountsFailBeforeListening(string scenario)
	{
		var port = GrpcRegionHost.Port();
		Type[] services = scenario switch
		{
			"handler" => [typeof(S.MissingHandler)],
			"endpoint" => [typeof(S.MissingEndpoint)],
			"empty" => [],
			"duplicate" => [typeof(S.Alpha), typeof(S.Duplicate)],
			"service-fallback-late" => [typeof(S.Alpha), typeof(S.Beta)],
			"path" => [typeof(S.InvalidPath)],
			"order" => [typeof(S.Ordered)],
			"type" => [typeof(S.NotGrpc)],
			_ => [typeof(S.Alpha)],
		};

		Action<IServiceCollection> configure = scenario switch
		{
			"global-fallback" => collection => collection.Configure<global::Grpc.AspNetCore.Server.GrpcServiceOptions>(options => options.IgnoreUnknownServices = false),
			"service-fallback" => collection => collection.Configure<global::Grpc.AspNetCore.Server.GrpcServiceOptions<S.Alpha>>(options => options.IgnoreUnknownServices = false),
			"service-fallback-late" => collection => collection.Configure<global::Grpc.AspNetCore.Server.GrpcServiceOptions<S.Beta>>(options => options.IgnoreUnknownServices = false),
			_ => null,
		};

		if(scenario == "path")
			await Assert.ThrowsAsync<ArgumentException>(Start);
		else
			await Assert.ThrowsAsync<InvalidOperationException>(Start);

		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
		Assert.Equal(port, ((IPEndPoint)socket.LocalEndPoint).Port);

		async Task Start()
		{
			(int Port, string Path)[] endpoints = scenario switch
			{
				"configuration" => [],
				"service-fallback-late" => [(port, "/a"), (port, "/mirror"), (port, "/b")],
				_ => [(port, "/a"), (port, "/mirror")],
			};
			await using var host = await GrpcRegionHost.StartAsync(services, endpoints, configure: configure);
		}
	}

	private static DateTime Deadline() => DateTime.UtcNow.AddSeconds(10);
	private static async Task<RpcException> Error(StatusCode code, Func<Task> call)
	{
		var error = await Assert.ThrowsAsync<RpcException>(call);
		Assert.Equal(code, error.StatusCode);
		return error;
	}

	private static async Task Unknown(GrpcChannel channel, string service, string method, Metadata headers = null, StatusCode expected = StatusCode.Unimplemented)
	{
		var marshaller = Marshallers.Create<P.Frame>(message => message.ToByteArray(), P.Frame.Parser.ParseFrom);
		using var call = channel.CreateCallInvoker().AsyncUnaryCall(new Method<P.Frame, P.Frame>(MethodType.Unary, service, method, marshaller, marshaller), null, new CallOptions(headers, Deadline()), new P.Frame());
		await Error(expected, () => call.ResponseAsync);
	}

	private static async Task<ServerReflectionResponse> Reflect(GrpcChannel channel, ServerReflectionRequest request)
	{
		var client = new ServerReflection.ServerReflectionClient(channel);
		using var call = client.ServerReflectionInfo(deadline: Deadline());
		await call.RequestStream.WriteAsync(request);
		await call.RequestStream.CompleteAsync();

		Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
		var response = call.ResponseStream.Current;
		Assert.False(await call.ResponseStream.MoveNext(CancellationToken.None));
		return response;
	}
}
