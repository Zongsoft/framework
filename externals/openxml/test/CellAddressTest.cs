using System;

using Xunit;

using Zongsoft.Externals.OpenXml;
using Zongsoft.Externals.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Tests
{
	public class CellAddressTest
	{
		[Fact]
		public void TestToString()
		{
			var address = new CellAddress(1, 26);
			Assert.Equal("A1", address.ToString());
		}
	}
}
