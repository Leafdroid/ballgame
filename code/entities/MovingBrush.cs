using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	// TODO: Add clientside duplicate for each client to predict client simulation

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

		/*
		public void DupeToCient( Client )
		{
			base.Spawn();
			Rotation = StartAngles.ToRotation();
			SharedSpawn();

			Transmit = TransmitType.Always;

			EnableTraceAndQueries = true;
			EnableAllCollisions = true;
			EnableDrawing = true;
			ServerEntity = true;
		}
		*/

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
			RemoveCollisionLayer( CollisionLayer.All );
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
			clientEnt.RemoveCollisionLayer( CollisionLayer.All );
			clientEnt.AddCollisionLayer( CollisionLayer.LADDER );
			ClientEntity = clientEnt;
		}

		public void AtTick( int tick )
		{
			float interval = Global.TickInterval;
			//Log.Info( $"{(Host.IsServer ? "server" : "client")} - {tick} tick" );
			float rad = tick * interval * MoveTime * MathF.PI * 0.5f;

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



		/*
		int lastTick = 0;
		int lastRealTick = 0;
		[Event.Tick]
		public void Frame()
		{
			
			if ( IsServer )
				return;

			if ( Local.Pawn is BallPlayer player && player.Ball.IsValid() )
			{
				lastTick = player.Ball.ActiveTick;
				lastRealTick = Time.Tick;
			}
			else
				AtTick( lastTick + Time.Tick - lastRealTick );
			
			
			DebugOverlay.Text( position, Velocity.ToString(), Color.White );
			
			colorHue = colorHue.LerpTo( closing ? 0f : 120f, Time.Delta * 10f );
			Color color = new ColorHsv( colorHue, 0.8f, 1f );

			DebugOverlay.Circle( StartPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( EndPosition, CurrentView.Rotation, 2f, color );
			DebugOverlay.Circle( position, CurrentView.Rotation, 1f, color );
			DebugOverlay.Line( position, ClientModel.WorldSpaceBounds.Center, color );
			DebugOverlay.Line( StartPosition, EndPosition, color );
			
		}
		*/
	}
}
