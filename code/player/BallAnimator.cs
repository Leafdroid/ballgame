using System;
using Ballers;

namespace Sandbox
{
	public class BallAnimator : PawnAnimator
	{
		TimeSince TimeSinceFootShuffle = 60;

		public override void Simulate()
		{

			DoRotation();
			DoWalk();

			SetParam( "b_grounded", true );

			if ( Host.IsClient && Client.IsValid() )
			{
				SetParam( "voice", Client.TimeSinceLastVoice < 0.5f ? Client.VoiceLevel : 0.0f );
			}

			Vector3 aimPos = Pawn.EyePos + Input.Rotation.Forward * 200;
			Vector3 lookPos = aimPos;

			//
			// Look in the direction what the player's input is facing
			//
			SetLookAt( "lookat_pos", lookPos ); // old
			SetLookAt( "aimat_pos", aimPos ); // old

			SetLookAt( "aim_eyes", lookPos );
			SetLookAt( "aim_head", lookPos );
			SetLookAt( "aim_body", aimPos );

			SetParam( "holdtype", 0 );
			SetParam( "aimat_weight", 0.5f ); // old
			SetParam( "aim_body_weight", 0.5f );
		}

		public virtual void DoRotation()
		{
			if ( Pawn is not BallPlayer player || player.Ball == null )
				return;

			var velocity = player.Ball.Velocity;// player.Ball.MoveDirection.Normal*player.Ball.NetVelocity.Length;

			Rotation idealRotation = Rotation.LookAt( velocity.WithZ(0), Vector3.Up );
			//
			// Our ideal player model rotation is the way we're facing
			//
			var allowYawDiff = Pawn.ActiveChild == null ? 90 : 50;

			float turnSpeed = 0.01f;

			//
			// If we're moving, rotate to our ideal rotation
			//
			Rotation = Rotation.Slerp( Rotation, idealRotation, player.Ball.Velocity.Length * Time.Delta * turnSpeed );

			//
			// Clamp the foot rotation to within 120 degrees of the ideal rotation
			//
			Rotation = Rotation.Clamp( idealRotation, allowYawDiff, out var change );

			//
			// If we did restrict, and are standing still, add a foot shuffle
			//
			if ( change > 1 && player.Ball.Velocity.Length <= 1 ) TimeSinceFootShuffle = 0;

			SetParam( "b_shuffle", TimeSinceFootShuffle < 0.1 );
		}

		void DoWalk()
		{
			if ( Pawn is not BallPlayer player || player.Ball == null )
				return;

			var velocity = player.Ball.Velocity;

			var forward = Rotation.Forward.Dot( velocity );
			var sideward = Rotation.Right.Dot( velocity );
			var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

			SetParam( "move_direction", angle );
			SetParam( "move_speed", velocity.Length );
			SetParam( "move_groundspeed", velocity.WithZ( 0 ).Length );
			SetParam( "move_y", sideward );
			SetParam( "move_x", forward );
			SetParam( "move_z", 0 );

			SetParam( "wish_direction", angle );
			SetParam( "wish_speed", velocity.Length );
			SetParam( "wish_groundspeed", velocity.WithZ( 0 ).Length );
			SetParam( "wish_y", sideward );
			SetParam( "wish_x", forward );
			SetParam( "wish_z", 0  );

		}
	}
}
