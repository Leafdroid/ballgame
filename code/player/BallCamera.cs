using System;
using Ballers;

namespace Sandbox
{
	public class BallCamera : Camera
	{
		private float pitch = 0f;
		private float roll = 0f;

		public override void Update()
		{
			if ( Local.Client.Pawn is not BallPlayer player )
				return;

			Ball ball = player.Ball;
			if ( !ball.IsValid() )
				return;

			Vector3 velocity = ball.Velocity;

			float vVel = CurrentView.Rotation.Forward.Dot( velocity );
			float hVel = CurrentView.Rotation.Right.Dot( velocity );

			float vT = vVel / Ball.MaxSpeed;
			float hT = hVel / Ball.MaxSpeed;

			pitch = pitch.LerpTo( vT * 10f, Time.Delta * 10f );
			roll = roll.LerpTo( -hT * 15f, Time.Delta * 10f );

			Rotation = Input.Rotation * Rotation.FromRoll( roll );

			Vector3 camPos = ball.Position + Rotation.Backward * 200;

			TraceResult cameraTrace = Trace.Ray( ball.Position, camPos )
				.Radius( 8f ).WorldOnly().Run();

			Position = cameraTrace.EndPos;

			FieldOfView = 75 + pitch;

			Viewer = null;
		}

		public override void BuildInput( InputBuilder input )
		{
			base.BuildInput( input );
		}
	}
}
