using System;
using Ballers;

namespace Sandbox
{
	public class BallCamera : Camera
	{
		private float pitch = 0f;
		private float roll = 0f;
		private float stateInterp = 0f;

		private Vector3 idlePos = Vector3.Zero;
		private Rotation idleRot = Rotation.From(new Angles(0,0,0));
		private float idleFov = 80f;

		private Vector3 lastBallPos;
		private Rotation lastBallRot;
		private float lastBallFov;

		public override void Update()
		{
			Ball ball = Ball.Find( Local.Client );

			bool ballExists = ball.IsValid();

			stateInterp = stateInterp.LerpTo( ballExists ? -0.01f : 1.01f, Time.Delta * 3f ).Clamp( 0f, 1f );

			idlePos = new Vector3(-1800,-500,700);
			idleRot = Rotation.From( new Angles( 25, 20, 0 ) );
			idleFov = 80f;

			Vector3 mins = Vector3.One*10000f;
			Vector3 maxs = -mins;
			foreach (Ball curBall in Ball.All)
			{
				if ( curBall == ball )
					continue;

				Vector3 pos = curBall.Model.Position;

				if ( pos.x < mins.x )
					mins.x = pos.x;
				else if ( pos.x > maxs.x )
					maxs.x = pos.x;

				if ( pos.y < mins.y )
					mins.y = pos.y;
				if ( pos.y > maxs.y )
					maxs.y = pos.y;

				if ( pos.z < mins.z )
					mins.z = pos.z;
				if ( pos.z > maxs.z )
					maxs.z = pos.z;
			}

			if ( ballExists )
			{
				ModelEntity model = ball.Model;

				Vector3 velocity = ball.Velocity;

				float vVel = CurrentView.Rotation.Forward.Dot( velocity );
				float hVel = CurrentView.Rotation.Right.Dot( velocity );

				float vT = vVel / ball.MaxSpeed;
				float hT = hVel / ball.MaxSpeed;

				pitch = pitch.LerpTo( vT * 10f, Time.Delta * 10f );
				roll = roll.LerpTo( -hT * 15f, Time.Delta * 10f );

				lastBallRot = Input.Rotation * Rotation.FromRoll( roll );
				Vector3 camPos = model.Position + lastBallRot.Backward * 200;

				TraceResult cameraTrace = Trace.Ray( model.Position, camPos )
					.Radius( 8f ).WorldOnly().Run();

				lastBallPos = cameraTrace.EndPos;

				lastBallFov = 75 + pitch;

				Viewer = null;
			}

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
