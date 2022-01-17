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

	public static float Bezinterp( float[] values, float t )
	{
		int valueCount = values.Length;

		switch ( valueCount )
		{
			case 0:
				return t;
			case 1:
				return values[0] * t;
			case 2:
				return values[0] * (1f - t) + values[1] * t;
			default:
				int iteration = 1;
				while ( iteration != valueCount )
				{
					for ( int i = 0; i < valueCount - iteration; i++ )
					{
						float val = values[i];
						float nextVal = values[i + 1];

						values[i] = val * (1f - t) + nextVal * t;
					}
					iteration++;
				}
				return values[0];
		}
	}

	[ClientCmd]
	public static void Bezier()
	{
		float[] floats = new float[4] { 0f, -2, 4f, 1f };

		float mult = 1f / 20f;
		for ( int i = 0; i < 20; i++ )
		{
			float t = i * mult;
			Log.Info( Bezinterp( floats, t ) );
		}
	}
}
