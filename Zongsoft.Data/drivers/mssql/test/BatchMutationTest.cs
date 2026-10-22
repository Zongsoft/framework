using Xunit;

namespace Zongsoft.Data.MsSql.Tests;

[Collection("Database")]
public class BatchMutationTest(DatabaseFixture database) : Zongsoft.Data.Tests.BatchMutationTestBase(database.Accessor, Global.IsTestingEnabled, true, typeof(DataConflictException))
{
}
