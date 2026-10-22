using Xunit;

namespace Zongsoft.Data.DuckDB.Tests;

[Collection("Database")]
public class BatchMutationTest(DatabaseFixture database) : Zongsoft.Data.Tests.BatchMutationTestBase(database.Accessor, Global.IsTestingEnabled, true, typeof(global::DuckDB.NET.Data.DuckDBException))
{
}
