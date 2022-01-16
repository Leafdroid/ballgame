using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	// TODO: Add clientside duplicate for each client to predict client simulation

	[Library( "func_bumper" )]
	public partial class BumperBrush : BrushEntity
	{
		[Property( "force" )]
		[Net] public float Force { get; private set; }

		[Property( "soundname" )]
		[Net] public string SoundName { get; private set; }

		public TimeSince TimeSinceBonk { get; private set; } = 0f;

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
			Transmit = TransmitType.Always;
		}

		public static readonly SoundEvent BonkSound = new( "sounds/ball/bonk.vsnd" )
		{
			Pitch = 1f,
			PitchRandom = 0.05f,
			Volume = 1f,
			DistanceMax = 2048f,
		};

		public void Bonk( Ball bonker, Vector3 pos )
		{
			if ( IsServer )
				ClientImpactSound( this, bonker, pos );
			else if ( Local.Client == bonker.Owner.Client )
				ImpactSound( pos );
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( IsClient && clientModel != null )
				clientModel.Delete();
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
			if ( clientModel == null )
				return;

			float scale;

			if ( TimeSinceBonk < 0.2f )
			{
				scale = Bezier( 1f, 1.2f, 0.9f, 1f, TimeSinceBonk * 5f );
			}
			else
				scale = 1f;

			if ( scale != clientModel.Transform.Scale )
				clientModel.Transform = new Transform( Position, Rotation, scale );
		}

		private void ImpactSound( Vector3 pos )
		{
			if ( SoundName != null )
				Sound.FromWorld( SoundName, pos );
			else
				Sound.FromWorld( BonkSound.Name, pos );

			TimeSinceBonk = 0f;
		}

		[ClientRpc]
		public static void ClientImpactSound( BumperBrush bumper, Ball ball, Vector3 pos )
		{
			if ( ball.IsValid() && (ball.Owner == null || ball.Owner.Client != Local.Client) )
				bumper.ImpactSound( pos );
		}

		private void SharedSpawn()
		{
			EnableDrawing = false;
			ClearCollisionLayers();
			RemoveCollisionLayer( CollisionLayer.All );
			AddCollisionLayer( CollisionLayer.STATIC_LEVEL );
		}

		SceneObject clientModel;

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();

			clientModel = new SceneObject( Model, Transform );
		}
	}
}
