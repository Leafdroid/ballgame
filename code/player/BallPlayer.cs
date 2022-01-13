using Sandbox;
using System;
using System.Linq;

namespace Ballers
{
	public partial class BallPlayer : Player
	{
		[Net] public Ball Ball { get; set; }

		public void Kill()
		{
			DamageInfo dmg = new DamageInfo();
			dmg.Damage = Health;
			TakeDamage( dmg );
		}

		public override void Respawn()
		{
			base.Respawn();

			EnableDrawing = false;

			Ball = Ball.Create( Client );
			Controller = new BallController();
			Camera = new BallCamera();

			EnableAllCollisions = false;
			EnableTraceAndQueries = false;
			Transmit = TransmitType.Always;
		}

		public BallPlayer() { }

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( Position.z < -512 )
			{
				Kill();
				return;
			}

			if ( Ball.IsValid() )
				Ball.Simulate();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			if ( Ball.IsValid() )
				Ball.Delete();
		}
	}
}
