using Sandbox;
using System;
using System.Linq;

namespace Ballers
{
	public partial class BallPlayer : Player
	{
		public Ball Ball => Ball.Find( Client.NetworkIdent );

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			Ball.RequestBalls();
		}

		public void Kill()
		{
			DamageInfo dmg = new DamageInfo();
			dmg.Damage = Health;
			TakeDamage( dmg );
		}

		public async void RespawnDelay()
		{
			await GameTask.DelaySeconds( .5f );
			Respawn();
		}

		public override void Respawn()
		{
			EnableDrawing = false;
			EnableTraceAndQueries = false;

			Controller = new BallController();
			Camera = new BallCamera();

			EnableAllCollisions = false;
			Transmit = TransmitType.Always;

			Ball.Create( Client );

			base.Respawn();
		}

		public BallPlayer() { }

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( Ball.IsValid() )
				Ball.Simulate();	
		}

		public override void OnKilled()
		{
			base.OnKilled();

			if ( IsServer && Ball.Find( Client.NetworkIdent ).IsValid() )
				Ball.Find( Client.NetworkIdent ).Delete();
		}
	}
}
