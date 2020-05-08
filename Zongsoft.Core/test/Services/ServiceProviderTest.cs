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
			IFoo Foo { get; }
		}

		private class Foo : IFoo
		{
		}

		private class Bar : IBar
		{
			public Bar(IFoo foo) => this.Foo = foo;

			public IFoo Foo { get; }
		}

		private interface IBaz
		{
			IFoo Foo { get; set; }
		}

		private class Baz : IBaz
		{
			[ServiceDependency]
			public IFoo Foo { get; set; }
		}

		[Fact]
		public void Test()
		{
			var services = new ServiceCollection()
				.AddSingleton<IFoo, Foo>();

			var provider = new ServiceProvider(services);

			Assert.NotNull(provider.GetService<IFoo>());
			Assert.Null(provider.GetService<IBar>());
		}

		[Fact]
		public void TestSame()
		{
			var services = new ServiceCollection()
				.AddSingleton<Foo>()
				.AddSingleton<IFoo, Foo>();

			var provider = new ServiceProvider(services);

			var foo1 = provider.GetService<IFoo>();
			var foo2 = provider.GetService<Foo>();

			Assert.NotNull(foo1);
			Assert.NotNull(foo2);
			Assert.NotSame(foo1, foo2);
			Assert.Same(foo1, provider.GetService<IFoo>());
			Assert.NotSame(foo1, provider.GetService<Foo>());
			Assert.NotSame(foo2, provider.GetService<IFoo>());
			Assert.Same(foo2, provider.GetService<Foo>());
			Assert.NotSame(provider.GetService<IFoo>(), provider.GetService<Foo>());
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
