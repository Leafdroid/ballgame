
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
		public bool Grounded;

		public void SimulatePhysics()
		{
			float dt = Time.Delta;

			float directionSpeed = Velocity.Dot( MoveDirection );

			float acceleration = Acceleration;
			if ( !Grounded )
				acceleration *= AirControl;

			float t = 1f - directionSpeed / MaxSpeed;
			acceleration *= t;

			Velocity += (MoveDirection * acceleration) * dt;
			Move();
			Velocity = Velocity.WithZ( 0 ).ClampLength( MaxSpeed ).WithZ( Velocity.z );
		}

		public void Move()
		{
			var mover = new MoveHelper( Position, Velocity );

			float friction = Drag;
			TraceResult groundTrace = mover.TraceDirection( Vector3.Down );
			if ( Grounded = groundTrace.Hit )
				friction = Friction;

			/*
			foreach ( MoveLinear platform in MoveLinear.All )
			{
				Vector3 relativeVelocity = platform.Velocity - Velocity;

				Vector3 moveTo = Position - relativeVelocity * Time.Delta;
				TraceResult pTrace = Trace.Ray( Position, moveTo )
					.Only( platform ).Radius( 40f ).Run();

				if ( pTrace.Hit )
				{
					float hitForce = pTrace.Normal.Dot( relativeVelocity );

					DebugOverlay.Text( platform.Position + Vector3.Up * 64f, hitForce.ToString() );
					DebugOverlay.Sphere( Position, 40f, Color.Red );

					if ( hitForce > 0 )
						mover.Position = pTrace.EndPos + pTrace.Normal;
					mover.Velocity += pTrace.Normal * hitForce;
				}
			}
			*/

			mover.ApplyFriction( friction, Time.Delta );

			// Apply gravity
			mover.Velocity += PhysicsWorld.Gravity * Time.Delta;

			mover.TryMove( Time.Delta );
			mover.TryUnstuck();

			TraceResult moveTrace = mover.TraceDirection( mover.Velocity * Time.Delta );
			
			if ( moveTrace.Hit )
			{
				float hitForce = mover.Velocity.Dot( -moveTrace.Normal );
				if ( IsServer )
					ClientImpactSound( this, hitForce );
				else if (Local.Client == Owner.Client)
					ImpactSound( hitForce );
			}

			Velocity = mover.Velocity;
			Position = mover.Position;

			UpdateModel();
		}

		private void ImpactSound(float force)
		{
			if ( force > 150f )
			{
				float volume = ((force - 150f) / (MaxSpeed - 150f)).Clamp( 0f, 1f );

				Sound impactSound = PlaySound( BounceSound.Name );
				impactSound.SetVolume( volume );
			}
		}

		[ClientRpc]
		public static void ClientImpactSound( Ball ball, float force )
		{
			if ( ball.IsValid() && ball.Owner.Client != Local.Client )
			{
				if ( force > 150f )
				{
					float volume = ((force - 150f) / (MaxSpeed - 150f)).Clamp( 0f, 1f );

					Sound impactSound = ball.PlaySound( BounceSound.Name );
					impactSound.SetVolume( volume );
				}
			}
		}

		public static readonly SoundEvent BounceSound = new()
		{
			Sounds = new List<string> {
			"sounds/ball/bounce1.vsnd",
			"sounds/ball/bounce2.vsnd",
			"sounds/ball/bounce3.vsnd",
			},
			Pitch = 1f,
			PitchRandom = 0.1f,
			Volume = 1f,
			DistanceMax = 2048f,
		};

		public static readonly SoundEvent RollingSoundEvent = new( "sounds/ball/shitroll.vsnd" )
		{
			DistanceMax = 1024f,
			Pitch = 3f,
			Volume = 0.05f,
		};
	}

	public static class TraceExtensions
	{
		public static Trace Only(this Trace trace, Entity entity)
		{
			if ( entity.IsValid() )
			{
				string idTag = $"ID:{entity.NetworkIdent}";
				if ( !entity.Tags.Has( idTag ) )
					entity.Tags.Add( idTag );

				// only hit specified entity
				return trace.EntitiesOnly().WithTag( idTag );
			}
			
			// hit no entities if specified entity is invalid
			return trace.WithTag("");
		}
	}

}
