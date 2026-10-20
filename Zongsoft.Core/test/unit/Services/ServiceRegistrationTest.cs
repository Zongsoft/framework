using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Configuration.Options;

using Xunit;

namespace Zongsoft.Services.Tests;

public class ServiceRegistrationTest
{
	[Fact]
	public void RegisterDiscoversNonPublicRegistrationsAndSkipsUnconstructibleTypes()
	{
		using var configuration = Configuration();
		var services = new ServiceCollection();
		services.Register(typeof(ServiceRegistrationTest).Assembly, configuration);
		using var provider = services.BuildServiceProvider();

		Assert.Equal(new[]
		{
			"DeepRegistration", "InternalServiceRegistration", "PrivateRegistration", "StaticFieldRegistration", "StaticPropertyRegistration",
		}, provider.GetServices<IServiceRegistration>().Select(registration => registration.GetType().Name).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void RegisterKeepsNonPublicServicesAndOptionsHidden()
	{
		using var configuration = Configuration("hidden");
		var services = new ServiceCollection();
		services.Register(typeof(ServiceRegistrationTest).Assembly, configuration);

		Assert.DoesNotContain(services, service => service.ServiceType == typeof(IHiddenService));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(PrivateService));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(InternalService));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(Container.PublicService));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(IConfigureOptions<HiddenOptions>));
	}

	[Fact]
	public void RegisterPreservesPublicServicesAndOptions()
	{
		using var configuration = Configuration("public");
		var services = new ServiceCollection();
		services.Register(typeof(ServiceRegistrationTest).Assembly, configuration);
		using var provider = services.BuildServiceProvider();

		Assert.Same(provider.GetRequiredService<PublicService>(), provider.GetRequiredService<IPublicService>());
		Assert.Same(StaticService.Instance, provider.GetRequiredService<IStaticService>());
		Assert.Same(StaticService.Instance, provider.GetRequiredService<StaticService>());
		Assert.Equal("configured", provider.GetRequiredService<IOptions<PublicOptions>>().Value.Value);
	}

	[Fact]
	public void RegisterUsesStaticRegistrationInstances()
	{
		using var configuration = Configuration();
		var services = new ServiceCollection();
		services.Register(typeof(ServiceRegistrationTest).Assembly, configuration);
		using var provider = services.BuildServiceProvider();
		var registrations = provider.GetServices<IServiceRegistration>().ToArray();

		Assert.Same(StaticFieldRegistration.Instance, Assert.Single(registrations.OfType<StaticFieldRegistration>()));
		Assert.Same(StaticPropertyRegistration.Instance, Assert.Single(registrations.OfType<StaticPropertyRegistration>()));
	}

	[Fact]
	public void RegisterPropagatesRegistrationExceptions()
	{
		using var configuration = Configuration("throw");
		var services = new ServiceCollection();
		var error = Assert.Throws<InvalidOperationException>(() => services.Register(typeof(ServiceRegistrationTest).Assembly, configuration));
		Assert.Same(ThrowingRegistration.Error, error);
	}

	private static ConfigurationRoot Configuration(string mode = "enabled") =>
		(ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["RegistrationFixture:Mode"] = mode,
			["RegistrationFixture:Options:Value"] = "configured",
		}).Build();

	private static void Add(IServiceCollection services, IConfiguration configuration, IServiceRegistration registration)
	{
		if(configuration?["RegistrationFixture:Mode"] == "enabled")
			services.AddSingleton(registration);
	}

	private sealed class PrivateRegistration : IServiceRegistration
	{
		private PrivateRegistration() { }
		public void Register(IServiceCollection services, IConfiguration configuration) => Add(services, configuration, this);
	}

	private static class Container
	{
		private static class Nested
		{
			private sealed class DeepRegistration : IServiceRegistration
			{
				public DeepRegistration() { }
				public void Register(IServiceCollection services, IConfiguration configuration) => Add(services, configuration, this);
			}
		}

		[Service<IHiddenService>]
		public sealed class PublicService : IHiddenService
		{
			[Options("RegistrationFixture:Options")]
			public HiddenOptions Options { get; set; }
		}
	}

	private sealed class StaticFieldRegistration : IServiceRegistration
	{
		public static readonly StaticFieldRegistration Instance = new();
		private StaticFieldRegistration() { }
		public void Register(IServiceCollection services, IConfiguration configuration) => Add(services, configuration, this);
	}

	private sealed class StaticPropertyRegistration : IServiceRegistration
	{
		public static StaticPropertyRegistration Instance { get; } = new();
		private StaticPropertyRegistration() { }
		public void Register(IServiceCollection services, IConfiguration configuration) => Add(services, configuration, this);
	}

	private abstract class AbstractRegistration : IServiceRegistration
	{
		public abstract void Register(IServiceCollection services, IConfiguration configuration);
	}

	private sealed class GenericRegistration<T> : IServiceRegistration
	{
		public void Register(IServiceCollection services, IConfiguration configuration) => throw new InvalidOperationException("An open generic registration must not run.");
	}

	private sealed class GenericContainer<T>
	{
		private sealed class Registration : IServiceRegistration
		{
			public void Register(IServiceCollection services, IConfiguration configuration) => throw new InvalidOperationException("A registration with an open declaring type must not run.");
		}
	}

	private sealed class ThrowingRegistration : IServiceRegistration
	{
		public static InvalidOperationException Error { get; } = new("Registration failed.");

		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			if(configuration?["RegistrationFixture:Mode"] == "throw")
				throw Error;
		}
	}

	internal interface IHiddenService { }

	[Service<IHiddenService>]
	private sealed class PrivateService : IHiddenService
	{
		[Options("RegistrationFixture:Options")]
		public HiddenOptions Options { get; set; }
	}

	[Service<IHiddenService>]
	internal sealed class InternalService : IHiddenService
	{
		[Options("RegistrationFixture:Options")]
		public HiddenOptions Options { get; set; }
	}

	public interface IPublicService { }
	public interface IStaticService { }

	[Service<IPublicService>]
	public sealed class PublicService : IPublicService
	{
		[Options("RegistrationFixture:Options")]
		public PublicOptions Options { get; set; }
	}

	[Service<IStaticService>(Members = nameof(Instance))]
	public sealed class StaticService : IStaticService
	{
		public static StaticService Instance { get; } = new();
		private StaticService() { }
	}

	public sealed class PublicOptions
	{
		public string Value { get; set; }
	}

	internal sealed class HiddenOptions
	{
		public string Value { get; set; }
	}
}

internal sealed class InternalServiceRegistration : IServiceRegistration
{
	private InternalServiceRegistration() { }

	public void Register(IServiceCollection services, IConfiguration configuration)
	{
		if(configuration?["RegistrationFixture:Mode"] == "enabled")
			services.AddSingleton<IServiceRegistration>(this);
	}
}
