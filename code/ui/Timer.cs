using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Ballers
{

	public class Timer : Panel
	{
		public Label Label;

		public Timer()
		{
			Label = Add.Label( "00:00:000", "value" );
		}

		public override void Tick()
		{
			var player = Local.Pawn as Ball;
			if ( player == null ) return;

			float time = Time.Now - player.PredictedStart;
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

			return $"{FillNumber( fullMinutes, 2 )}:{FillNumber( fullSeconds, 2 )}:{FillNumber( milliseconds, 3 )}";
		}
		private static string FillNumber( int num, int desired )
		{
			string number = num.ToString();

			int delta = desired - number.Length;

			if ( delta > 0 )
			{
				number = "";
				for ( int i = 0; i < delta; i++ )
					number += "0";
				number += num.ToString();
			}
			return number;
		}
	}

}
