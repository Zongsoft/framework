using System;

using Xunit;

using Zongsoft.Externals.OpenXml;
using Zongsoft.Externals.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Tests
{
	public class CellAddressTest
	{
		[Fact]
		public void TestPosition()
		{
			var address = new CellAddress(0, 0);
			Assert.Equal(1, address.Row);
			Assert.Equal(1, address.Column);

			address = new CellAddress(1, 1);
			Assert.Equal(1, address.Row);
			Assert.Equal(1, address.Column);

			address = new CellAddress(2, 2);
			Assert.Equal(2, address.Row);
			Assert.Equal(2, address.Column);

			address = new CellAddress(100, 200);
			Assert.Equal(100, address.Row);
			Assert.Equal(200, address.Column);
		}

		[Fact]
		public void TestToString()
		{
			var address = new CellAddress(1, 1);
			Assert.Equal("A1", address);

			address = new CellAddress(2, 2);
			Assert.Equal("B2", address);

			address = new CellAddress(3, 3);
			Assert.Equal("C3", address);

			address = new CellAddress(1, 24);
			Assert.Equal("X1", address);

			address = new CellAddress(2, 25);
			Assert.Equal("Y2", address);

			address = new CellAddress(3, 26);
			Assert.Equal("Z3", address);

			address = new CellAddress(10, 27);
			Assert.Equal("AA10", address);

			address = new CellAddress(20, 28);
			Assert.Equal("AB20", address);

			address = new CellAddress(30, 29);
			Assert.Equal("AC30", address);
		}
	}
}
