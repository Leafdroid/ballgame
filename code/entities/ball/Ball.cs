
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class Ball : ModelEntity
	{
		public static readonly new List<Ball> All = new();

		public static Ball Create( Client client, ControlType controller = ControlType.Player )
		{
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

		private bool hasColor = false;

		public override void Spawn()
		{
			base.Spawn();

			Predictable = true;

			SetModel( "models/ball.vmdl" );
			PhysicsEnabled = false;

			EnableAllCollisions = false;
			EnableTraceAndQueries = false;
			Transmit = TransmitType.Always;

			All.Add( this );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			PhysicsEnabled = false;
			Predictable = true;

			SetupTerry();

			All.Add( this );
		}

		private bool popped = false;
		protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( IsClient && Controller == ControlType.Player && !popped )
			{
				Ragdoll();
				//Sound.FromWorld( WilhelmScream.Name, Position );
				BallDome.Create( this );
				popped = true;
			}

			if ( Terry.IsValid() )
				Terry.Delete();

			if ( All.Contains( this ) )
				All.Remove( this );
		}

		private float GetHue()
		{
			int id = Rand.Int( 65535 );
			if ( Owner.IsValid() && Owner.Client.IsValid() )
				id = (int)(Owner.Client.PlayerId & 65535);

			Random seedColor = new Random( id );
			return (float)seedColor.NextDouble() * 360f;
		}

		public void FrameSimulate()
		{
			UpdateTerry();

			if ( hasColor )
				return;

			if ( !SceneObject.IsValid() )
				return;

			float hue = GetHue();

			float saturation = Controller == ControlType.Player ? 0.75f : 0.4f;

			Color ballColor = new ColorHsv( hue, saturation, 1f );
			Color ballColor2 = new ColorHsv( (hue + 30f) % 360, saturation, 1f );

			SceneObject.SetValue( "tint", ballColor );
			SceneObject.SetValue( "tint2", ballColor2 );

			hasColor = true;
		}

		public static readonly SoundEvent WilhelmScream = new( "sounds/ball/wilhelm.vsnd" )
		{
			DistanceMax = 1536f,
		};

		public override string ToString() => $"{ (Owner.IsValid() ? Owner.Name : "Unknown") }'s ball";
	}
}
