using System;
using System.Linq;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;

namespace Zongsoft.Data.Tests;

public abstract class DriverFeatureTestBase(IDataDriver driver, bool updateOlder, bool multitable, bool insertReturning = true, bool connectionSharing = false)
{
	[Fact]
	public void ReturningDeclaresOperationAndValueKinds()
	{
		var feature = Assert.IsAssignableFrom<IReturningFeature>(driver.Features.Get<IReturningFeature>());

		Assert.True(feature.Supports(DataAccessMethod.Update, ReturningKind.Newer));
		Assert.Equal(updateOlder, feature.Supports(DataAccessMethod.Update, ReturningKind.Older));
		Assert.True(feature.Supports(DataAccessMethod.Delete, ReturningKind.Older));
		Assert.False(feature.Supports(DataAccessMethod.Delete, ReturningKind.Newer));
		Assert.Equal(insertReturning, feature.Supports(DataAccessMethod.Insert, ReturningKind.Newer));
		Assert.False(feature.Supports(DataAccessMethod.Insert, ReturningKind.Older));
		Assert.Equal(insertReturning, feature.Supports(DataAccessMethod.Upsert, ReturningKind.Newer));
		Assert.Equal(insertReturning && updateOlder, feature.Supports(DataAccessMethod.Upsert, ReturningKind.Older));
		Assert.False(feature.Supports(DataAccessMethod.Update, (ReturningKind)999));
		Assert.Same(DataExecutor.Instance, driver.Executor);
	}

	[Fact]
	public void RegisteredFeaturesDescribeDriverCapabilities()
	{
		var feature = driver.Features.Get<IMultitableFeature>();

		Assert.Equal(multitable, feature?.CanUpdate ?? false);
		Assert.Equal(multitable, feature?.CanDelete ?? false);
		Assert.False(driver.Features.Get<ITransactionFeature>()?.Suppressed ?? false);
		Assert.Equal(connectionSharing, driver.Features.Get<IConnectionSharingFeature>() != null);
		Assert.Same(driver.Features.Get<IReturningFeature>(),
			Assert.Single(driver.Features, registration => registration.Key == typeof(IReturningFeature)).Value);
		Assert.Equal(driver.Features.Count, driver.Features.ToList().Count);
	}

	[Theory]
	[InlineData("Server=localhost", true)]
	[InlineData("Server=localhost;MultipleActiveResultSets=True", true)]
	[InlineData("Server=localhost;MultipleActiveResultSets=False", false)]
	public void ConnectionSharingDependsOnDriverAndSettings(string settings, bool enabled)
	{
		var source = new DataSource("ConnectionSharing", settings);
		Assert.Empty(source.Features);

		source.Features.Set<IConnectionSharingFeature>(driver.Features.Get<IConnectionSharingFeature>());

		Assert.Equal(connectionSharing && enabled,
			source.Features.Get<IConnectionSharingFeature>()?.CanShare(source) == true);

		source.Features.Set<IConnectionSharingFeature>(ConnectionSharingFeature.Instance);

		Assert.False(source.Features.Get<IConnectionSharingFeature>().CanShare(source));
		Assert.Equal(connectionSharing && enabled, driver.Features.Get<IConnectionSharingFeature>()?.CanShare(source) == true);
	}

	[Fact]
	public void DataSourceOverrideDoesNotChangeDriver()
	{
		var features = new FeatureCollection(driver.Features);
		var original = driver.Features.Get<IReturningFeature>();
		var replacement = new ReturningFeature(false);
		features.Set<IReturningFeature>(replacement);

		Assert.Same(replacement, features.Get<IReturningFeature>());
		Assert.Same(original, driver.Features.Get<IReturningFeature>());

		features.Set<IReturningFeature>(null);

		Assert.Same(original, features.Get<IReturningFeature>());
	}
}
