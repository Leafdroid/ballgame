
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class Ball : Player
	{
		public static new List<Ball> ReplayGhosts = new();

		/*
		public static Ball Create( Client client, ControlType controller = ControlType.Player )
		{
			if ( Host.IsClient )
				return null;

			ReplayData replayData = null;
			BallPlayer player = null;

			if ( controller == ControlType.Player )
			{
				if ( !client.Pawn.IsValid() )
					return null;

				player = client.Pawn as BallPlayer;

				if ( !player.IsValid() )
					return null;
			}
			else
			{
				replayData = ReplayData.FromFile( client );
				if ( replayData == null )
					return null;
			}


			// .OrderBy( x => Guid.NewGuid() )
			var spawnpoint = Entity.All.OfType<SpawnPoint>().FirstOrDefault();

			Vector3 position = Vector3.Up * 80f;
			if ( spawnpoint != null )
				position += spawnpoint.Position;

			//position = new Vector3( 0, -2700, 540 );

			Ball ball = null;

			if ( controller == ControlType.Player )
			{
				string clothingData = player.Client.GetClientData( "avatar" );
				ball = new Ball() { Owner = player, Position = position, ClothingData = clothingData, Controller = controller };
				player.Ball = ball;
			}
			else if ( controller == ControlType.Replay )
			{
				ball = new Ball() { Position = position, Controller = controller };
				ball.ReplayData = replayData;
			}

			return ball;
		}
		*/

		[Net, Predicted] public bool Popped { get; private set; }

		public override void Respawn()
		{
			Host.AssertServer();

			Velocity = Vector3.Zero;

			Popped = false;
			ActiveTick = 0;
			EnableDrawing = true;

			ClothingData = Client.GetClientData( "avatar" );

			SetModel( "models/ball.vmdl" );

			Camera = new BallCamera();
			ReplayData = new ReplayData();

			PhysicsEnabled = false;

			// for water collision effects!
			SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 40f );
			EnableAllCollisions = false;
			EnableTraceAndQueries = true;
			ClearCollisionLayers();
			SetInteractsWith( CollisionLayer.Water );

			EnableShadowCasting = true;
			Transmit = TransmitType.Always;

			Position = Vector3.Up * 80f;
			var spawnpoint = All.OfType<SpawnPoint>().FirstOrDefault();
			if ( spawnpoint != null )
				Position += spawnpoint.Position;

			ResetInterpolation();
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			SetupTerry();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( Terry.IsValid() )
				Terry.Delete();

			if ( ReplayGhosts.Contains( this ) )
				ReplayGhosts.Remove( this );
		}

		public async void RespawnAsync( float time )
		{
			await GameTask.DelaySeconds( time );
			Respawn();
		}

		public void Pop( bool predicted = true )
		{
			if ( (IsServer || !predicted) && Popped )
				return;

			if ( IsServer )
				PopRpc( predicted );

			if ( IsClient )
			{
				Ragdoll();
				BallDome.Create( this );
			}
			else
				RespawnAsync( 2f );

			Popped = true;
			EnableDrawing = false;
		}

		[ClientRpc]
		public void PopRpc( bool predicted = true )
		{
			if ( !predicted || Client != Local.Client )
				Pop();
		}

		private bool isColored = false;
		private float GetHue()
		{
			int id = Rand.Int( 65535 );
			if ( Client.IsValid() )
				id = (int)(Client.PlayerId & 65535);

			Random seedColor = new Random( id );
			return (float)seedColor.NextDouble() * 360f;
		}

		private void SetupColors()
		{
			float hue = GetHue();

			float saturation = Controller == ControlType.Player ? 0.75f : 0.4f;

			Color ballColor = new ColorHsv( hue, saturation, 1f );
			Color ballColor2 = new ColorHsv( (hue + 30f) % 360, saturation, 1f );

			SceneObject.SetValue( "tint", ballColor );
			SceneObject.SetValue( "tint2", ballColor2 );

			isColored = true;
		}

		[Event.Frame]
		public void Frame()
		{
			UpdateTerry();

			if ( !SceneObject.IsValid() )
				return;

			if ( !isColored )
				SetupColors();
		}


		[ServerCmd( "kill" )]
		public static void Kill()
		{
			if ( ConsoleSystem.Caller != null && ConsoleSystem.Caller.Pawn is Ball player )
				player.Pop( false );
		}
	}
}
