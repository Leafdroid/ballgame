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
		public ModelEntity ClientEntity;

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

		public float MoveTime => Speed / MoveDistance;
		public Vector3 MoveDirection => Rotation.From( MoveAngles ).Forward;
		public Vector3 TargetPosition => StartPosition + MoveDirection * MoveDistance;

		public MovingBrush()
		{
			All.Add( this );
		}

		public override void Spawn()
		{
			base.Spawn();
			Rotation = StartAngles.ToRotation();
			SharedSpawn();

			Transmit = TransmitType.Always;

			EnableTraceAndQueries = true;
			EnableAllCollisions = true;
			EnableDrawing = true;
		}

		private void SharedSpawn()
		{
			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.LADDER );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();

			if ( IsClient )
				Log.Info( "ClientSpawn" );

			EnableTraceAndQueries = false;
			EnableAllCollisions = false;
			EnableDrawing = false;

			ModelEntity clientEnt = new ModelEntity();
			clientEnt.Model = Model;
			clientEnt.SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			clientEnt.EnableAllCollisions = true;
			clientEnt.Transform = Transform;

			clientEnt.ClearCollisionLayers();
			clientEnt.AddCollisionLayer( CollisionLayer.LADDER );
			ClientEntity = clientEnt;
		}

		public void AtTick( int tick )
		{
			AtTime( tick * Global.TickInterval );
		}

		public void AtTime( float time )
		{
			float rad = time * MoveTime * MathF.PI * 0.5f;

			float sine = MathF.Sin( rad );
			float cosine = MathF.Cos( rad );
			float t = sine * 0.5f + 0.5f;

			Vector3 pos = StartPosition.LerpTo( TargetPosition, t );
			Vector3 vel = MoveDirection * (Speed * cosine);

			if ( IsServer )
			{
				Position = pos;
				Velocity = vel;
			}
			else
			{
				ClientEntity.Position = pos;
				ClientEntity.Velocity = vel;
			}
		}

		int lastTick = 0;
		int lastRealTick = 0;
		//float colorHue = 0f;
		[Event.Tick]
		public void Frame()
		{
			if ( IsServer )
				return;

			if ( Local.Pawn is Ball player && player.LifeState == LifeState.Alive )
			{
				if ( lastRealTick != Time.Tick - 1 )
					ClientEntity.ResetInterpolation();

				lastTick = player.ActiveTick;
				lastRealTick = Time.Tick;
			}
			else
				AtTick( lastTick + Time.Tick - lastRealTick );

			int tick = lastTick + Time.Tick - lastRealTick;
			float rad = tick * Global.TickInterval * MoveTime * MathF.PI * 0.5f;
			float cosine = MathF.Cos( rad );
			bool closing = cosine <= 0f;

			/*
			colorHue = colorHue.LerpTo( closing ? 0f : 120f, Time.Delta * 10f );
			Color color = new ColorHsv( colorHue, 0.8f, 1f );

			DebugOverlay.Circle( StartPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( TargetPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( ClientEntity.Position, CurrentView.Rotation, 1f, color );
			DebugOverlay.Line( ClientEntity.Position, ClientEntity.WorldSpaceBounds.Center, color );
			DebugOverlay.Line( StartPosition, TargetPosition, color );
			*/
		}
	}
}
