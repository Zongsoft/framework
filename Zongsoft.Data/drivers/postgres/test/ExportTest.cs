using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.PostgreSql.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class ExportTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

}
