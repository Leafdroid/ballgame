
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class BallGib : ModelEntity
	{
		public const float PopForce = 300f; // pop!
		private TimeSince LifeTime { get; set; } = 0;

		public static readonly SoundEvent Pop = new( "sounds/ball/pop.vsnd" )
		{
			DistanceMax = 1536f,
		};

		public static readonly SoundEvent Click = new( "sounds/ball/click.vsnd" )
		{
			DistanceMax = 256f,
			Volume = 0.8f
		};


		public static void Create( Ball ball )
		{
			if ( Host.IsServer )
				return;

			if ( !ball.IsValid() || !ball.SceneObject.IsValid() )
				return;

			Vector3 pos = ball.Position;
			Rotation rot = ball.Rotation;
			Vector3 vel = ball.Velocity + rot.Up * PopForce;

			var dir = ball.Velocity.Normal;
			var axis = new Vector3( -dir.y, dir.x, 0.0f );
			var angle = ball.Velocity.Length / (40.0f * (float)Math.PI);

			Sound.FromWorld( Pop.Name, ball.Position );

			BallGib dome1 = new BallGib() { Position = pos, Rotation = rot, Velocity = vel };
			dome1.SceneObject.SetValue( "tint2", ball.SceneObject.GetVectorValue( "tint2" ) );
			dome1.PhysicsBody.AngularVelocity = axis * angle * 2f;
			dome1.PhysicsBody.Mass = Ball.Mass * 0.5f;

			vel += rot.Down * PopForce * 2f;
			rot = rot * Rotation.FromPitch( 180f );

			BallGib dome2 = new BallGib() { Position = pos, Rotation = rot, Velocity = vel };
			dome2.SceneObject.SetValue( "tint2", ball.SceneObject.GetVectorValue( "tint" ) );
			dome2.PhysicsBody.AngularVelocity = axis * angle * 2f;
			dome2.PhysicsBody.Mass = Ball.Mass * 0.5f;
		}

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "models/ball.vmdl" );
			SetBodyGroup( 0, 1 );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

			PhysicsBody.DragEnabled = false;
			PhysicsBody.AngularDamping = 0;
			PhysicsBody.AngularDrag = 0;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			// make fancy blink sprite thing when shrinking out of existance
		}

		private float Bezier( float a, float b, float c, float t )
		{
			float n = 1f - t;
			float d = a * n + b * t;
			float e = b * n + c * t;
			return d * n + e * t;
		}
		private float Bezier( float a, float b, float c, float d, float t )
		{
			float n = 1f - t;
			float ab = a * n + b * t;
			float bc = b * n + c * t;
			float cd = c * n + d * t;
			float abbc = ab * n + bc * t;
			float bccd = bc * n + cd * t;
			return abbc * n + bccd * t;
		}


		[Event.Frame]
		public void FrameSimulate()
		{
			//DebugOverlay.Sphere( PhysicsBody.MassCenter, 8f, Color.White );

			if ( LifeTime <= 0.25f )
			{
				float t = LifeTime * 3f;
				if ( t > 1f )
					t = 1f;

				Scale = Bezier( 1f, 1.3f, 0.9f, 1f, t );
			}
			else if ( LifeTime > 6 )
			{


				float t = (LifeTime - 6f) * 2.5f;
				if ( t > 1f )
					t = 1f;

				Scale = Bezier( 1f, 1.1f, 1.2f, 0f, t );

				if ( t == 1f && EnableDrawing )
				{
					//Sound.FromWorld( Click.Name, Position );
					EnableDrawing = false;
					Delete();
				}
			}
		}

		[ClientCmd( "gibby" )]
		public static void SpawnGib()
		{
			BallPlayer player = Local.Client.Pawn as BallPlayer;

			if ( player.IsValid() && player.Ball.IsValid() )
				BallGib.Create( player.Ball );
		}
	}
}
