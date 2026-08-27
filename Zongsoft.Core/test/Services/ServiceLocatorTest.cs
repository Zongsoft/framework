using System;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Services.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ServiceLocatorCollection
{
	public const string Name = nameof(ServiceLocatorCollection);
}

[Collection(ServiceLocatorCollection.Name)]
public sealed class ServiceLocatorTest
{
	private const string DirectServiceName = "service-locator-direct";
	private const string WrongDirectServiceName = "service-locator-wrong-direct";
	private const string PrimaryContainerName = "service-locator-primary-container";
	private const string WrongContainerName = "service-locator-wrong-container";

	[Fact]
	public void Locate_EmptyQualifiedName_GenericAndRuntimeOverloadsReturnSameService()
	{
		var expected = new NamedService("default", "default");
		using var services = CreateServices(collection => collection.AddSingleton<ITestService>(expected));
		using var application = new ApplicationScope(services.Provider);

		Assert.Same(expected, services.Provider.Locate<ITestService>(null));
		Assert.Same(expected, services.Provider.Locate(string.Empty, typeof(ITestService)));
		Assert.Same(expected, ServiceLocator.Locate<ITestService>(" \t"));
		Assert.Same(expected, ServiceLocator.Locate(null, typeof(ITestService)));
	}

	[Fact]
	public void Locate_Name_UsesDirectRegistrationBeforeOtherStrategies()
	{
		var matched = new NamedService(DirectServiceName, "matched");
		var fallback = new NamedProvider(new NamedService(DirectServiceName, "fallback"));
		using var services = CreateServices(collection =>
		{
			collection.AddSingleton<ITestService>(matched);
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(fallback);
		}, registerAttributedServices: true);

		var expected = services.Provider.Resolve<DirectService>();

		Assert.NotNull(expected);
		Assert.Same(expected, services.Provider.Locate<ITestService>(DirectServiceName));
		Assert.Same(expected, services.Provider.Locate(DirectServiceName, typeof(ITestService)));
		Assert.Equal(0, fallback.CallCount);
	}

	[Fact]
	public void Locate_Name_FindsMatchingServiceBeforeProviderFallback()
	{
		const string name = "service-locator-matched";
		var expected = new NamedService(name, "matched");
		var fallback = new NamedProvider(new NamedService(name, "fallback"));
		using var services = CreateServices(collection =>
		{
			collection.AddSingleton<ITestService>(expected);
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(fallback);
		});

		Assert.Same(expected, services.Provider.Locate<ITestService>(name));
		Assert.Same(expected, services.Provider.Locate(name, typeof(ITestService)));
		Assert.Equal(0, fallback.CallCount);
	}

	[Fact]
	public void Locate_Name_IgnoresWrongTypedDirectRegistrationAndFindsTypedService()
	{
		var expected = new NamedService(WrongDirectServiceName, "matched");
		using var services = CreateServices(collection => collection.AddSingleton<ITestService>(expected), registerAttributedServices: true);

		Assert.IsType<WrongDirectService>(services.Provider.Resolve(WrongDirectServiceName));
		Assert.Same(expected, services.Provider.Locate<ITestService>(WrongDirectServiceName));
		Assert.Same(expected, services.Provider.Locate(WrongDirectServiceName, typeof(ITestService)));
	}

	[Fact]
	public void Locate_Name_FallsBackToCovariantServiceProvider()
	{
		const string name = "service-locator-provided";
		var expected = new NamedService(name, "provided");
		var provider = new NamedProvider(expected);
		using var services = CreateServices(collection =>
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(provider));

		Assert.Same(expected, services.Provider.Locate<ITestService>(name));
		Assert.Same(expected, services.Provider.Locate(name, typeof(ITestService)));
		Assert.Equal(2, provider.CallCount);
	}

	[Fact]
	public void Locate_QualifiedName_UsesDirectContainer()
	{
		using var services = CreateServices(registerAttributedServices: true);
		var container = services.Provider.Resolve<PrimaryContainerProvider>();
		var qualifiedName = $" contained @ {PrimaryContainerName} ";

		Assert.NotNull(container);
		Assert.Same(container.Service, services.Provider.Locate<ITestService>(qualifiedName));
		Assert.Same(container.Service, services.Provider.Locate(qualifiedName, typeof(ITestService)));
		Assert.Equal(2, container.CallCount);
	}

	[Fact]
	public void Locate_QualifiedName_IgnoresWrongTypedContainerAndFindsCompatibleProvider()
	{
		var expected = new NamedService("contained", "compatible");
		var compatible = new MatchingProvider(WrongContainerName, expected);
		using var services = CreateServices(collection =>
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(compatible), registerAttributedServices: true);
		var wrong = services.Provider.Resolve<WrongContainerProvider>();
		var qualifiedName = $"contained@{WrongContainerName}";

		Assert.NotNull(wrong);
		Assert.Same(expected, services.Provider.Locate<ITestService>(qualifiedName));
		Assert.Same(expected, services.Provider.Locate(qualifiedName, typeof(ITestService)));
		Assert.Equal(0, wrong.CallCount);
		Assert.Equal(2, compatible.CallCount);
	}

	[Fact]
	public void Locate_MissingServicesAndProviders_ReturnNull()
	{
		using var services = CreateServices();

		Assert.Null(services.Provider.Locate<ITestService>("service-locator-missing"));
		Assert.Null(services.Provider.Locate("service-locator-missing", typeof(ITestService)));
		Assert.Null(services.Provider.Locate<ITestService>("missing@missing-container"));
		Assert.Null(services.Provider.Locate("missing@missing-container", typeof(ITestService)));
		Assert.Null(services.Provider.Locate<IOtherService>(null));
		Assert.Null(services.Provider.Locate(null, typeof(IOtherService)));
	}

	[Fact]
	public void Locate_NullServices_ThrowsArgumentNullException()
	{
		System.IServiceProvider missingServices = null;

		var genericServicesException = Assert.Throws<ArgumentNullException>(() => missingServices.Locate<ITestService>(null));
		var runtimeServicesException = Assert.Throws<ArgumentNullException>(() => ServiceLocator.Locate(missingServices, null, typeof(ITestService)));

		Assert.Equal("services", genericServicesException.ParamName);
		Assert.Equal("services", runtimeServicesException.ParamName);
	}

	[Fact]
	public void Converter_CanConvertFrom_RequiresStringAndServiceType()
	{
		var descriptor = TypeDescriptor.GetProperties(typeof(ConverterOptions))[nameof(ConverterOptions.Service)];
		var context = new DescriptorContext(descriptor);
		var explicitConverter = new ServiceLocator.Converter(typeof(ITestService));
		var contextualConverter = new ServiceLocator.Converter();

		Assert.True(explicitConverter.CanConvertFrom(null, typeof(string)));
		Assert.False(explicitConverter.CanConvertFrom(null, typeof(int)));
		Assert.True(contextualConverter.CanConvertFrom(context, typeof(string)));
		Assert.False(contextualConverter.CanConvertFrom(null, typeof(string)));
	}

	[Fact]
	public void Converter_ConvertFrom_UsesPropertyDescriptorServiceType()
	{
		const string name = "service-locator-contextual-converter";
		var expected = new NamedService(name, "contextual");
		using var services = CreateServices(collection =>
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(new NamedProvider(expected)));
		using var application = new ApplicationScope(services.Provider);
		var descriptor = TypeDescriptor.GetProperties(typeof(ConverterOptions))[nameof(ConverterOptions.Service)];
		var context = new DescriptorContext(descriptor);
		var converter = new ServiceLocator.Converter();

		Assert.Same(expected, converter.ConvertFrom(context, CultureInfo.InvariantCulture, name));
	}

	[Fact]
	public void Converter_ConvertFrom_ExplicitServiceTypeTakesPrecedenceOverContext()
	{
		const string name = "service-locator-explicit-precedence";
		var expected = new NamedService(name, "explicit");
		using var services = CreateServices(collection =>
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(new NamedProvider(expected)));
		using var application = new ApplicationScope(services.Provider);
		var descriptor = TypeDescriptor.GetProperties(typeof(ConverterOptions))[nameof(ConverterOptions.Other)];
		var context = new DescriptorContext(descriptor);
		var converter = new ServiceLocator.Converter(typeof(ITestService));

		Assert.Same(expected, converter.ConvertFrom(context, CultureInfo.InvariantCulture, name));
	}

	[Fact]
	public void GetTypeConverter_ExplicitConverter_UsesTypeConstructorWithoutContext()
	{
		const string name = "service-locator-explicit-converter";
		var expected = new NamedService(name, "explicit");
		using var services = CreateServices(collection =>
			collection.AddSingleton<Zongsoft.Services.IServiceProvider<ITestService>>(new NamedProvider(expected)));
		using var application = new ApplicationScope(services.Provider);
		var member = typeof(ConverterOptions).GetProperty(nameof(ConverterOptions.Service));
		var converter = Zongsoft.Common.Convert.GetTypeConverter(member, explicitly: true);

		Assert.IsType<ServiceLocator.Converter>(converter);
		Assert.True(converter.CanConvertFrom(null, typeof(string)));
		Assert.Same(expected, converter.ConvertFrom(null, CultureInfo.InvariantCulture, name));
	}

	[Fact]
	public void Converter_ConvertFrom_WithoutServiceType_DelegatesToBase()
	{
		var converter = new ServiceLocator.Converter();

		Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, "missing"));
	}

	private static ServiceScope CreateServices(Action<IServiceCollection> configure = null, bool registerAttributedServices = false)
	{
		var collection = new ServiceCollection();

		if(registerAttributedServices)
			collection.Register(typeof(ServiceLocatorTest).Assembly, null);

		configure?.Invoke(collection);
		return new ServiceScope(new ServiceProviderFactory().CreateServiceProvider(collection));
	}

	public interface ITestService
	{
		string Name { get; }
		string Value { get; }
	}

	public interface IOtherService { }

	[Service(DirectServiceName, typeof(ITestService))]
	public sealed class DirectService : ITestService
	{
		public string Name => DirectServiceName;
		public string Value => "direct";
	}

	[Service(WrongDirectServiceName)]
	public sealed class WrongDirectService { }

	[Service(PrimaryContainerName, typeof(Zongsoft.Services.IServiceProvider<ITestService>))]
	public sealed class PrimaryContainerProvider : Zongsoft.Services.IServiceProvider<ITestService>
	{
		private int _callCount;

		public int CallCount => _callCount;
		public ITestService Service { get; } = new NamedService("contained", "primary");

		public ITestService GetService(string name = null)
		{
			_callCount++;
			return string.Equals(name, this.Service.Name, StringComparison.OrdinalIgnoreCase) ? this.Service : null;
		}
	}

	[Service(WrongContainerName)]
	public sealed class WrongContainerProvider : Zongsoft.Services.IServiceProvider<IOtherService>
	{
		private int _callCount;

		public int CallCount => _callCount;

		public IOtherService GetService(string name = null)
		{
			_callCount++;
			return null;
		}
	}

	private sealed class NamedService(string name, string value) : ITestService
	{
		public string Name { get; } = name;
		public string Value { get; } = value;
	}

	private sealed class NamedProvider(ITestService service) : Zongsoft.Services.IServiceProvider<ITestService>
	{
		private int _callCount;

		public int CallCount => _callCount;

		public ITestService GetService(string name = null)
		{
			_callCount++;
			return string.Equals(name, service.Name, StringComparison.OrdinalIgnoreCase) ? service : null;
		}
	}

	private sealed class MatchingProvider(string containerName, ITestService service) : Zongsoft.Services.IServiceProvider<ITestService>, IMatchable
	{
		private int _callCount;

		public int CallCount => _callCount;
		public bool Match(object argument) => string.Equals(containerName, argument?.ToString(), StringComparison.OrdinalIgnoreCase);

		public ITestService GetService(string name = null)
		{
			_callCount++;
			return string.Equals(name, service.Name, StringComparison.OrdinalIgnoreCase) ? service : null;
		}
	}

	private sealed class ConverterOptions
	{
		[TypeConverter(typeof(ServiceLocator.Converter))]
		public ITestService Service { get; set; }

		[TypeConverter(typeof(ServiceLocator.Converter))]
		public IOtherService Other { get; set; }
	}

	private sealed class DescriptorContext(PropertyDescriptor descriptor) : ITypeDescriptorContext
	{
		public IContainer Container => null;
		public object Instance => null;
		public PropertyDescriptor PropertyDescriptor { get; } = descriptor;

		public object GetService(Type serviceType) => null;
		public void OnComponentChanged() { }
		public bool OnComponentChanging() => true;
	}

	private sealed class ServiceScope(System.IServiceProvider provider) : IDisposable
	{
		public System.IServiceProvider Provider { get; } = provider;
		public void Dispose() => (this.Provider as IDisposable)?.Dispose();
	}

	private sealed class ApplicationScope : IDisposable
	{
		private static readonly FieldInfo CurrentField = typeof(ApplicationContext).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);

		private readonly IApplicationContext _previous;
		private readonly TestApplicationContext _current;

		public ApplicationScope(System.IServiceProvider services)
		{
			_previous = ApplicationContext.Current;
			_current = new TestApplicationContext(services);
		}

		public void Dispose()
		{
			_current.Dispose();
			CurrentField.SetValue(null, _previous);
		}
	}

	private sealed class TestApplicationContext(System.IServiceProvider services) : ApplicationContext(services) { }
}
