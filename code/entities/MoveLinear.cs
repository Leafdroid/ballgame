using Sandbox;
using System;
using System.Collections.Generic;

namespace Ballers
{

	[Library( "func_movelinear" )]
	public partial class MoveLinear : FuncBrush
	{
		public static readonly new List<MoveLinear> All = new();

		[Property( "origin" )]
		[Net] public Vector3 StartPosition { get; private set; }
		public Vector3 EndPosition => StartPosition + MoveDirection * MoveDistance;

		[Property( "speed" )]
		[Net] public float Speed { get; private set; }
		public Vector3 ClientVelocity { get; set; }

		[Property( "movedistance" )]
		[Net] public float MoveDistance { get; private set; }

		[Property( "movedir" )]
		[Net] public Angles MoveAngles { get; private set; }
		public Rotation MoveRotation => Rotation.From( MoveAngles );
		public Vector3 MoveDirection => MoveRotation.Forward;
		public ModelEntity ClientModel { get; private set; }

		public override void Spawn()
		{
			base.Spawn();

			All.Add( this );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			EnableDrawing = false;

			ClientModel = new ModelEntity();
			ClientModel.SetModel( GetModel() );
			ClientModel.EnableAllCollisions = false;
			ClientModel.EnableTraceAndQueries = false;

			All.Add( this );
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( IsClient && ClientModel.IsValid() )
			{
				ClientModel.Delete();
			}
		}

		/*
		public void BallTrace()
		{
			if ( IsServer )
				return;

			Ball ball = Ball.Find( Local.Client );
			if ( !ball.IsValid() )
				return;

			Entity entity = IsServer ? this : ClientModel;
			//Vector3 position = IsServer ? Position : ClientModel.Position;
			Vector3 velocity = IsServer ? Velocity : ClientModel.Velocity;

			Vector3 relativeVelocity = velocity - ball.Velocity;


			TraceResult ballTrace = Trace.Ray( ball.Position, ball.Position + relativeVelocity * Time.Delta )
			.Only( entity ).Radius( 40f ).Run();

			Vector3 hitDir = (ballTrace.EndPos - ball.Position).Normal;

			if ( ballTrace.Hit ) 
			{
				//ball.Velocity += relativeVelocity;

				DebugOverlay.Line( ballTrace.EndPos, ballTrace.EndPos + hitDir * 32f );
				//ball.Move();
				DebugOverlay.Sphere( ballTrace.EndPos, 40f, Color.Red, true );
			}
		}
		*/

		public void Simulate()
		{
			if ( IsClient )
				return;
			
			float moveTime = Speed / MoveDistance;
			float rad = Time.Now * moveTime * MathF.PI;
			float sine = MathF.Sin( rad );
			float cosine = MathF.Cos( rad );
			float t = sine * 0.5f + 0.5f;

			Vector3 position = StartPosition.LerpTo( EndPosition, t );
			Vector3 velocity = MoveDirection * (Speed * cosine);

			Position = position;
			Velocity = velocity;
		}

		[Event.Frame]
		public void Frame()
		{
			float moveTime = Speed / MoveDistance;
			float rad = Time.Now * moveTime * MathF.PI;
			float sine = MathF.Sin( rad );
			float cosine = MathF.Cos( rad );
			float t = sine * 0.5f + 0.5f;
			bool closing = cosine <= 0f;

			Vector3 position = StartPosition.LerpTo( EndPosition, t );

			ClientModel.Position = position;
			DebugOverlay.Text( position, Velocity.ToString(), Color.White );

			Color color = closing ? Color.Red : Color.Green;

			DebugOverlay.Sphere( StartPosition, 2f, color );
			DebugOverlay.Sphere( EndPosition, 2f, color );
			DebugOverlay.Sphere( position, 1f, color );
			DebugOverlay.Line( StartPosition, EndPosition, color );
		}
	}
}
