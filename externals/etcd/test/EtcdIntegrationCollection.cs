using Xunit;

namespace Zongsoft.Externals.Etcd.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EtcdIntegrationCollection
{
	public const string Name = "Etcd integration";
}
