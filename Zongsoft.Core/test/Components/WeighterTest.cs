using System;
using System.Linq;

using Xunit;

namespace Zongsoft.Components
{
	public class WeighterTest
	{
		[Fact]
		public void TestGet1()
		{
			var servers = new []
			{
				new Server("A", 4),
				new Server("B", 2),
				new Server("C", 1),
			};

			var weighter = new Weighter<Server>(servers, server => server.Weight);

			//Round 1
			var server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("A", server.Name);
			Assert.Equal(4, server.Weight);

			//Round 2
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("B", server.Name);
			Assert.Equal(2, server.Weight);

			//Round 3
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("A", server.Name);
			Assert.Equal(4, server.Weight);

			//Round 4
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("C", server.Name);
			Assert.Equal(1, server.Weight);

			//Round 5
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("A", server.Name);
			Assert.Equal(4, server.Weight);

			//Round 6
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("B", server.Name);
			Assert.Equal(2, server.Weight);

			//Round 7
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("A", server.Name);
			Assert.Equal(4, server.Weight);

			//Round 8
			server = weighter.Get();
			Assert.NotNull(server);
			Assert.Equal("A", server.Name);
			Assert.Equal(4, server.Weight);
		}

		[Fact]
		public void TestGet2()
		{
			var server = new Server("X");
			var weighter = new Weighter<Server>(new[] { server }, server => server.Weight);

			for(int i = 0; i < 10; i++)
			{
				var found = weighter.Get();
				Assert.NotNull(found);
				Assert.Equal(found, server);
			}
		}

		[Fact]
		public void TestAdd()
		{
			var weighter = new Weighter<Server>(Array.Empty<Server>(), server => server.Weight);

			Assert.Equal(0, weighter.Count);
			Assert.Empty(weighter);

			var server1 = new Server("A", 50);
			weighter.Add(server1);
			Assert.Equal(1, weighter.Count);

			var server2 = new Server("B", 20);
			weighter.Add(server2);
			Assert.Equal(2, weighter.Count);

			var server3 = new Server("C", 10);
			weighter.Add(server3);
			Assert.Equal(3, weighter.Count);

			var servers = weighter.ToArray();
			Assert.Equal(3, servers.Length);
			Assert.Equal(server1, servers[0]);
			Assert.Equal(server2, servers[1]);
			Assert.Equal(server3, servers[2]);
		}

		[Fact]
		public void TestRemove()
		{
			var server1 = new Server("A", 4);
			var server2 = new Server("B", 2);
			var server3 = new Server("C", 1);

			var weighter = new Weighter<Server>(new[] { server1, server2, server3 }, server => server.Weight);

			Assert.Equal(3, weighter.Count);
			Assert.True(weighter.Remove(server2));
			Assert.Equal(2, weighter.Count);

			var servers = weighter.ToArray();
			Assert.Equal(2, servers.Length);
			Assert.Contains(server1, servers);
			Assert.Contains(server3, servers);

			Assert.False(weighter.Remove(server2));
			Assert.Equal(2, servers.Length);

			Assert.True(weighter.Remove(server3));
			Assert.Equal(1, weighter.Count);

			servers = weighter.ToArray();
			Assert.Single(servers);
			Assert.Contains(server1, servers);

			Assert.False(weighter.Remove(server3));
			Assert.Equal(1, weighter.Count);

			Assert.True(weighter.Remove(server1));
			Assert.Equal(0, weighter.Count);

			servers = weighter.ToArray();
			Assert.Empty(servers);
		}

		[Fact]
		public void TestClear()
		{
			var server1 = new Server("A", 4);
			var server2 = new Server("B", 2);
			var server3 = new Server("C", 1);

			var weighter = new Weighter<Server>(new[] { server1, server2, server3 }, server => server.Weight);

			Assert.Equal(3, weighter.Count);
			weighter.Clear();
			Assert.Equal(0, weighter.Count);
			Assert.Empty(weighter);
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
