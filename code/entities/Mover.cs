using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	[Library( "ball_mover" )]
	[Hammer.SupportsSolid]
	[Hammer.RenderFields]
	[Hammer.DoorHelper( "movedir", "islocal", "movetype", "movedistance" )]
	[Hammer.VisGroup( Hammer.VisGroup.Dynamic )]
	public partial class Mover : ModelEntity
	{
		public static new List<Mover> All = new();
		public ModelEntity ClientEntity;

		/// <summary>
		/// DONT TOUCH!! Used for display purposes.
		/// </summary>
		[Property( "islocal" )]
		private bool isLocal { get; set; } = true;

		/// <summary>
		/// DONT TOUCH!! Used for display purposes.
		/// </summary>
		[Property( "movetype" )]
		private int moveType { get; set; } = 3;

		[Property( "origin" )]
		[Net] public Vector3 StartPosition { get; private set; }

		[Property( "angles" )]
		[Net] public Angles StartAngles { get; private set; }

		[Property( "speed", Title = "Speed" )]
		[Net] public float Speed { get; private set; }

		[Property( "movedistance", Title = "Move Distance" )]
		[Net] public float MoveDistance { get; private set; }

		[Property( "movedir", Title = "Move Direction" )]
		[Net] public Angles MoveAngles { get; private set; }

		/// <summary>
		/// Where in the animation the mover will start. 0 = retracted, 1 = extended.
		/// </summary>
		[Property( "starttime", Title = "Start Time" )]
		[Net] public float StartTime { get; private set; } = 0f;

		public float MoveTime => Speed / MoveDistance;
		public Vector3 MoveDirection => Rotation.From( MoveAngles ).Forward;
		public Vector3 TargetPosition => StartPosition + MoveDirection * MoveDistance;

		public Mover()
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
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.LADDER );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();

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
			float rad = time * MoveTime * MathF.PI * 0.5f + (MathF.PI * (1f - StartTime));

			float sine = MathF.Cos( rad );
			float cosine = MathF.Sin( rad );
			float t = sine * 0.5f + 0.5f;

			Vector3 pos = StartPosition.LerpTo( TargetPosition, t );
			Vector3 vel = MoveDirection * -(Speed * cosine);

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

		//private HashSet<Entity> transferredChildren = new HashSet<Entity>();
		int lastTick = 0;
		int lastRealTick = 0;
		//float colorHue = 0f;

		[Event.Frame]
		public void Frame()
		{
			/*
			if ( Children.Count > 0 )
			{
				foreach ( Entity child in Children )
				{
					if ( !transferredChildren.Contains( child ) )
					{
						child.SetParent( ClientEntity );
						transferredChildren.Add( child );
					}
				}
			}
			*/

			if ( Local.Pawn is Ball player && player.LifeState == LifeState.Alive )
			{
				if ( lastRealTick != Time.Tick - 1 )
					ClientEntity.ResetInterpolation();

				lastTick = player.ActiveTick;
				lastRealTick = Time.Tick;
			}
			else
				AtTick( lastTick + Time.Tick - lastRealTick );
		}
	}
}
