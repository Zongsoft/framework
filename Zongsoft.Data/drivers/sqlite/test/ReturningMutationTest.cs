using System;

using Xunit;

namespace Zongsoft.Data.SQLite.Tests;

[Collection("Database")]
public class ReturningMutationTest(DatabaseFixture database) : Zongsoft.Data.Tests.ReturningMutationTestBase(database.Accessor, Global.IsTestingEnabled, false, true)
{
}
