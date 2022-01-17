using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_bumper" )]
	public partial class BumperBrush : BrushEntity
	{
		[Property( "force" )]
		[Net] public float Force { get; private set; }

		[Property( "pitch" )]
		[Net] public float Pitch { get; private set; } = 1f;

		[Property( "soundname" )]
		[Net] public string SoundName { get; private set; }

		private TimeSince timeSinceBonk = 0f;
		private bool justBonked = false;

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
			Transmit = TransmitType.Always;
		}

		public static readonly SoundEvent BoingSound = new( "sounds/ball/boing.vsnd" )
		{
			Pitch = 0.95f,
			PitchRandom = 0.05f,
			Volume = 0.65f,
			DistanceMax = 2048f,
		};

		public void Bonk( Ball bonker, Vector3 pos )
		{
			if ( IsServer )
				ClientImpactSound( this, bonker, pos );
			else if ( Local.Pawn == bonker.Owner )
				ImpactSound( pos );
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
		public void Frame()
		{
			if ( SceneObject == null )
				return;

			if ( justBonked )
			{
				timeSinceBonk = 0f;
				justBonked = false;
			}

			float scale;

			if ( timeSinceBonk < 0.2f )
				scale = Bezier( 1f, 1.2f, 0.95f, 1f, timeSinceBonk * 5f );
			else
				scale = 1f;

			if ( scale != SceneObject.Transform.Scale )
				SceneObject.Transform = new Transform( Position, Rotation, scale );
		}

		private void ImpactSound( Vector3 pos )
		{
			justBonked = true;

			if ( SoundName != null )
				Sound.FromWorld( SoundName, pos ).SetPitch( Pitch );
			else
				Sound.FromWorld( BoingSound.Name, pos ).SetPitch( Pitch );
		}

		[ClientRpc]
		public static void ClientImpactSound( BumperBrush bumper, Ball ball, Vector3 pos )
		{
			if ( ball.IsValid() && ball.Owner != Local.Pawn )
			{
				bumper.ImpactSound( pos );
			}
		}

		private void SharedSpawn()
		{
			EnableDrawing = true;
			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.STATIC_LEVEL );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
