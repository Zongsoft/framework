using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class DataServiceMutabilityTest
	{
		[Fact]
		public void TestParse()
		{
			var succeed = DataServiceMutability.TryParse("  \t", out var result);
			Assert.True(succeed);
			Assert.Equal(DataServiceMutability.None, result);

			succeed = DataServiceMutability.TryParse("none", out result);
			Assert.True(succeed);
			Assert.Equal(DataServiceMutability.None, result);

			succeed = DataServiceMutability.TryParse(" None ", out result);
			Assert.True(succeed);
			Assert.Equal(DataServiceMutability.None, result);

			succeed = DataServiceMutability.TryParse(" Nothing ", out result);
			Assert.False(succeed);
			succeed = DataServiceMutability.TryParse(" None, Other ", out result);
			Assert.False(succeed);

			succeed = DataServiceMutability.TryParse(" None, Delete ", out result);
			Assert.True(succeed);
			Assert.True(result.Deletable);
			Assert.False(result.Insertable);
			Assert.False(result.Updatable);
			Assert.False(result.Upsertable);

			succeed = DataServiceMutability.TryParse("  insert, , None", out result);
			Assert.True(succeed);
			Assert.False(result.Deletable);
			Assert.True(result.Insertable);
			Assert.False(result.Updatable);
			Assert.False(result.Upsertable);

			succeed = DataServiceMutability.TryParse(" None , UPDATE , None", out result);
			Assert.True(succeed);
			Assert.False(result.Deletable);
			Assert.False(result.Insertable);
			Assert.True(result.Updatable);
			Assert.False(result.Upsertable);

			succeed = DataServiceMutability.TryParse(" None , Upsertable , None", out result);
			Assert.True(succeed);
			Assert.False(result.Deletable);
			Assert.False(result.Insertable);
			Assert.False(result.Updatable);
			Assert.True(result.Upsertable);

			succeed = DataServiceMutability.TryParse(" None , UPDATE , \t, None, Delete ,, INSERTABLE ,Insert,Upsertable", out result);
			Assert.True(succeed);
			Assert.True(result.Deletable);
			Assert.True(result.Insertable);
			Assert.True(result.Updatable);
			Assert.True(result.Upsertable);
		}
	}
}
