
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	public partial class Ball
	{
		public static float Acceleration = 750;
		public static float AirControl = 1f;
		public static float MaxSpeed = 1100f;

		public static float Friction = 0.25f;
		public static float Drag = 0.1f;
		public static float WallBounce = 0.25f;
		public static float FloorBounce = 0.25f;

		public static List<Ball> All { get; private set; } = new();
		public static Ball Find( int networkIdent ) => dictionary.TryGetValue( networkIdent, out Ball ball ) ? ball : null;
		public static Ball Find( Client client ) => Find( client.NetworkIdent );

		private static Dictionary<int,Ball> dictionary = new();

		public static Ball Instantiate( Client client, Vector3 position  )
		{
			Ball newBall = new Ball() { Owner = client, Position = position };
			All.Add( newBall );
			dictionary.Add( client.NetworkIdent, newBall );
			
			return newBall;
		}

		public static Ball Create( Client client )
		{
			if ( Host.IsClient )
				return null;

			var spawnpoint = Entity.All.OfType<SpawnPoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

			Vector3 position = Vector3.Up * 40f;
			if ( spawnpoint != null )
				position += spawnpoint.Position;

			Ball newBall = Instantiate( client, position );
			newBall.clothesData = client.GetClientData( "avatar" );
			NetCreate( client, position, newBall.clothesData );

			return newBall;
		}
		private Ball() { }

		[ClientRpc]
		public static void NetCreate( Client owner, Vector3 position, string clothesData )
		{
			if ( !owner.IsValid() )
				return;
			// return if ball already exists on client
			if ( Find( owner.NetworkIdent ).IsValid() )
				return;

			Ball newBall = Instantiate( owner, position );
			newBall.clothesData = clothesData;
			newBall.SetupModels();
		}

		[ClientRpc]
		public static void NetDelete( int netIdent )
		{
			Ball ball = Find( netIdent );
			if ( ball.IsValid() ) 
				ball.Delete();
		}

		[ClientRpc]
		public static void NetData( int netIdent, Vector3 pos, Vector3 vel )
		{
			Ball ball = Find( netIdent );
			if ( !ball.IsValid() )
				return;

			if ( ball.Owner != Local.Client)
			{
				ball.Position = pos;
				ball.Velocity = vel;
			}
		}

		[ServerCmd]
		public static void RequestBalls()
		{
			if ( ConsoleSystem.Caller == null )
				return;

			foreach ( Ball ball in All )
				NetCreate( To.Single( ConsoleSystem.Caller ), ball.Owner, ball.Position, ball.clothesData );
		}

		[ServerCmd]
		public static void SendData( int netIdent, Vector3 pos, Vector3 vel)
		{
			Ball ball = Find( netIdent );

			if ( !ball.IsValid() )
				return;

			if ( ConsoleSystem.Caller != ball.Owner )
				return;

			// currently broken in the case of falling
			/*
			float error = (ball.Position - pos).Length;
			if ( error > 25 )	
				Log.Warning( $"{ball.Owner.Name} requested a position {error} units away from the expected position." );

			if ( error > 100 )
			{
				Log.Error( $"{ball.Owner.Name} requested a position {error} units away from the expected position." );
				ball.Owner.Kick();
			}
			*/

			ball.Position = ball.Position.LerpTo( pos, 0.25f );
			ball.Velocity = ball.Velocity.LerpTo( vel, 0.25f );
		}

		[Event.Frame]
		public static void StaticFrame()
		{
			foreach ( Ball ball in All )
				ball.Frame();
		}

		[Event.Physics.PreStep]
		public static void StaticPreStep()
		{
			foreach ( Ball ball in All )
				ball.PreStep();
		}

		[Event.Tick]
		public static void StaticTick()
		{
			foreach(Ball ball in All)
			{
				if ( ball.Owner == null || !ball.Owner.IsValid() )
					ball.queueDeletion = true;

				int indent = ball.NetworkIdent;
				if ( ball.queueDeletion && dictionary.ContainsKey( indent ) )
					dictionary.Remove( indent );
			}
			All = All.Where( b => !b.queueDeletion ).ToList();

			foreach ( Ball ball in All )
				ball.Tick();
		}
	}

	public static class BallExtensions
	{
		public static bool IsValid( this Ball ball ) => ball != null && !ball.QueueDeletion;
	}
}
