
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

			Vector3 clampedVelocity = Velocity.WithZ( 0 ).ClampLength( MaxSpeed );
			float directionSpeed = clampedVelocity.Dot( MoveDirection );

			float acceleration = Acceleration;
			if ( !Grounded )
				acceleration *= AirControl;

			float t = 1f - directionSpeed / MaxSpeed;
			acceleration *= t;

			Velocity += MoveDirection * acceleration * dt;
			//Velocity = Velocity.WithZ( 0 ).ClampLength( MaxSpeed ).WithZ( Velocity.z );
			Move();

		}

		public void Move()
		{
			float dt = Time.Delta;

			var mover = new MoveHelper( Position, Velocity );

			Grounded = mover.TraceDirection( Vector3.Down ).Hit;

			float friction = Grounded ? Friction : Drag;

			mover.ApplyFriction( friction, dt );

			mover.Velocity += PhysicsWorld.Gravity * dt;

			mover.TryMove( dt );
			mover.TryUnstuck();

			TraceResult moveTrace = mover.Trace
				.HitLayer( CollisionLayer.LADDER, true )
				.FromTo( mover.Position, mover.Position + mover.Velocity * dt )
				.Run();

			if ( moveTrace.Hit )
			{
				float hitForce = mover.Velocity.Dot( -moveTrace.Normal );
				if ( IsServer )
					ClientImpactSound( this, hitForce );
				else if ( Local.Client == Owner.Client )
					ImpactSound( hitForce );

				/* silly popping on big bang
				if ( hitForce > 250f )
				{
					if ( Host.IsServer )
					{
						(Owner as BallPlayer).Kill();
					}
					else if ( Owner == Local.Client.Pawn && !popped )
					{
						Ragdoll();

						if ( Terry.IsValid() )
							Terry.Delete();

						//Sound.FromWorld( WilhelmScream.Name, Position );
						BallGib.Create( this );
						popped = true;
					}
				}
				*/
			}

			Velocity = mover.Velocity;
			Position = mover.Position;

			UpdateModel();
		}

		private void ImpactSound( float force )
		{
			if ( force > 150f )
			{
				float volume = ((force - 150f) / (MaxSpeed - 150f) * 1.2f).Clamp( 0f, 1f );
				float pitch = ((force - 150f) / (MaxSpeed - 150f) * 3f).Clamp( 0.8f, 0.85f );

				Sound impactSound = PlaySound( BounceSound.Name );
				impactSound.SetVolume( volume );
				impactSound.SetPitch( pitch );
			}
		}

		[ClientRpc]
		public static void ClientImpactSound( Ball ball, float force )
		{
			if ( ball.IsValid() && (ball.Owner == null || ball.Owner.Client != Local.Client) )
				ball.ImpactSound( force );
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
		public static Trace Only( this Trace trace, Entity entity )
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
			return trace.WithTag( "" );
		}

		public static Trace IgnoreMovingBrushes( this Trace trace )
		{
			string[] tags = new string[MovingBrush.All.Count];

			int index = 0;
			foreach ( MovingBrush brush in MovingBrush.All )
			{
				string tag = $"MovingBrush:{brush.NetworkIdent}";
				tags[index] = tag;
				if ( !brush.Tags.Has( tag ) )
					brush.Tags.Add( tag );
			}

			return trace.WithoutTags( tags );
		}
	}

}
