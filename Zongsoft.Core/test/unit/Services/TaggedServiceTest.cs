using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Services.Tests;

public sealed class TaggedServiceTest
{
	[Fact]
	public void Register_SameServiceAndTagAcrossAttributes_MergesAllContracts()
	{
		using var provider = CreateProvider();

		var first = Assert.Single(provider.Provider.Resolves<ITaggedContractA>(TaggedService.Tag.ToLowerInvariant()));
		var second = Assert.Single(provider.Provider.Resolves<ITaggedContractB>(TaggedService.Tag.ToUpperInvariant()));

		Assert.Same(first, second);
	}

	[Fact]
	public async Task TaggedServices_ConcurrentProviderCreationAndLookup_HasNoRaceOrLoss()
	{
		var tasks = Enumerable.Range(0, 12).Select(_ => Task.Run(() =>
		{
			for(var index = 0; index < 8; index++)
			{
				using var provider = CreateProvider();
				var first = Assert.Single(provider.Provider.Resolves<ITaggedContractA>(TaggedService.Tag));
				var second = Assert.Single(provider.Provider.Resolves<ITaggedContractB>(TaggedService.Tag));
				Assert.Same(first, second);
			}
		}));

		await Task.WhenAll(tasks);
	}

	[Fact]
	public void TaggedServices_DisposedProvider_IsCollectible()
	{
		var reference = CreateWeakProviderReference();

		for(var index = 0; index < 10 && reference.IsAlive; index++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		Assert.False(reference.IsAlive, "The tagged service table retained a disposed provider.");
	}

	private static ProviderScope CreateProvider()
	{
		var services = new ServiceCollection();
		services.Register(typeof(TaggedServiceTest).Assembly, null);
		return new ProviderScope(new ServiceProviderFactory().CreateServiceProvider(services));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateWeakProviderReference()
	{
		var scope = CreateProvider();
		Assert.Single(scope.Provider.Resolves<ITaggedContractA>(TaggedService.Tag));
		var reference = new WeakReference(scope.Provider);
		scope.Dispose();
		return reference;
	}

	private sealed class ProviderScope(IServiceProvider provider) : IDisposable
	{
		public IServiceProvider Provider { get; } = provider;
		public void Dispose() => (this.Provider as IDisposable)?.Dispose();

	}
}

public interface ITaggedContractA { }
public interface ITaggedContractB { }

[Service<ITaggedContractA>(Tags = Tag)]
[Service<ITaggedContractB>(Tags = Tag)]
public sealed class TaggedService : ITaggedContractA, ITaggedContractB
{
	public const string Tag = "P0-Merged-Contracts";
}
