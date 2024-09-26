using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests
{
	public class EventContextTest
	{
		[Fact]
		public void TestGetArgument()
		{
			object argument;

			var stringContext = new EventContext<string>(MyEventRegistry.Instance, "MyEvent", "MyString");
			argument = EventContextUtility.GetArgument(stringContext);
			Assert.NotNull(argument);
			Assert.IsType<string>(argument);
			Assert.Equal("MyString", argument);

			var integerContext = new EventContext<int>(MyEventRegistry.Instance, "MyEvent", 100);
			argument = EventContextUtility.GetArgument(integerContext);
			Assert.NotNull(argument);
			Assert.IsType<int>(argument);
			Assert.Equal(100, argument);
		}

		internal class MyEventRegistry() : EventRegistryBase("MyEventRegistry")
		{
			public static readonly MyEventRegistry Instance = new();
		}
	}
}
