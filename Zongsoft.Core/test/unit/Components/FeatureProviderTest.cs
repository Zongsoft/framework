using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class FeatureProviderTest
{
	[Fact]
	public void CapturesDistinctSourcesAndFeaturesAcrossNestedCompositions()
	{
		var shared = new Feature();
		var other = new Feature();
		var first = new Provider(shared, shared);
		var second = new Provider(shared, other);
		var nested = new FeatureProvider(first, null, second);
		var provider = new FeatureProvider(first, nested, second, nested);
		nested.Providers.Add(provider);

		var features = provider.GetFeatures();
		first.Features = [];
		second.Features = [];

		Assert.Equal([shared, other], features);
		Assert.Equal([shared, other], features);
		Assert.Equal(1, first.Reads);
		Assert.Equal(1, second.Reads);
		Assert.Empty(provider.GetFeatures());
		Assert.Equal(2, first.Reads);
		Assert.Equal(2, second.Reads);
	}

	[Fact]
	public void SnapshotsNestedSourcesBeforeAnyCaptureCallback()
	{
		var original = new Provider(new Feature());
		var added = new Provider(new Feature());
		var nested = new FeatureProvider(original);
		var mutator = new Provider
		{
			Capturing = () =>
			{
				nested.Providers.Clear();
				nested.Providers.Add(added);
			},
		};
		var provider = new FeatureProvider(mutator, nested);

		Assert.Same(original.Features[0], Assert.Single(provider.GetFeatures()));
		Assert.Equal(1, original.Reads);
		Assert.Equal(0, added.Reads);
		Assert.Same(added.Features[0], Assert.Single(provider.GetFeatures()));
		Assert.Equal(1, original.Reads);
		Assert.Equal(1, added.Reads);
	}

	[Fact]
	public void CaptureFailuresPropagateAndNextCaptureCanSucceed()
	{
		var failed = new InvalidOperationException();
		var source = new Provider { Capturing = () => throw failed };
		var provider = new FeatureProvider(source);

		Assert.Same(failed, Assert.Throws<InvalidOperationException>(() => provider.GetFeatures()));
		provider.Providers.Remove(source);
		var replacement = new Provider(new Feature());
		provider.Providers.Add(replacement);

		Assert.Same(replacement.Features[0], Assert.Single(provider.GetFeatures()));
	}

	private sealed class Feature : IFeature;

	private sealed class Provider(params IFeature[] features) : IFeatureProvider
	{
		public IFeature[] Features { get; set; } = features;
		public Action Capturing { get; init; }
		public int Reads { get; private set; }
		public IEnumerable<IFeature> GetFeatures()
		{
			this.Reads++;
			this.Capturing?.Invoke();
			return this.Features;
		}
	}
}
