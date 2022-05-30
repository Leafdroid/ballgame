
using Sandbox;

namespace Ballers
{
	public partial class BallersGame : Sandbox.Game
	{
		public BallersGame()
		{
			if ( IsServer )
				new BallersHudEntity();
		}

		public override void Simulate( Client client )
		{
			base.Simulate( client );

			if ( client.IsListenServerHost && Host.IsServer )
			{
				foreach ( Ball ball in Ball.ReplayGhosts )
					ball.Simulate( client );
			}

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

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			if ( IsServer )
				Ball.DeliverClothing( client );

			ReplayData replay = ReplayData.FromClient( client );
			if ( replay != null )
			{
				float time = replay.FinishTime;
				string timeString = Stringify( time );
				client.SetValue( "time", time );
				client.SetValue( "timeString", timeString );
			}

			var player = new Ball();
			client.Pawn = player;

			player.Create();
			player.Respawn();
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{

			base.ClientDisconnect( cl, reason );
		}
	}
}
