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

		[Fact]
		public void TestParse()
		{
			CellAddress address;

			Assert.True(CellAddress.TryParse("A1", out address));
			Assert.Equal(1, address.Column);
			Assert.Equal(1, address.Row);
			Assert.Equal(address, CellAddress.Parse("a1"));

			Assert.True(CellAddress.TryParse("B12 ", out address));
			Assert.Equal(2, address.Column);
			Assert.Equal(12, address.Row);
			Assert.Equal(address, CellAddress.Parse("b12"));

			Assert.True(CellAddress.TryParse(" C123", out address));
			Assert.Equal(3, address.Column);
			Assert.Equal(123, address.Row);
			Assert.Equal(address, CellAddress.Parse("c123"));

			Assert.True(CellAddress.TryParse(" x1 ", out address));
			Assert.Equal(24, address.Column);
			Assert.Equal(1, address.Row);
			Assert.Equal(address, CellAddress.Parse("X1"));

			Assert.True(CellAddress.TryParse(" y12  ", out address));
			Assert.Equal(25, address.Column);
			Assert.Equal(12, address.Row);
			Assert.Equal(address, CellAddress.Parse("Y12"));

			Assert.True(CellAddress.TryParse("   z123", out address));
			Assert.Equal(26, address.Column);
			Assert.Equal(123, address.Row);
			Assert.Equal(address, CellAddress.Parse("Z123"));

			Assert.True(CellAddress.TryParse("AA1", out address));
			Assert.Equal(27, address.Column);
			Assert.Equal(1, address.Row);
			Assert.Equal(address, CellAddress.Parse("aa1"));

			Assert.True(CellAddress.TryParse("AB12", out address));
			Assert.Equal(28, address.Column);
			Assert.Equal(12, address.Row);
			Assert.Equal(address, CellAddress.Parse("Ab12"));

			Assert.True(CellAddress.TryParse("AC123", out address));
			Assert.Equal(29, address.Column);
			Assert.Equal(123, address.Row);
			Assert.Equal(address, CellAddress.Parse("ac123"));

			Assert.True(CellAddress.TryParse("ABC1", out address));
			Assert.Equal(0 + (int)Math.Pow(26, 2) + 1 + (int)Math.Pow(26, 1) + 2 + (int)Math.Pow(26, 0), address.Column);
			Assert.Equal(1, address.Row);
			Assert.Equal(address, CellAddress.Parse("AbC1"));

			Assert.True(CellAddress.TryParse("xyz12", out address));
			Assert.Equal(23 + (int)Math.Pow(26, 2) + 24 + (int)Math.Pow(26, 1) + 25 + (int)Math.Pow(26, 0), address.Column);
			Assert.Equal(12, address.Row);
			Assert.Equal(address, CellAddress.Parse("xYz12"));
		}
	}
}
