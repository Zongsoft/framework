using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

	[Fact]
	public void TestEventRaises()
	{
		const int COUNT = 100;
		const int TIMEOUT = 2 * 1000;

		//挂载监测完成事件
		_superviser.Supervised += this.OnSupervised;

		//并发执行监测方法，以触发监测完成事件
		Parallel.For(0, COUNT, index =>
		{
			_superviser.Supervise($"S{index + 1}", new MySupervisable($"S{index + 1}"));
		});

		//确保计数器数值一致
		Assert.Equal(COUNT, _raises);

		//挂载取消监测事件
		_superviser.Unsupervised += this.OnUnsupervised;

		//并发执行取消监测方法，以触发监测取消事件
		Parallel.For(0, COUNT, index =>
		{
			_superviser.Unsupervise($"S{index + 1}", out _);
		});

		//由于取消监测事件回调有延迟，因此需要等待取消事件回调完成
		SpinWait.SpinUntil(() => _raises <= 0, TIMEOUT);

		//确保计数器数值一致
		Assert.Equal(0, _raises);
	}

	#region 事件处理
	private volatile int _raises = 0;
	private void OnSupervised(object sender, SuperviserEventArgs<string> e) => Interlocked.Increment(ref _raises);
	private void OnUnsupervised(object sender, SuperviserEventArgs<string> e) => Interlocked.Decrement(ref _raises);
	#endregion

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
		protected override void OnUnsupervised(ISuperviser<string> superviser, SupervisableReason reason) => _isUnsupervised = true;
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
