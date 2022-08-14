﻿using Sandbox;
using System;
using System.Collections.Generic;

namespace Ballers
{
	public struct MoveHelper
	{
		public Vector3 Position;
		public Vector3 Velocity;

		public Trace Trace;
		public Ball Ball;

		public MoveHelper( Vector3 position, Vector3 velocity, Ball ball ) : this()
		{
			Ball = ball;
			Velocity = velocity;
			Position = position;

			// Hit everything but other balls
			Trace = Trace.Ray(0, 0)
				.WithoutTags("player");
		}

		public TraceResult TraceFromTo( Vector3 start, Vector3 end )
		{
			return Trace.FromTo( start, end ).Run();
		}

		public TraceResult TraceDirection( Vector3 down )
		{
			return TraceFromTo( Position, Position + down );
		}

		public float TryMove( float timestep )
		{
			float travelFraction = 0;

			Surface hitSurface = null;
			Vector3 hitPos = Vector3.Zero;
			float prevDot = 0f;

			using var moveplanes = new VelocityClipPlanes( Velocity, 10 );

			for ( int bump = 0; bump < moveplanes.Max; bump++ )
			{
				if ( Velocity.Length.AlmostEqual( 0.0f ) )
					break;

				bool hitBrush = false;
				foreach ( Mover brush in Mover.All )
				{
					if ( Host.IsServer )
						brush.AtTick( Ball.ActiveTick );
					else
						brush.AtTime( Ball.SimulationTime );

					ModelEntity mover = Host.IsServer ? brush : brush.ClientEntity;

					Vector3 relativeVelocity = Velocity - mover.Velocity;

					Vector3 movePos = Position + relativeVelocity * timestep;

					TraceResult moverTrace = Trace.FromTo( Position, movePos )
					.WithTag( "ladder" )
					.Only( mover )
					.Run();

					if ( moverTrace.Hit )
					{
						float hitDot = MathF.Abs( moverTrace.Normal.Dot( relativeVelocity ) );
						if ( hitDot > prevDot )
						{
							prevDot = hitDot;
							hitSurface = moverTrace.Surface;
							hitPos = moverTrace.EndPosition - moverTrace.Normal * 40f;
						}

						if ( !moveplanes.TryAdd( moverTrace.Normal, mover.Velocity, ref Velocity ) )
						{
							hitBrush = true;
							break;
						}
					}
				}
				if ( hitBrush )
					break;

				TraceResult pm = Trace.FromTo( Position, Position + Velocity * timestep )
				.WithTag("ladder")
				.Run();

				if ( pm.Hit )
				{
					if ( pm.Normal.Dot( -Velocity ) > prevDot )
					{
						prevDot = pm.Normal.Dot( -Velocity );
						hitSurface = pm.Surface;
						hitPos = pm.EndPosition - pm.Normal * 40f;
					}
				}

				if ( pm.StartedSolid )
				{
					Position += pm.Normal * 0.01f;
					continue;
				}

				travelFraction += pm.Fraction;

				if ( pm.Fraction > 0.0f )
				{
					Position = pm.EndPosition + pm.Normal * 0.01f;

					moveplanes.StartBump( Velocity );
				}

				timestep -= timestep * pm.Fraction;

				Vector3 planeVelocity = Vector3.Zero;

				bool hitEntity = pm.Hit && pm.Entity.IsValid();
				if ( hitEntity && !pm.Entity.IsWorld )
				{
					switch ( pm.Entity )
					{
						case BumperBrush bumper:
							bumper.Bonk( Ball, pm.EndPosition - pm.Normal * 40f );
							planeVelocity = pm.Normal * bumper.Force;
							break;
						case var ent:
							planeVelocity = ent.Velocity;
							break;
					}
				}

				if ( !moveplanes.TryAdd( pm.Normal, planeVelocity, ref Velocity ) )
					break;
			}


			Vector3 bumpVelocity = moveplanes.OrginalVelocity - moveplanes.BumpVelocity;
			float bumpForce = bumpVelocity.Length;
			Ball.PlayImpactSound( bumpForce );

			if ( bumpForce > 350f && hitSurface != null && !silentSurfaces.Contains( hitSurface.ResourceName ) )
			{
				string sound = bumpForce > 700f ? hitSurface.Sounds.ImpactHard : hitSurface.Sounds.ImpactSoft;
				PredictionSound.World( Ball.Client, sound, hitPos );
			}

			if ( travelFraction == 0 )
				Velocity = 0;

			return travelFraction;
		}

		private static HashSet<string> silentSurfaces = new HashSet<string>()
		{
			"concrete"
		};

		public void ApplyFriction( float frictionAmount, float delta )
		{
			float StopSpeed = 100.0f;

			var speed = Velocity.Length;
			if ( speed < 0.1f )
			{
				Velocity = 0;
				return;
			}

			// Bleed off some speed, but if we have less than the bleed
			//  threshold, bleed the threshold amount.
			float control = (speed < StopSpeed) ? StopSpeed : speed;

			// Add the amount to the drop amount.
			var drop = control * delta * frictionAmount;

			// scale the velocity
			float newspeed = speed - drop;
			if ( newspeed < 0 ) newspeed = 0;
			if ( newspeed == speed ) return;

			newspeed /= speed;
			Velocity *= newspeed;
		}

		public void TryUnstuck()
		{
			var tr = TraceFromTo( Position, Position );
			if ( !tr.StartedSolid ) return;

			Position += tr.Normal * 1.0f;
			Velocity += tr.Normal * 50.0f;
		}
	}
}
