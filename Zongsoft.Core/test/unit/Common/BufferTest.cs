using System;

using Xunit;

namespace Zongsoft.Common.Tests;

public class BufferTest
{
	[Fact]
	public void Lease_Array_CopiesValidPrefixAndDoesNotObserveCallerMutation()
	{
		var source = new[] { 1, 2, 3, 4 };
		using var owner = source.Lease(3);

		source[0] = 10;
		source[3] = 40;

		Assert.Equal(3, owner.Memory.Length);
		Assert.Equal([1, 2, 3], owner.Memory.ToArray());
	}

	[Fact]
	public void Lease_Array_OwnerMutationDoesNotChangeCallerArray()
	{
		var source = new[] { "alpha", "beta" };
		using var owner = source.Lease();

		owner.Memory.Span[0] = "changed";

		Assert.Equal(["alpha", "beta"], source);
		Assert.Equal("changed", owner.Memory.Span[0]);
	}

	[Fact]
	public void Lease_Array_DisposeIsIdempotentAndMakesOwnerInaccessible()
	{
		var source = new byte[] { 1, 2, 3 };
		var owner = source.Lease();

		owner.Dispose();
		owner.Dispose();

		Assert.Equal([1, 2, 3], source);
		Assert.Throws<ObjectDisposedException>(() => _ = owner.Memory);
	}

	[Fact]
	public void Lease_NullAndZeroLength_ReturnsNoOwnedBuffer()
	{
		byte[] missing = null;
		var source = new byte[] { 1, 2, 3 };

		Assert.Null(missing.Lease());

		using var owner = source.Lease(0);
		Assert.True(owner.Memory.IsEmpty);
		source[0] = 9;
		Assert.True(owner.Memory.IsEmpty);
	}
}
