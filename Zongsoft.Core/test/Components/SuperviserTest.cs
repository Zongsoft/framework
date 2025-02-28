using System;
using System.Linq;
using System.Threading;

using Xunit;

namespace Zongsoft.Components.Tests;

public class SuperviserTest
{
	private readonly Superviser<string> _superviser = new();

	private void Initialize(int count = 2)
	{
		for(int i = 0; i < count; i++)
		{
			var name = $"S{i + 1}";
			_superviser.Supervise(name, new MySupervisable(name));
			_superviser.Supervise(name, new MySupervisable(name));
		}
	}

	[Fact]
	public void TestContains()
	{
		this.Initialize(2);

		Assert.Equal(2, _superviser.Count);
		Assert.True(_superviser.Contains("S1"));
		Assert.True(_superviser.Contains("S2"));
		Assert.False(_superviser.Contains("Not Existed!"));

		Assert.NotNull(_superviser["S1"]);
		Assert.NotNull(_superviser["S2"]);
		Assert.Null(_superviser["Not Existed!"]);
	}

	[Fact]
	public void TestUnsupervise()
	{
		this.Initialize(2);

		Assert.True(_superviser.Unsupervise("S1", out var observable));
		Assert.NotNull(observable);
		Assert.IsType<MySupervisable>(observable);
		Assert.Equal("S1", ((MySupervisable)observable).Name);
		Assert.True(((MySupervisable)observable).IsUnsupervised(1000));
		Assert.False(_superviser.Contains("S1"));

		Assert.True(_superviser.Unsupervise("S2", out observable));
		Assert.NotNull(observable);
		Assert.IsType<MySupervisable>(observable);
		Assert.Equal("S2", ((MySupervisable)observable).Name);
		Assert.True(((MySupervisable)observable).IsUnsupervised(1000));
		Assert.False(_superviser.Contains("S2"));

		Assert.Equal(0, _superviser.Count);
	}

	private sealed class MySupervisable(string name) : Supervisable<string>, IEquatable<string>, IEquatable<MySupervisable>
	{
		#region 私有变量
		private bool _isUnsupervised;
		#endregion

		#region 公共属性
		public string Name { get; } = name;
		#endregion

		#region 取消监视
		public bool IsUnsupervised(int milliseconds) => SpinWait.SpinUntil(() => _isUnsupervised, milliseconds);
		public bool IsUnsupervised(TimeSpan timeout) => SpinWait.SpinUntil(() => _isUnsupervised, timeout);
		protected override void OnUnsupervised(ISuperviser<string> superviser) => _isUnsupervised = true;
		#endregion

		#region 重写方法
		public bool Equals(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
		public bool Equals(MySupervisable other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj switch
		{
			string name => this.Equals(name),
			MySupervisable other => this.Equals(other),
			_ => false,
		};

		public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
		public override string ToString() => this.Name;
		#endregion
	}
}
