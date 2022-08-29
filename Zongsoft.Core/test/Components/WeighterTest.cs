using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Components
{
	public class WeighterTest
	{
		[Fact]
		public void Test1()
		{
			var servers = new Server[]
			{
				new Server(@"serverA", 5),
				new Server(@"serverB", 1),
				new Server(@"serverC", 1),
			};

			var weighter = new Weighter<Server>(servers.Select(server => new Weighter<Server>.Entry(server, server.Weight)));

			for(int i = 0; i < 100; i++)
			{
				var server = weighter.Get();

				Console.WriteLine($"[{(i + 1):00}]{server.Name}");
				System.Diagnostics.Debug.WriteLine($"[{(i+1):00}]{server.Name}");
			}
		}

		[Fact]
		public void Test2()
		{
			var servers = new Server[]
			{
				new Server(@"serverA")
			};

			var weighter = new Weighter<Server>(servers.Select(server => new Weighter<Server>.Entry(server, server.Weight)));

			for(int i = 0; i < 100; i++)
			{
				var server = weighter.Get();

				Console.WriteLine($"[{(i + 1):00}]{server.Name}");
				System.Diagnostics.Debug.WriteLine($"[{(i + 1):00}]{server.Name}");
			}
		}

		public class Server : IEquatable<Server>
		{
			public Server(string name, int weight = 100)
			{
				this.Name = name;
				this.Weight = weight;
			}

			public string Name { get; }
			public int Weight { get; }

			public bool Equals(Server other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
			public override bool Equals(object obj) => obj is Server other && this.Equals(other);
			public override int GetHashCode() => this.Name.GetHashCode();
			public override string ToString() => $"{this.Name}:{this.Weight}";
		}
	}
}
