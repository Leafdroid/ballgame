
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
		public const float Acceleration = 600f; // yeah
		public const float AirControl = 0.85f; // acceleration multiplier in air
		public const float MaxSpeed = 1100f; // this is the max speed the ball can accelerate to by itself

		public const float Friction = 0.1f;//0.15f; // resistance multiplier on ground
		public const float Viscosity = 3f; // resistance multiplier in water
		public const float Drag = 0.05f; // resistance multiplier in air
		public const float Bounciness = .35f; // elasticity of collisions, aka how much boing 
		public const float Buoyancy = 2.5f; // floatiness

		public const float Mass = 50f; // how heavy!!

		public bool Grounded { get; private set; }

		private void SimulatePhysics()
		{
			Vector3 clampedVelocity = Velocity.WithZ( 0 ).ClampLength( MaxSpeed );
			float directionSpeed = clampedVelocity.Dot( MoveDirection );

			float acceleration = Acceleration;
			if ( !Grounded )
				acceleration *= AirControl;

			float t = 1f - directionSpeed / MaxSpeed;
			acceleration *= t;

			Velocity += MoveDirection * acceleration * Time.Delta;
			Move();
		}

		private void TraceTriggers( out bool fallDamage )
		{
			fallDamage = false;

			TraceResult[] triggerTraces = Trace.Ray( Position, Position )
				.Radius( 40f )
				.HitLayer( CollisionLayer.All, false )
				.HitLayer( CollisionLayer.Trigger, true )
				.RunAll();

			if ( triggerTraces == null )
				return;

			foreach ( var trace in triggerTraces )
			{
				if ( trace.Entity.IsValid() )
				{
					switch ( trace.Entity )
					{
						case FallDamageBrush:
							fallDamage = true;
							break;
						default:
							continue;
					}
				}
			}
		}

		private void Move()
		{
			TraceTriggers( out bool fallDamage );

			float dt = Time.Delta;

			var mover = new MoveHelper( Position, Velocity, this );

			Grounded = mover.TraceDirection( Vector3.Down ).Hit;

			TraceResult waterTrace = Trace.Ray( Position + Vector3.Up * 80f, Position )
				.Radius( 40f )
				.HitLayer( CollisionLayer.All, false )
				//.HitLayer( CollisionLayer.STATIC_LEVEL, false )
				.HitLayer( CollisionLayer.Water, true )
				.Run();

			float friction = Grounded ? Friction : Drag;

			if ( waterTrace.Hit )
			{
				float waterLevel = (waterTrace.EndPos.z - Position.z) * 0.0125f;
				float underwaterVolume = 0.5f - 0.5f * MathF.Cos( MathF.PI * waterLevel );
				mover.Velocity -= PhysicsWorld.Gravity * underwaterVolume * Buoyancy * dt;

				friction = Viscosity * underwaterVolume + friction * (1f - underwaterVolume);
			}

			mover.ApplyFriction( friction, dt );

			mover.Velocity += PhysicsWorld.Gravity * dt;

			mover.TryMove( dt );
			mover.TryUnstuck(); // apparently this isnt needed i think

			TraceResult moveTrace = mover.Trace
				.FromTo( mover.Position, mover.Position + mover.Velocity * dt )
				.Run();

			if ( moveTrace.Hit )
			{
				float hitForce = mover.Velocity.Dot( -moveTrace.Normal );
				PlayImpactSound( hitForce );
			}

			if ( Grounded && fallDamage )
			{
				Delete();
				return;
			}

			Velocity = mover.Velocity;
			Position = mover.Position;

			UpdateModel();

		}

		public void PlayImpactSound( float force )
		{
			if ( IsServer )
				ClientImpactSound( this, force );
			else if ( Local.Client == Owner.Client )
				ImpactSound( force );
		}

		private void ImpactSound( float force )
		{
			if ( force > 150f )
			{
				float scale = (force - 150f) / 1000f;
				float volume = (scale * 1.2f).Clamp( 0f, 1f );
				float pitch = (scale * 3f).Clamp( 0.8f, 0.85f );

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
	}

}
