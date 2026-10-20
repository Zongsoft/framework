using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Xunit;

namespace Zongsoft.Plugins.Web.IntegrationTests;

internal static partial class EndpointScenarios
{
	private static async Task TransportFactoriesPreserveSelectionOwnershipAndCarrier(EndpointTestScope scope, string registration)
	{
		var state = new FactoryState();
		var application = await Builder(scope, builder =>
		{
			ConfigureEndpoint(builder.Configuration, 18081);
			builder.Configuration["Kestrel:Endpoints:quic:Url"] = "https://127.0.0.1:18082/quic";
			builder.Configuration["Kestrel:Endpoints:quic:Protocols"] = "http3";
			builder.Services.RemoveAll<IConnectionListenerFactory>();
			builder.Services.RemoveAll<IMultiplexedConnectionListenerFactory>();
			builder.Services.AddSingleton(state);

			foreach(var contract in new[] { typeof(IConnectionListenerFactory), typeof(IMultiplexedConnectionListenerFactory) })
			{
				builder.Services.Add(registration switch
				{
					"instance" => ServiceDescriptor.Singleton(contract, new ProbeFactory(state)),
					"factory" => ServiceDescriptor.Singleton(contract, _ => new ProbeFactory(state)),
					"type" => ServiceDescriptor.Singleton(contract, typeof(ProbeFactory)),
					_ => throw new ArgumentOutOfRangeException(nameof(registration)),
				});
			}
		}).BuildAsync();

		try
		{
			var tcp = Assert.Single(application.Services.GetServices<IConnectionListenerFactory>());
			var quic = Assert.Single(application.Services.GetServices<IMultiplexedConnectionListenerFactory>());
			Assert.Equal(2, state.Factories.Count);
			var tcpAddress = new IPEndPoint(IPAddress.Loopback, 18081);
			var quicAddress = new IPEndPoint(IPAddress.Loopback, 18082);

			foreach(var factory in new object[] { tcp, quic })
			{
				var selector = Assert.IsAssignableFrom<IConnectionListenerFactorySelector>(factory);
				Assert.True(selector.CanBind(tcpAddress));
				Assert.False(selector.CanBind(new IPEndPoint(IPAddress.Any, 18081)));
			}

			Assert.All(state.Factories, factory => Assert.Equal(2, factory.SelectionCalls));

			Assert.IsType<ProbeFactory>(tcp);
			Assert.IsType<ProbeFactory>(quic);

			using var cancellation = new CancellationTokenSource();
			var features = new FeatureCollection();
			var tcpError = await Assert.ThrowsAsync<InvalidOperationException>(() => tcp.BindAsync(tcpAddress, cancellation.Token).AsTask());
			var quicError = await Assert.ThrowsAsync<InvalidOperationException>(() => quic.BindAsync(quicAddress, features, cancellation.Token).AsTask());
			var tcpFactory = Assert.Single(state.Factories, factory => !factory.Multiplexed);
			var quicFactory = Assert.Single(state.Factories, factory => factory.Multiplexed);
			Assert.Same(tcpFactory.Error, tcpError);
			Assert.Same(quicFactory.Error, quicError);
			Assert.Equal(tcpAddress, tcpFactory.Endpoint);
			Assert.Equal(quicAddress, quicFactory.Endpoint);
			Assert.Same(features, quicFactory.Features);
			Assert.All(state.Factories, factory =>
			{
				Assert.Equal(1, factory.BindCalls);
				Assert.Equal(cancellation.Token, factory.Cancellation);
				Assert.Equal(0, factory.Disposals);
			});
		}
		finally
		{
			await application.DisposeAsync();
		}

		Assert.All(state.Factories, factory => Assert.Equal(registration == "instance" ? 0 : 1, factory.Disposals));
	}

	private sealed class FactoryState
	{
		public List<ProbeFactory> Factories { get; } = [];
	}

	private sealed class ProbeFactory : IConnectionListenerFactory, IMultiplexedConnectionListenerFactory, IConnectionListenerFactorySelector, IDisposable, IAsyncDisposable
	{
		public ProbeFactory(FactoryState state) => state.Factories.Add(this);
		public InvalidOperationException Error { get; } = new("The fixture does not create a physical transport.");
		public int SelectionCalls { get; private set; }
		public int BindCalls { get; private set; }
		public int Disposals { get; private set; }
		public bool Multiplexed { get; private set; }
		public EndPoint Endpoint { get; private set; }
		public IFeatureCollection Features { get; private set; }
		public CancellationToken Cancellation { get; private set; }

		public bool CanBind(EndPoint endpoint)
		{
			this.SelectionCalls++;
			return endpoint is IPEndPoint address && address.Address.Equals(IPAddress.Loopback);
		}

		public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
		{
			this.Record(endpoint, cancellationToken);
			return ValueTask.FromException<IConnectionListener>(this.Error);
		}

		public ValueTask<IMultiplexedConnectionListener> BindAsync(EndPoint endpoint, IFeatureCollection features = null, CancellationToken cancellationToken = default)
		{
			this.Multiplexed = true;
			this.Features = features;
			this.Record(endpoint, cancellationToken);
			return ValueTask.FromException<IMultiplexedConnectionListener>(this.Error);
		}

		public void Dispose() => this.Disposals++;
		public ValueTask DisposeAsync()
		{
			this.Disposals++;
			return ValueTask.CompletedTask;
		}

		private void Record(EndPoint endpoint, CancellationToken cancellation)
		{
			this.BindCalls++;
			this.Endpoint = endpoint;
			this.Cancellation = cancellation;
		}
	}
}
