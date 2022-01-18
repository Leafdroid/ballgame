
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class BallDome : ModelEntity
	{
		public const float PopForce = 300f; // pop!
		private TimeSince lifeTime { get; set; } = 0;

		public static readonly SoundEvent Pop = new( "sounds/ball/pop.vsnd" )
		{
			DistanceMax = 1536f,
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

			BallDome dome1 = new BallDome() { Position = pos, Rotation = rot, Velocity = vel };
			dome1.SceneObject.SetValue( "tint2", ball.SceneObject.GetVectorValue( "tint2" ) );
			dome1.PhysicsBody.AngularVelocity = axis * angle * 2f;
			dome1.PhysicsBody.Mass = Ball.Mass * 0.5f;

			vel += rot.Down * PopForce * 2f;
			rot = rot * Rotation.FromPitch( 180f );

			BallDome dome2 = new BallDome() { Position = pos, Rotation = rot, Velocity = vel };
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

			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.Debris );
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

			if ( lifeTime <= 0.25f )
			{
				float t = lifeTime * 3f;
				if ( t > 1f )
					t = 1f;

				Scale = Bezier( 1f, 1.3f, 0.9f, 1f, t );
			}
			else if ( lifeTime > 6 )
			{


				float t = (lifeTime - 6f) * 2.5f;
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
	}
}
