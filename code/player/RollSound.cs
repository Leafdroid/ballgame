
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class RollSound
	{
		private float groundTime = 0f;
		private Ball ball;
		private Sound sound;
		private static SoundEvent soundEvent = new SoundEvent( "sounds/ball/roll.vsnd" )
		{
			DistanceMax = 1024f,
		};

		public RollSound( Ball ball )
		{
			this.ball = ball;
			//Respawn();
		}

		public void Respawn()
		{
			sound.Stop();
			//sound = ball.PlaySound( soundEvent.Name );
		}

		public static void RespawnPredicted( Ball ball )
		{
			if ( Host.IsServer )
				RespawnRpc( ball );
			else if ( ball.Client == Local.Client )
				ball.RollSound.Respawn();
		}

		[ClientRpc]
		public static void RespawnRpc( Ball ball )
		{
			if ( ball.Client != Local.Client )
				ball.RollSound.Respawn();
		}

		public void Update( bool grounded, float speed )
		{
			groundTime += (grounded ? 10f : -4f) * Time.Delta;
			groundTime = groundTime < 0f ? 0f : groundTime > 1f ? 1f : groundTime;

			float speedT = speed * 0.001f;
			speedT = speedT < 0f ? 0f : speedT > 1f ? 1f : speedT;

			float t = groundTime * speedT;
			float pitch = t < 0.25f ? 0.25f : t > 0.75f ? 0.75f : t;
			sound.SetVolume( t );
			sound.SetPitch( pitch );
		}

		public static void UpdatePredicted( Ball ball, bool grounded, float speed )
		{
			if ( Host.IsServer )
				UpdateRpc( ball, grounded, speed );
			else if ( ball.Client == Local.Client )
				ball.RollSound.Update( grounded, speed );
		}

		[ClientRpc]
		public static void UpdateRpc( Ball ball, bool grounded, float speed )
		{
			if ( ball.Client != Local.Client )
				ball.RollSound.Update( grounded, speed );
		}
	}
}
