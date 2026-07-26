using System;

namespace Zongsoft.Data.TDengine.Tests.Models;

public struct MotorWaveform
{
	/// <summary>获取或设置采集时间。</summary>
	public DateTime Timestamp { get; set; }
	/// <summary>获取或设置线路编号。</summary>
	public string LineId { get; set; }
	/// <summary>获取或设置站点编号。</summary>
	public string SiteId { get; set; }
	/// <summary>获取或设置扶梯编号。</summary>
	public string ElevatorCode { get; set; }
	/// <summary>获取或设置测量点编号。</summary>
	public string MeasuringId { get; set; }
	/// <summary>获取或设置波形采样值。</summary>
	public string Values { get; set; }
	/// <summary>获取或设置采样频率。</summary>
	public int SampleRate { get; set; }
	/// <summary>获取或设置采样长度。</summary>
	public int Length { get; set; }
	/// <summary>获取或设置压缩比。</summary>
	public int Ratio { get; set; }
}
