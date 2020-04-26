using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Services.Tests
{
	public class ServiceProviderTest
	{
		public interface IFoo
		{
		}

		public interface IBar
		{
		}

		public class Foo : IFoo
		{
		}

		public class Bar : IBar
		{
		}

		[Fact]
		public void Test()
		{
			var services = new ServiceCollection()
				.AddSingleton<IFoo, Foo>();

			var provider = new ServiceProvider(services);

			Assert.NotNull(provider.GetService<IFoo>());
			Assert.Null(provider.GetService<IBar>());

			provider.Services.AddSingleton<IBar, Bar>();

			Assert.NotNull(provider.GetService<IFoo>());
			Assert.NotNull(provider.GetService<IBar>());
		}
	}
}
