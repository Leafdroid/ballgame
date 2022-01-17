using Sandbox;

namespace Ballers
{
	public class BallController : PawnController
	{

		public override void Simulate()
		{
			var player = Pawn as BallPlayer;
			if ( !player.IsValid() ) return;

			var ball = player.Ball;
			if ( !ball.IsValid() ) return;

			Position = ball.Position - Vector3.Up * 36;

			EyeRot = Input.Rotation;
			EyePosLocal = Vector3.Up * 54;
			Velocity = ball.Velocity;
		}
	}
}
