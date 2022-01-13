using Sandbox;
using System.Linq;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using Sandbox.Internal.Globals;

public class Stopwatch
{
	private DateTime startTime;
	public Stopwatch() { startTime = DateTime.Now; }
	public double Stop() => (DateTime.Now - startTime).TotalMilliseconds;
	public double Lap()
	{
		double stopTime = Stop();
		Restart();
		return stopTime;
	}
	public void Restart() => startTime = DateTime.Now;
}
