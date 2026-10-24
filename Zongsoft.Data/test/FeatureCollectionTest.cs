using System;
using System.Linq;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;

namespace Zongsoft.Data.Tests;

public class FeatureCollectionTest
{
	[Fact]
	public void RegistrationUsesExactContractAndRestoresParent()
	{
		var parent = new FeatureCollection();
		var original = new MultitableFeature(true, true);
		parent.Set<IMultitableFeature>(original);
		var child = new FeatureCollection(parent);

		Assert.Same(original, child.Get<IMultitableFeature>());
		Assert.Null(child.Get<MultitableFeature>());
		Assert.Empty(child);

		var replacement = new MultitableFeature(true, false);
		child.Set<IMultitableFeature>(replacement);

		Assert.Same(replacement, child.Get<IMultitableFeature>());
		Assert.Same(original, parent.Get<IMultitableFeature>());
		Assert.False(child.Get<IMultitableFeature>().CanUpdate);
		Assert.True(child.Get<IMultitableFeature>().CanDelete);

		child.Set<IMultitableFeature>(null);

		Assert.Same(original, child.Get<IMultitableFeature>());
		Assert.Empty(child);
	}

	[Fact]
	public void SetReplacesTheSameContract()
	{
		var features = new FeatureCollection();
		features.Set<IReturningFeature>(ReturningFeature.Instance);
		var replacement = new ReturningFeature(false);
		features.Set<IReturningFeature>(replacement);

		Assert.Same(replacement, features.Get<IReturningFeature>());
		var registration = Assert.Single(features);
		Assert.Equal(typeof(IReturningFeature), registration.Key);
		Assert.Same(replacement, registration.Value);
	}

	[Fact]
	public void SettingNullRemovesRegistration()
	{
		var features = new FeatureCollection();
		Assert.Null(features.Get<IReturningFeature>());

		features.Set<IReturningFeature>(ReturningFeature.Instance);

		Assert.Same(ReturningFeature.Instance, features.Get<IReturningFeature>());
		Assert.Single(features);

		features.Set<IReturningFeature>(null);

		Assert.Null(features.Get<IReturningFeature>());
		Assert.Empty(features);
	}

	[Fact]
	public void TransactionFeatureCanOverrideInheritedSuppression()
	{
		var parent = new FeatureCollection();
		parent.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));
		var features = new FeatureCollection(parent);
		Assert.True(features.Get<ITransactionFeature>().Suppressed);

		features.Set<ITransactionFeature>(TransactionFeature.Instance);

		Assert.False(features.Get<ITransactionFeature>().Suppressed);
		Assert.True(parent.Get<ITransactionFeature>().Suppressed);

		features.Set<ITransactionFeature>(null);

		Assert.True(features.Get<ITransactionFeature>().Suppressed);
	}

	[Fact]
	public void RemovingLocalRegistrationsKeepsInheritedFeatures()
	{
		var parent = new FeatureCollection();
		parent.Set<IReturningFeature>(ReturningFeature.Instance);
		parent.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));
		var features = new FeatureCollection(parent);
		features.Set<IReturningFeature>(new ReturningFeature(false));
		features.Set<IMultitableFeature>(new MultitableFeature(true, true));
		features.Set<IReturningFeature>(null);
		features.Set<IMultitableFeature>(null);
		features.Set<ITransactionFeature>(null);

		Assert.Same(ReturningFeature.Instance, features.Get<IReturningFeature>());
		Assert.Same(parent.Get<ITransactionFeature>(), features.Get<ITransactionFeature>());
		Assert.Null(features.Get<IMultitableFeature>());
		Assert.Empty(features);
		Assert.Equal(2, parent.Count);
	}

	[Fact]
	public void EnumerationContainsOnlyLocalContractRegistrations()
	{
		var parent = new FeatureCollection();
		parent.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));
		var features = new FeatureCollection(parent);
		features.Set<IMultitableFeature>(new MultitableFeature(true, true));
		features.Set<IReturningFeature>(ReturningFeature.Instance);
		var registrations = features.ToDictionary(item => item.Key, item => item.Value);

		Assert.Equal(2, features.Count);
		Assert.Equal(features.Count, registrations.Count);
		Assert.False(registrations.ContainsKey(typeof(ITransactionFeature)));
		Assert.Same(ReturningFeature.Instance, registrations[typeof(IReturningFeature)]);
		Assert.Same(features.Get<IMultitableFeature>(), registrations[typeof(IMultitableFeature)]);
	}
}
