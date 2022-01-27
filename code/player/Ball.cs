
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
		public static List<Ball> ReplayGhosts = new();
		public ReplayData ReplayData { get; set; } = new ReplayData();
		public ReplayData PersonalBest { get; set; }

		[Net, Predicted] public int CheckpointIndex { get; set; } = 0;
		[Net, Predicted] public float PredictedStart { get; private set; }
		[Net, Predicted] public int ActiveTick { get; private set; } = 0;
		public float SimulationTime => PredictedStart == -1f ? 0f : Time.Now - PredictedStart;
		public int PredictionTick => (int)(Global.TickRate * SimulationTime);


		public override void Respawn()
		{
			if ( !(this as ModelEntity).IsValid() )
				return;

			if ( Controller == ControlType.Player )
				PersonalBest = ReplayData.FromClient( Client );

			GravityType = GravityType.Default;
			Velocity = Vector3.Zero;
			LifeState = LifeState.Alive;
			EnableDrawing = true;

			SetSpawnpoint();
			ResetInterpolation();
		}

		public void Create()
		{
			if ( !(this as ModelEntity).IsValid() )
				return;

			Host.AssertServer();

			SetModel( "models/ball.vmdl" );

			if ( Controller == ControlType.Player )
			{
				ReplayData = new ReplayData();
				Camera = new BallCamera();
			}
			else
			{
				if ( !ReplayGhosts.Contains( this ) )
					ReplayGhosts.Add( this );
			}

			if ( Client.IsValid() )
				ClothingData = Client.GetClientData( "avatar" );

			PhysicsEnabled = false;

			// for water collision effects!
			SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, 40f );
			EnableAllCollisions = false;
			EnableTraceAndQueries = true;
			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.Player );
			SetInteractsWith( CollisionLayer.Water );

			EnableShadowCasting = true;
			Transmit = TransmitType.Always;
		}

		private void SetSpawnpoint()
		{
			Position = Vector3.Up * 40f;

			var spawnpoints = All.OfType<BallSpawn>();
			var desiredSpawn = spawnpoints.Where( s => s.Index == CheckpointIndex ).FirstOrDefault();
			if ( desiredSpawn != null )
			{
				Position += desiredSpawn.Position;
				return;
			}

			var spawnpoint = All.OfType<SpawnPoint>().FirstOrDefault();
			if ( spawnpoint != null )
				Position += spawnpoint.Position;
		}


		public override void Simulate( Client cl )
		{
			if ( LifeState == LifeState.Alive )
			{
				if ( PredictedStart == -1 )
				{
					PredictedStart = Time.Now;
					ActiveTick = 0;
				}

				SimulateInputs();
				SimulatePhysics();
			}

			ActiveTick++;
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
			await GameTask.Delay( (int)(time * 1000f) );
			Respawn();
		}

		public void Pop( bool predicted = true )
		{
			if ( CheckpointIndex == 0 )
				Reset( false );

			if ( (IsServer || !predicted) && LifeState == LifeState.Dead )
				return;

			if ( IsServer )
			{
				PopRpc( predicted );
				RespawnAsync( 2f );

				if ( Controller == ControlType.Player )
					Client.AddInt( "deaths" );
			}
			else
			{
				Ragdoll();
				BallDome.Create( this );
			}

			LifeState = LifeState.Dead;
			EnableDrawing = false;
		}

		[ClientRpc]
		public void PopRpc( bool predicted = true )
		{
			if ( !predicted || Client != Local.Client || Controller == ControlType.Replay )
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

			float saturation = Controller == ControlType.Player ? 0.8f : 0.35f;

			Color ballColor = new ColorHsv( hue, saturation, 1f );
			Color ballColor2 = new ColorHsv( (hue + 25f) % 360, saturation, 1f );

			SceneObject.SetValue( "tint", ballColor );
			SceneObject.SetValue( "tint2", ballColor2 );

			isColored = true;
		}

		public void Reset( bool withPop = true )
		{
			if ( withPop )
				Pop();

			if ( Controller == ControlType.Player )
				ReplayData = new ReplayData();
			ActiveTick = 0;
			CheckpointIndex = 0;
			PredictedStart = -1;
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

		[ServerCmd( "reset" )]
		public static void ResetCommand()
		{
			if ( ConsoleSystem.Caller != null && ConsoleSystem.Caller.Pawn is Ball player )
			{
				player.Pop( false );
				player.Reset( false );
			}
		}
	}
}
