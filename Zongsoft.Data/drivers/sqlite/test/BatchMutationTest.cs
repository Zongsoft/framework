using Xunit;

namespace Zongsoft.Data.SQLite.Tests;

[Collection("Database")]
public class BatchMutationTest(DatabaseFixture database) : Zongsoft.Data.Tests.BatchMutationTestBase(database.Accessor, Global.IsTestingEnabled, true, typeof(Microsoft.Data.Sqlite.SqliteException))
{
}
