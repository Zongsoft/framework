using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Services.Tests
{
	public class ServiceProviderTest
	{
		private ServiceProvider _provider1;
		private ServiceProvider _provider2;
		private ServiceProvider _provider3;

		public ServiceProviderTest()
		{
			_provider1 = new ServiceProvider("ServiceProvider#1");
			_provider2 = new ServiceProvider("ServiceProvider#2");
			_provider3 = new ServiceProvider("ServiceProvider#3");

			ServiceProviderFactory.Instance.Default = new ServiceProvider("Default");
			ServiceProviderFactory.Instance.Default.Register("string", "I'm a service.");

			_provider1.Register("DC1", new DummyCommand("DummyCommand-1"), typeof(ICommand));
			_provider1.Register(typeof(Zongsoft.Tests.Address));

			_provider2.Register("DC2", new DummyCommand("DummyCommand-2"), typeof(ICommand));
			_provider2.Register(typeof(Zongsoft.Tests.Department));

			_provider3.Register("DC3", new DummyCommand("DummyCommand-3"), typeof(ICommand));
			_provider3.Register(typeof(Zongsoft.Tests.Person));
		}

		[Fact]
		public void Test()
		{
			ICommand command = null;

			command = _provider1.Resolve<ICommand>();
			Assert.NotNull(command);
			Assert.Equal("DummyCommand-1", command.Name);

			command = _provider1.Resolve("DC1") as ICommand;
			Assert.NotNull(command);
			Assert.Equal("DummyCommand-1", command.Name);

			command = _provider1.Resolve("DC2") as ICommand;
			Assert.Null(command);

			Assert.NotNull(_provider1.Resolve("string"));
			Assert.IsAssignableFrom<string>(_provider1.Resolve("string"));
			Assert.NotNull(_provider2.Resolve<string>());
			Assert.NotNull(_provider3.Resolve<string>());
			Assert.IsAssignableFrom<string>(_provider3.Resolve("string"));

			//将二号服务容器加入到一号服务容器中
			_provider1.Register(_provider2);

			command = _provider1.Resolve("DC2") as ICommand;
			Assert.NotNull(command);
			Assert.Equal("DummyCommand-2", command.Name);

			command = _provider1.Resolve("DC3") as ICommand;
			Assert.Null(command);

			//将三号服务容器加入到二号服务容器中
			_provider2.Register(_provider3);

			//将一号服务容器加入到三号服务容器中（形成循环链）
			_provider3.Register(_provider1);

			command = _provider1.Resolve("DC3") as ICommand;
			Assert.NotNull(command);
			Assert.Equal("DummyCommand-3", command.Name);

			var address = _provider1.Resolve<Zongsoft.Tests.Address>();
			Assert.NotNull(address);

			var department = _provider1.Resolve<Zongsoft.Tests.Department>();
			Assert.NotNull(department);

			var person = _provider1.Resolve<Zongsoft.Tests.Person>();
			Assert.NotNull(person);

			Assert.NotNull(_provider1.Resolve("string"));
			Assert.IsAssignableFrom<string>(_provider1.Resolve("string"));
			Assert.NotNull(_provider2.Resolve<string>());
			Assert.NotNull(_provider3.Resolve<string>());
			Assert.IsAssignableFrom<string>(_provider3.Resolve("string"));

			//测试不存在的服务
			Assert.Null(_provider1.Resolve<IWorker>());
			Assert.Null(_provider1.Resolve("NoExisted"));
		}

		private class DummyCommand : CommandBase
		{
			public DummyCommand(string name) : base(name)
			{
			}

			protected override object OnExecute(object parameter)
			{
				throw new NotImplementedException();
			}
		}
	}
}
