using Sandbox;
using System;
using System.Linq;

namespace Ballers
{
	public partial class BallPlayer : Player
	{
		public Clothing.Container Clothing = new();

		public Ball Ball => Ball.Find( Client.NetworkIdent );

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			Ball.RequestBalls();
		}

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );
			//
			// Use WalkController for movement (you can make your own PlayerController for 100% control)
			//
			Controller = new BallController();

			//
			// Use StandardPlayerAnimator  (you can make your own PlayerAnimator for 100% control)
			//
			Animator = new BallAnimator();

			//
			// Use ThirdPersonCamera (you can make your own Camera for 100% control)
			//
			Camera = new BallCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;
			Transmit = TransmitType.Always;
			EnableTraceAndQueries = false;

			Predictable = true;

			Clothing.LoadFromClient( Client );
			Clothing.DressEntity( this );

			Ball.Create( Client );

			base.Respawn();
		}

		public BallPlayer() { }
		

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( Ball.IsValid() )
				Ball.SimulateInput();	
		}

		public override void OnKilled()
		{
			base.OnKilled();

			if ( IsServer && Ball.Find( Client.NetworkIdent ).IsValid() )
				Ball.Find( Client.NetworkIdent ).Delete();

			EnableDrawing = false;
		}
	}
}
