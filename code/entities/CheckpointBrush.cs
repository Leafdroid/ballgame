using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Sandbox.UI;

namespace Ballers
{

	[Library( "func_checkpoint" )]
	[Hammer.SupportsSolid]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public partial class CheckpointBrush : ModelEntity
	{
		/// <summary>
		/// Used for checkpoint order and for linking spawnpoints. Do not use 0 for this, start with 1.
		/// </summary>
		[Property( "index", Title = "Index" )]
		[Net] public int Index { get; private set; } = 0;

		public static int LastIndex { get; private set; }
		public bool IsFinish => Index == LastIndex;

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
			Transmit = TransmitType.Always;
		}

		private void SharedSpawn()
		{
			EnableDrawing = false;
			EnableAllCollisions = false;
			EnableTraceAndQueries = true;

			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.Trigger );

			if ( Index > LastIndex )
				LastIndex = Index;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}

		public void Trigger( Ball player, float fraction )
		{
			// not correct order, ignore
			if ( Index != player.CheckpointIndex + 1 )
				return;

			if ( IsClient )
				Sound.FromScreen( Swoosh.Name );


			float tickTime = player.ActiveTick * Global.TickInterval;
			float fractionTime = (1f - fraction) * Global.TickInterval;
			float time = tickTime - fractionTime;

			player.CheckpointIndex++;

			if ( player.Controller == Ball.ControlType.Player )
				player.ReplayData.AddTime( time );

			/*
			string timeString = Stringify( time );
			Log.Info( $"Reached checkpoint in {player.ActiveTick} ticks ({tickTime} seconds)" );
			Log.Info( $"Fraction was {fraction}, subtracting {fractionTime} seconds." );
			Log.Info( $"Full time was {time} seconds. ({timeString})" );
			*/

			if ( IsFinish )
				Finished( player, time );
			else
				Checkpointed( player, time );
		}

		public void Finished( Ball ball, float time )
		{
			if ( ball.Controller == Ball.ControlType.Replay )
			{
				ball.Pop();
				ball.DeleteAsync( 1f );
				return;
			}

			Client client = ball.Client;

			if ( Host.IsServer )
			{
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

			ball.Reset();
		}

		public void Checkpointed( Ball ball, float time )
		{
			if ( Host.IsClient )
				return;

			if ( ball.Controller == Ball.ControlType.Replay )
				return;

			Client client = ball.Client;
			string text = $"{client.Name} reached checkpoint {ball.CheckpointIndex} in {Stringify( time )}!";

			Log.Info( text );
			ChatBox.AddInformation( To.Everyone, text, $"avatar:{client.PlayerId}" );
		}

		private static string Stringify( float time )
		{
			float minutes = time / 60f;
			int fullMinutes = (int)minutes;
			float seconds = (minutes - fullMinutes) * 60f;
			int fullSeconds = (int)seconds;
			int milliseconds = (int)((seconds - fullSeconds) * 1000f);

			return $"{fullMinutes.ToString( "00" )}:{fullSeconds.ToString( "00" )}.{milliseconds.ToString( "000" )}";
		}

		public static readonly SoundEvent Badge = new( "sounds/ball/badge.vsnd" )
		{
			Pitch = 1f,
			Volume = 0.4f,
			DistanceMax = 1024,
			UI = true
		};

		public static readonly SoundEvent Swoosh = new( "sounds/ball/swoosh.vsnd" )
		{
			Pitch = 1f,
			Volume = 0.4f,
			DistanceMax = 1024,
			UI = true
		};
	}
}
