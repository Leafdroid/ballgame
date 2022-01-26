
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

		public void Finished( Ball ball )
		{
			if ( Host.IsClient )
				return;

			if ( ball.Controller == Ball.ControlType.Replay )
			{
				ball.Pop();
				ball.DeleteAsync( 1f );
				return;
			}

			Client client = ball.Client;

			float time = Time.Now - ball.PredictedStart;
			string timeString = Stringify( time );

			float personalBest = client.GetValue( "time", -1f );
			bool newBest = personalBest == -1f || personalBest > time;
			bool worldBest = false;
			if ( newBest )
			{
				client.SetValue( "time", time );
				client.SetValue( "timeString", timeString );

				ball.ReplayData.Write( client );

				string fileName = $"records/{Global.MapName}/{client.PlayerId}.record";
				FileSystem.Data.CreateDirectory( $"records/{Global.MapName}" );

				using ( var writer = new BinaryWriter( FileSystem.Data.OpenWrite( fileName ) ) )
					writer.Write( time );

				string worldBestFile = $"records/{Global.MapName}/world.record";
				if ( FileSystem.Data.FileExists( worldBestFile ) )
				{
					using ( var reader = new BinaryReader( FileSystem.Data.OpenRead( worldBestFile ) ) )
					{
						float worldTime = reader.ReadSingle();

						if ( time < worldTime )
							worldBest = true;
					}
				}
				else worldBest = true;

				if ( worldBest )
				{
					using ( var writer = new BinaryWriter( FileSystem.Data.OpenWrite( worldBestFile ) ) )
						writer.Write( time );
				}
			}

			string text = $"{client.Name} finished in {timeString}!{(worldBest ? " New world record!" : newBest ? " New personal best!" : "")}";

			Log.Info( text );
			ChatBox.AddInformation( To.Everyone, text, $"avatar:{client.PlayerId}" );
		}

		public void Checkpointed( Ball ball )
		{
			if ( Host.IsClient )
				return;

			if ( ball.Controller == Ball.ControlType.Replay )
				return;

			Client client = ball.Client;
			float time = Time.Now - ball.PredictedStart;
			string text = $"{client.Name} reached checkpoint {ball.CheckpointIndex} in {Stringify( time )}!";

			Log.Info( text );
			ChatBox.AddInformation( To.Everyone, text, $"avatar:{client.PlayerId}" );
		}

		public string Stringify( float time )
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
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{

			base.ClientDisconnect( cl, reason );
		}
	}
}
