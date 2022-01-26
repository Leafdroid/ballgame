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

			Vector3 position = player.Position;
			if ( player.LifeState == LifeState.Dead )
			{
				Transform bone = player.TerryRagdoll.GetBoneTransform( 0 );
				position = bone.Position + Vector3.Up * 8f;
			}

			Vector3 velocity = player.LifeState == LifeState.Alive ? player.Velocity : Vector3.Zero;

			float vVel = Rotation.Forward.Dot( velocity );
			float hVel = Rotation.Right.Dot( velocity );

			float vT = vVel / Ball.MaxSpeed;
			float hT = hVel / Ball.MaxSpeed;

			fov = fov.LerpTo( vT * 10f, Time.Delta * 10f );
			roll = roll.LerpTo( -hT * 15f, Time.Delta * 10f );

			Rotation = Rotation.From( Input.Rotation.Pitch(), Input.Rotation.Yaw(), roll );

			Vector3 camPos = position + (Rotation.Backward * 10 * zoom + Rotation.Up * 0.5f * zoom);

			Position = cameraTrace.FromTo( position, camPos ).Run().EndPos;

			FieldOfView = 75 + fov;

			Viewer = null;
		}

		public override void BuildInput( InputBuilder input )
		{
			base.BuildInput( input );
		}
	}
}
