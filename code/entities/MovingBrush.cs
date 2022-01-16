using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_movelinear" )]
	public partial class MovingBrush : BrushEntity
	{
		public static new List<MovingBrush> All = new();

		[Property( "origin" )]
		[Net] public Vector3 StartPosition { get; private set; }

		[Property( "angles" )]
		[Net] public Angles StartAngles { get; private set; }

		[Property( "speed" )]
		[Net] public float Speed { get; private set; }

		[Property( "movedistance" )]
		[Net] public float MoveDistance { get; private set; }

		[Property( "movedir" )]
		[Net] public Angles MoveAngles { get; private set; }

		public Vector3 TargetPosition => StartPosition + MoveDirection * MoveDistance;
		public Rotation MoveRotation => Rotation.From( MoveAngles );
		public Vector3 MoveDirection => MoveRotation.Forward;

		public MovingBrush()
		{
			All.Add( this );
		}

		public override void Spawn()
		{
			base.Spawn();
			Rotation = StartAngles.ToRotation();
			SharedSpawn();
		}

		private void SharedSpawn()
		{
			EnableTraceAndQueries = true;
			EnableAllCollisions = true;
			EnableDrawing = true;

			ClearCollisionLayers();
			RemoveCollisionLayer( CollisionLayer.All );
			AddCollisionLayer( CollisionLayer.LADDER );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}

		public void AtTick( int tick )
		{
			float moveTime = Speed / MoveDistance;

			float interval = Global.TickInterval;
			float rad = tick * interval * moveTime * MathF.PI * 0.5f;

			float sine = MathF.Sin( rad );
			float cosine = MathF.Cos( rad );
			float t = sine * 0.5f + 0.5f;

			if ( IsServer || Local.Pawn == Owner )
			{
				Position = StartPosition.LerpTo( TargetPosition, t );
				Velocity = MoveDirection * (Speed * cosine);
			}
		}

		//float colorHue = 0f;

		/*
		int lastTick = 0;
		int lastRealTick = 0;
		[Event.Frame]
		public void Frame()
		{
			if ( Local.Pawn is BallPlayer player && player.Ball.IsValid() )
			{
				AtTick( player.Ball.ActiveTick );
				lastTick = player.Ball.ActiveTick;
				lastRealTick = Time.Tick;
			}
			else
				AtTick( lastTick + Time.Tick - lastRealTick );

			//DebugOverlay.Text( position, Velocity.ToString(), Color.White );
			/*
			colorHue = colorHue.LerpTo( closing ? 0f : 120f, Time.Delta * 10f );
			Color color = new ColorHsv( colorHue, 0.8f, 1f );

			DebugOverlay.Circle( StartPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( EndPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( position, CurrentView.Rotation, 1f, color );
			DebugOverlay.Line( position, ClientModel.WorldSpaceBounds.Center, color );
			DebugOverlay.Line( StartPosition, EndPosition, color );
			*/
		//}

	}
}
