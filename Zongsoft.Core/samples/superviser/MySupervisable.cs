using System;
using System.Threading;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

public class MySupervisable : Supervisable<string>, IEquatable<MySupervisable>, IEquatable<string>, IDisposable
{
	#region 常量定义
	private const int INTERVAL = 2000;
	#endregion

	#region 成员字段
	private string _name;
	private Timer _timer;
	#endregion

	#region 构造函数
	public MySupervisable(string name, SupervisableOptions options = null) : base(options)
	{
		_name = name ?? throw new ArgumentNullException(nameof(name));
		_timer = new(this.OnTick, null, Timeout.Infinite, 2000);
	}
	#endregion

	#region 公共属性
	public string Name => _name;
	public DateTime Timestamp { get; private set; }
	#endregion

	#region 公共方法
	public void Open() => _timer.Change(0, INTERVAL);
	public void Pause() => _timer.Change(Timeout.Infinite, Timeout.Infinite);
	public void Resume() => _timer.Change(0, INTERVAL);

	public void Close()
	{
		_timer.Change(Timeout.Infinite, Timeout.Infinite);
		this.Observer?.OnCompleted();
	}

	public void Error(string message = null)
	{
		if(string.IsNullOrEmpty(message))
			message = $"Err #{Random.Shared.NextInt64():X}";

		this.Observer?.OnError(new InvalidOperationException(message));
	}
	#endregion

	#region 私有方法
	private void OnTick(object state)
	{
		this.Timestamp = DateTime.Now;
		this.Observer?.OnNext(Random.Shared.NextInt64().ToString("X"));
	}
	#endregion

	#region 重写方法
	protected override Subscriber OnSubscribe(IObserver<string> observer)
	{
		Terminal.WriteLine(
			CommandOutletContent.Create(CommandOutletColor.DarkGray, $"[{DateTime.Now:HH:mm:ss.fff}] ")
								.Append(CommandOutletColor.DarkGreen, $"{this.Name} Subscribed."));

		return base.OnSubscribe(observer);
	}

	protected override void OnUnsupervised(ISuperviser<string> superviser, SupervisableReason reason)
	{
		Terminal.WriteLine(
			CommandOutletContent.Create(CommandOutletColor.DarkGray, $"[{DateTime.Now:HH:mm:ss.fff}] ")
								.Append(CommandOutletColor.DarkCyan, $"{this.Name} Unsupervised<{reason}>."));

		this.Dispose();
	}

	public bool Equals(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	public bool Equals(MySupervisable other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj switch
	{
		string name => this.Equals(name),
		MySupervisable supervisable => this.Equals(supervisable),
		_ => false,
	};

	public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
	public override string ToString() => this.Options == null ?
		$"{this.Name}@{this.Timestamp:HH:mm:ss.fff}" :
		$"{this.Name}@{this.Timestamp:HH:mm:ss.fff}({this.Options})";
	#endregion

	#region 处置方法
	public void Dispose() => Terminal.WriteLine(CommandOutletColor.Magenta, $"The {this.Name} supervisable object was disposed.");
	#endregion
}
