using System;
using Ballers;

namespace Sandbox
{
	public class BallCamera : Camera
	{
		private float pitch = 0f;
		private float roll = 0f;
		private float stateInterp = 0f;

		private Vector3 idlePos = new Vector3( -1800, -500, 700 );
		private Rotation idleRot = Rotation.From( new Angles( 25, 20, 0 ) );
		private float idleFov = 75f;

		private Vector3 lastBallPos;
		private Rotation lastBallRot;
		private float lastBallFov;

		public override void Update()
		{
			//if ( Host.IsServer )
				//return;
			
			if ( Local.Client.Pawn is not BallPlayer player )
				return;

			Ball ball = player.Ball;
			bool ballValid = ball.IsValid();
			stateInterp = stateInterp.LerpTo( ballValid ? -0.01f : 1.01f, Time.Delta * 10f ).Clamp( 0f, 1f );
			if ( !ballValid )
				return;

			Vector3 velocity = ball.Velocity;

			float vVel = CurrentView.Rotation.Forward.Dot( velocity );
			float hVel = CurrentView.Rotation.Right.Dot( velocity );

			float vT = vVel / Ball.MaxSpeed;
			float hT = hVel / Ball.MaxSpeed;

			pitch = pitch.LerpTo( vT * 10f, Time.Delta * 10f );
			roll = roll.LerpTo( -hT * 15f, Time.Delta * 10f );

			lastBallRot = Input.Rotation * Rotation.FromRoll( roll );
			Vector3 camPos = ball.Position + lastBallRot.Backward * 200;

			TraceResult cameraTrace = Trace.Ray( ball.Position, camPos )
				.Radius( 8f ).WorldOnly().Run();

			lastBallPos = cameraTrace.EndPos;

			lastBallFov = 75 + pitch;

			Viewer = null;

			float n = 1f - stateInterp;
			Position = n * lastBallPos + stateInterp * idlePos;
			Rotation = Rotation.Lerp( lastBallRot, idleRot, stateInterp );
			FieldOfView = n * lastBallFov + stateInterp * idleFov;
		}

		public override void BuildInput( InputBuilder input )
		{
			base.BuildInput( input );
		}
	}
}
