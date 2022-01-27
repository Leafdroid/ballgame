
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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
		private static string Stringify( float time )
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

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			if ( IsServer )
				Ball.DeliverClothing( client );

			string fileName = $"records/{Global.MapName}/{client.PlayerId}.record";
			if ( FileSystem.Data.FileExists( fileName ) )
			{
				using ( var reader = new BinaryReader( FileSystem.Data.OpenRead( fileName ) ) )
				{
					float time = reader.ReadSingle();
					string timeString = Stringify( time );
					client.SetValue( "time", time );
					client.SetValue( "timeString", timeString );
				}
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
