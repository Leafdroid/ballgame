using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace Ballers
{

	public class Timer : Panel
	{
		public Label Label;

		public Timer()
		{
			Label = Add.Label( "00:00.000", "value" );
		}

		public override void Tick()
		{
			var player = Local.Pawn as Ball;
			if ( player == null ) return;

			float time = player.SimulationTime;
			if ( time < 0f )
				time = 0;

			Label.Text = $"{Stringify( time )}";
		}

		public static string Stringify( float time )
		{
			float minutes = time / 60f;
			int fullMinutes = (int)minutes;
			float seconds = (minutes - fullMinutes) * 60f;
			int fullSeconds = (int)seconds;
			int milliseconds = (int)((seconds - fullSeconds) * 1000f);

			return $"{fullMinutes.ToString( "00" )}:{fullSeconds.ToString( "00" )}.{milliseconds.ToString( "000" )}";
		}
	}

}
