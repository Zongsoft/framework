using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Xunit;

namespace Zongsoft.Services.Tests
{
	public class ServiceProviderTest
	{
		private interface IFoo
		{
		}

		private interface IBar
		{
		}

		private class Foo : IFoo
		{
		}

		private class Bar : IBar
		{
		}

		private interface IBaz
		{
		}

		private class Baz : IBaz
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

			provider.Descriptors.AddSingleton<IBar, Bar>();

			Assert.NotNull(provider.GetService<IFoo>());
			Assert.NotNull(provider.GetService<IBar>());

			Assert.Null(provider.GetService<Foo>());
			Assert.Null(provider.GetService<Bar>());
		}

		[Fact]
		public void TestSame()
		{
			var services = new ServiceCollection()
				.AddSingleton<Foo>()
				.AddSingleton<IFoo, Foo>();

			var provider = new ServiceProvider(services);

			Assert.NotNull(provider.GetService<IFoo>());
			Assert.NotNull(provider.GetService<Foo>());
			Assert.NotSame(provider.GetService<IFoo>(), provider.GetService<Foo>());

			provider.Descriptors
				.AddSingleton<Bar>()
				.AddSingleton<IBar>(srv => srv.GetRequiredService<Bar>());

			Assert.NotNull(provider.GetService<IBar>());
			Assert.NotNull(provider.GetService<Bar>());
			Assert.Same(provider.GetService<IBar>(), provider.GetService<Bar>());
		}

		[Fact]
		public void TestScope()
		{
			var services = new ServiceCollection()
				.AddSingleton<IFoo, Foo>()
				.AddScoped<IBar, Bar>()
				.AddScoped<IBaz, Baz>();

			var root = new ServiceProvider(services);

			Assert.NotNull(root.GetService<IFoo>());
			Assert.NotNull(root.GetService<IBar>());
			Assert.NotNull(root.GetService<IBaz>());

			var sub1 = root.CreateScope().ServiceProvider;
			var sub2 = root.CreateScope().ServiceProvider;

			Assert.Same(root.GetService<IFoo>(), sub1.GetService<IFoo>());
			Assert.Same(root.GetService<IFoo>(), sub2.GetService<IFoo>());
			Assert.Same(sub1.GetService<IFoo>(), sub2.GetService<IFoo>());

			Assert.NotSame(root.GetService<IBar>(), sub1.GetService<IBar>());
			Assert.NotSame(root.GetService<IBar>(), sub2.GetService<IBar>());
			Assert.NotSame(sub1.GetService<IBar>(), sub2.GetService<IBar>());
		}
	}
}
