using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_movelinear" )]
	public partial class MovingBrush : BrushEntity
	{
		public static readonly new List<MovingBrush> All = new();

		[Property( "origin" )]
		[Net] public Vector3 StartPosition { get; private set; }

		[Property( "angles" )]
		[Net] public Angles StartAngles { get; private set; }

		public Vector3 EndPosition => StartPosition + MoveDirection * MoveDistance;

		[Property( "speed" )]
		[Net] public float Speed { get; private set; }

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

			Rotation = StartAngles.ToRotation();
			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.LADDER );

			All.Add( this );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			EnableDrawing = false;

			ClientModel = new ModelEntity();
			ClientModel.Rotation = StartAngles.ToRotation();
			ClientModel.SetModel( GetModel() );
			ClientModel.EnableAllCollisions = false;
			ClientModel.EnableTraceAndQueries = false;

			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.LADDER );

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

		public void Simulate()
		{
			float moveTime = Speed / MoveDistance;
			float rad = Time.Now * moveTime * MathF.PI;
			float sine = MathF.Sin( rad );
			float cosine = MathF.Cos( rad );
			float t = sine * 0.5f + 0.5f;

			Vector3 position = StartPosition.LerpTo( EndPosition, t );
			Vector3 velocity = MoveDirection * (Speed * cosine);

			if ( IsServer )
			{
				Position = position;
				Velocity = velocity;
			}
		}

		float colorHue = 0f;
		public void FrameSimulate()
		{
			float moveTime = Speed / MoveDistance;
			float rad = Time.Now * moveTime * MathF.PI;
			float sine = MathF.Sin( rad );
			float cosine = MathF.Cos( rad );
			float t = sine * 0.5f + 0.5f;
			bool closing = cosine <= 0f;

			Vector3 position = StartPosition.LerpTo( EndPosition, t );

			ClientModel.Position = position;
			//DebugOverlay.Text( position, Velocity.ToString(), Color.White );

			colorHue = colorHue.LerpTo( closing ? 0f : 120f, Time.Delta * 10f );
			Color color = new ColorHsv( colorHue, 0.8f, 1f );

			DebugOverlay.Circle( StartPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( EndPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( position, CurrentView.Rotation, 1f, color );
			DebugOverlay.Line( position, ClientModel.WorldSpaceBounds.Center, color );
			DebugOverlay.Line( StartPosition, EndPosition, color );
		}
	}
}
