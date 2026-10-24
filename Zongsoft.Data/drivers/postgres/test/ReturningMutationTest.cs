using System;

using Xunit;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class ReturningMutationTest(DatabaseFixture database) : Zongsoft.Data.Tests.ReturningMutationTestBase(database.Accessor, Global.IsTestingEnabled, true, true)
{
}
