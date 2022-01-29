
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{

	public enum GravityType
	{
		Default,
		Magnet,
		Manipulated
	}

	public partial class Ball : Player
	{
		public const float Acceleration = 700f; // yeah
		public const float AirControl = 0.85f; // acceleration multiplier in air
		public const float MaxSpeed = 1100f; // this is the max speed the ball can accelerate to by itself

		public const float Friction = 0.1f;//0.15f; // resistance multiplier on ground
		public const float Viscosity = 3f; // resistance multiplier in water
		public const float Drag = 0.05f; // resistance multiplier in air
		public const float Bounciness = .35f; // elasticity of collisions, aka how much boing 
		public const float Buoyancy = 2.5f; // floatiness

		public const float Mass = 50f; // how heavy!!

		public Vector3 GetGravity()
		{
			if ( GravityType == GravityType.Default )
				return PhysicsWorld.Gravity;
			else
				return GravityRotation.Forward * PhysicsWorld.Gravity.Length;
		}

		[Net, Predicted] public GravityType GravityType { get; private set; }
		[Net, Predicted] public Rotation GravityRotation { get; private set; }
		[Net, Predicted] public bool Grounded { get; private set; }

		private void SimulatePhysics()
		{
			Vector3 flatVelocity = Velocity - GetGravity().Normal * Velocity.Dot( GetGravity().Normal );
			Vector3 clampedVelocity = flatVelocity.ClampLength( MaxSpeed );
			float directionSpeed = clampedVelocity.Dot( MoveDirection );

			float acceleration = Acceleration;
			if ( !Grounded )
				acceleration *= AirControl;

			float t = 1f - directionSpeed / MaxSpeed;
			acceleration *= t;

			Velocity += MoveDirection * acceleration * Time.Delta;

			Move();

			RollSound.UpdatePredicted( this, Grounded, Velocity.Length );

			UpdateModel();
		}

		private void TraceTriggers( Trace moveTrace, out bool fallDamage )
		{
			fallDamage = false;

			TraceResult[] triggerTraces = moveTrace
				.FromTo( Position, Position + Velocity * Time.Delta )
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
						case HurtBrush hurtBrush:
							/*
							if ( hurtBrush.RequireCollision )
								fallDamage = true;
							else
								Pop();
							*/
							break;
						case CheckpointBrush checkPoint:
							checkPoint.Trigger( this, trace.Fraction );
							break;
						default:
							continue;
					}
				}
			}
		}

		private void Move()
		{
			float dt = Time.Delta;

			var mover = new MoveHelper( Position, Velocity, this );

			Vector3 gravityNormal = GetGravity().Normal;
			Grounded = mover.TraceDirection( gravityNormal ).Hit;

			Vector3 flatVelocity = Velocity - gravityNormal * Velocity.Dot( gravityNormal );
			float speedFraction = flatVelocity.Length / MaxSpeed;
			if ( speedFraction > 1f )
				speedFraction = 1f;

			TraceResult groundTrace = mover.TraceDirection( gravityNormal * 16f + speedFraction * 24f );
			if ( groundTrace.Hit )
			{
				string surface = groundTrace.Surface.Name;

				Rotation want = Rotation.LookAt( -groundTrace.Normal, GravityRotation.Up );

				switch ( surface )
				{
					case "magnet":
						GravityType = GravityType.Magnet;
						GravityRotation = Rotation.Slerp( GravityRotation, want, Time.Delta * 6f );
						break;
					case "gravity":
						GravityType = GravityType.Manipulated;
						GravityRotation = Rotation.Slerp( GravityRotation, want, Time.Delta * 6f );
						break;
					default:
						if ( GravityType != GravityType.Manipulated )
						{
							GravityRotation = Rotation.Slerp( GravityRotation, Rotation.LookAt( Vector3.Down ), Time.Delta * 10f );

							GravityType = GravityType.Default;
						}
						break;
				}
			}
			else if ( GravityType == GravityType.Magnet )
				GravityType = GravityType.Default;

			TraceResult waterTrace = Trace.Ray( Position + Vector3.Up * 80f, Position )
				.Radius( 40f )
				.HitLayer( CollisionLayer.All, false )
				.HitLayer( CollisionLayer.Water, true )
				.Run();

			float friction = Grounded ? Friction : Drag;

			if ( waterTrace.Hit )
			{
				float waterLevel = (waterTrace.EndPos.z - Position.z) * 0.0125f;
				float underwaterVolume = 0.5f - 0.5f * MathF.Cos( MathF.PI * waterLevel );
				mover.Velocity -= GetGravity() * underwaterVolume * Buoyancy * dt;

				friction = Viscosity * underwaterVolume + friction * (1f - underwaterVolume);
			}

			mover.ApplyFriction( friction, dt );

			if ( ConsoleSystem.GetValue( "sv_cheats" ) == "1" && Input.Down( InputButton.Jump ) )
				mover.Velocity -= GetGravity() * dt;
			else
				mover.Velocity += GetGravity() * dt;

			mover.TryMove( dt );
			//mover.TryUnstuck();

			TraceResult moveTrace = mover.Trace
				.FromTo( mover.Position, mover.Position + mover.Velocity * dt )
				.Run();

			Velocity = mover.Velocity;
			Position = mover.Position;

			TraceTriggers( mover.Trace, out bool fallDamage );
			if ( fallDamage && (waterTrace.Hit || moveTrace.Hit) )
			{
				Pop();
				return;
			}
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
			if ( force > 175f )
			{
				float scale = (force - 175f) / 2250f;
				float volume = (scale * 1.2f).Clamp( 0f, 1f );
				float pitch = (scale * 3f).Clamp( 0.75f, 0.85f );

				Sound impactSound = PlaySound( BounceSound.Name );
				impactSound.SetVolume( volume );
				impactSound.SetPitch( pitch );
			}
		}

		[ClientRpc]
		public static void ClientImpactSound( Ball ball, float force )
		{
			if ( ball.Client != Local.Client || ball.Controller == ControlType.Replay )
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
