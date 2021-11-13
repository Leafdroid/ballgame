
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ballers
{
	public partial class Ball
	{
		public bool Grounded;
		public Sound RollingSound;
		private float soundInterp = 0f;

		public void PreStep()
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

			if ( Host.IsClient )
				SendData( NetworkIdent, Position, Velocity );
		}

		public void Move()
		{
			var mover = new BallMoveHelper( Position, Velocity );
			mover.Trace = mover.Trace.Radius( 40f ).WorldOnly();
			mover.MaxStandableAngle = 50.0f;
			mover.GroundBounce = FloorBounce;
			mover.WallBounce = WallBounce;

			float friction = Drag;
			TraceResult groundTrace = mover.TraceDirection( Vector3.Down );
			if ( Grounded = groundTrace.Hit )
				friction = Friction;

			mover.ApplyFriction( friction, Time.Delta );

			// Apply gravity
			mover.Velocity += Vector3.Down * 800 * Time.Delta;

			mover.TryMove( Time.Delta );
			mover.TryUnstuck();

			if (IsClient)
			{
				TraceResult moveTrace = mover.TraceDirection( mover.Velocity * Time.Delta  );
				if ( moveTrace.Hit )
				{
					float hitForce2 = mover.Velocity.Dot( -moveTrace.Normal );
					if ( hitForce2 > 150f )
					{
						float volume = ((hitForce2 - 150f) / (MaxSpeed - 150f)).Clamp( 0f, 1f );

						Sound impactSound =	Sound.FromWorld( BounceSounds.Name, moveTrace.EndPos );
						impactSound.SetVolume( volume );
					}
				}
			}

			Velocity = mover.Velocity;
			Position = mover.Position;

			if ( IsServer )
				NetData( NetworkIdent, Position, Velocity );

			if ( IsClient  )
			{
				UpdateModel();
				soundInterp = soundInterp.LerpTo( Grounded ? 1.1f : -0.1f, 0.25f );
				soundInterp = soundInterp.Clamp( 0f, 1f );

				float speed = Velocity.WithZ( 0 ).Length / MaxSpeed;
				float volume = speed * 0.45f * soundInterp;
				RollingSound.SetVolume( volume );
				float pitch = soundInterp+speed*10f;
				RollingSound.SetPitch( pitch );
			}
		}

		public static readonly SoundEvent BounceSounds = new()
		{
			Sounds = new List<string> {
			"sounds/ball/bounce1.vsnd",
			"sounds/ball/bounce2.vsnd",
			"sounds/ball/bounce3.vsnd",
			},
			Pitch = 1f,
			PitchRandom = 0.1f,
			Volume = 1f,
			DistanceMax = 3072f,
		};

		public static readonly SoundEvent RollingSoundEvent = new( "sounds/ball/shitroll.vsnd" )
		{
			DistanceMax = 512f,
			Pitch = 3f,
			Volume = 0.05f,
		};
	}
}
