using System;

using Xunit;

namespace Zongsoft.Data.DuckDB.Tests;

[Collection("Database")]
public class ReturningMutationTest(DatabaseFixture database) : Zongsoft.Data.Tests.ReturningMutationTestBase(database.Accessor, Global.IsTestingEnabled, false, true)
{
}
