﻿using System;
using System.Buffers;

namespace Ballers
{
	/// <summary>
	/// Used to store a list of planes that an object is going to hit, and then
	/// remove velocity from them so the object can slide over the surface without
	/// going through any of the planes.
	/// </summary>
	public struct VelocityClipPlanes : IDisposable
	{
		public Vector3 OrginalVelocity { get; private set; }
		public Vector3 BumpVelocity { get; private set; }
		Vector3[] Planes;

		/// <summary>
		/// Maximum number of plasnes that can be hit
		/// </summary>
		public int Max { get; private set; }

		/// <summary>
		/// Number of planes we're currently holding
		/// </summary>
		public int Count { get; private set; }

		public VelocityClipPlanes( Vector3 originalVelocity, int max = 10 )
		{
			Max = max;
			OrginalVelocity = originalVelocity;
			BumpVelocity = originalVelocity;
			Planes = ArrayPool<Vector3>.Shared.Rent( max );
			Count = 0;
		}

		/// <summary>
		/// Try to add this plane and restrain velocity to it (and its brothers)
		/// </summary>
		/// <returns>False if we ran out of room and should stop adding planes</returns>
		public bool TryAdd( Vector3 normal, Vector3 planeVelocity, ref Vector3 velocity )
		{
			if ( Count == Max )
			{
				velocity = 0;
				return false;
			}

			Planes[Count++] = normal;

			//
			// if we only hit one plane then apply the bounce
			//
			if ( Count == 1 )
			{
				//	BumpVelocity = velocity;
				BumpVelocity = ClipVelocity( BumpVelocity, planeVelocity, normal );
				velocity = BumpVelocity;

				return true;
			}

			//
			// clip to all of the planes we've put in
			//
			velocity = BumpVelocity;
			if ( TryClip( ref velocity, planeVelocity ) )
			{
				// Hit the floor and the wall, go along the join
				if ( Count == 2 )
				{
					var dir = Vector3.Cross( Planes[0], Planes[1] );
					velocity = dir.Normal * dir.Dot( velocity );
				}
				else
				{
					velocity = Vector3.Zero;
					return true;
				}
			}

			//
			// We're moving in the opposite direction to our 
			// original intention so just stop right there.
			//
			if ( velocity.Dot( OrginalVelocity ) < 0 )
			{
				velocity = 0;
			}

			return true;
		}

		/// <summary>
		/// Try to clip our velocity to all the planes, so we're not travelling into them
		/// Returns true if we clipped properly
		/// </summary>
		bool TryClip( ref Vector3 velocity, Vector3 planeVelocity )
		{
			for ( int i = 0; i < Count; i++ )
			{
				velocity = ClipVelocity( BumpVelocity, planeVelocity, Planes[i] );

				if ( MovingTowardsAnyPlane( velocity, i ) )
					return false;
			}

			return true;
		}

		/// <summary>
		/// Returns true if we're moving towards any of our planes (except for skip)
		/// </summary>
		bool MovingTowardsAnyPlane( Vector3 velocity, int iSkip )
		{
			for ( int j = 0; j < Count; j++ )
			{
				if ( j == iSkip ) continue;
				if ( velocity.Dot( Planes[j] ) < 0 ) return false;
			}

			return true;
		}

		/// <summary>
		/// Start a new bump. Clears planes and resets BumpVelocity
		/// </summary>
		public void StartBump( Vector3 velocity )
		{
			BumpVelocity = velocity;
			Count = 0;
		}

		/// <summary>
		/// Clip the velocity to the normal
		/// </summary>
		Vector3 ClipVelocity( Vector3 vel, Vector3 planeVelocity, Vector3 norm )
		{
			float planeSpeed = planeVelocity.Dot( -norm );

			float backoff = Vector3.Dot( vel, norm ) * (1f + Ball.Bounciness);
			float toClip = backoff + planeSpeed;

			float normVel = vel.Distance( norm );

			if ( normVel - backoff < toClip )
				toClip -= (normVel - backoff);

			if ( toClip > 0 )
				return vel;

			var o = vel - (norm * toClip);

			return o;
		}

		public void Dispose()
		{
			ArrayPool<Vector3>.Shared.Return( Planes );
		}
	}
}
