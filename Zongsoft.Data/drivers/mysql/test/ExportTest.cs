using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class ExportTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

}
