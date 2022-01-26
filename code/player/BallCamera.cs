using System;
using Ballers;

namespace Sandbox
{
	public class BallCamera : Camera
	{
		private float fov = 0f;
		private float roll = 0f;

		private int zoom = 20;

		private const int minZoom = 10;
		private const int maxZoom = 30;

		private static Trace cameraTrace = Trace.Ray( 0, 0 )
			.Radius( 10f )
			.HitLayer( CollisionLayer.Debris, false );

		public override void Update()
		{
			if ( Local.Client.Pawn is not Ball player )
				return;

			zoom -= Input.MouseWheel;
			zoom = zoom.Clamp( minZoom, maxZoom );

			Vector3 velocity = player.Velocity;

			float vVel = Rotation.Forward.Dot( velocity );
			float hVel = Rotation.Right.Dot( velocity );

			float vT = vVel / Ball.MaxSpeed;
			float hT = hVel / Ball.MaxSpeed;

			fov = fov.LerpTo( vT * 10f, Time.Delta * 10f );
			roll = roll.LerpTo( -hT * 15f, Time.Delta * 10f );

			Rotation = Rotation.From( Input.Rotation.Pitch(), Input.Rotation.Yaw(), roll );

			Vector3 camPos = player.Position + (Rotation.Backward * 10 * zoom + Rotation.Up * 0.5f * zoom);

			Position = cameraTrace.FromTo( player.Position, camPos ).Run().EndPos;

			FieldOfView = 75 + fov;

			Viewer = null;
		}

		public override void BuildInput( InputBuilder input )
		{
			base.BuildInput( input );
		}
	}
}
