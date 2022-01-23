using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_checkpoint" )]
	[Hammer.Solid]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public partial class CheckpointBrush : BrushEntity
	{
		/// <summary>
		/// Used for checkpoint order and for linking spawnpoints. Do not use 0 for this, start with 1.
		/// </summary>
		[Property( "index", Title = "Index" )]
		[Net] public int Index { get; private set; } = 0;

		public static int LastIndex { get; private set; }

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
			Transmit = TransmitType.Always;
		}

		private void SharedSpawn()
		{
			EnableDrawing = false;
			EnableAllCollisions = false;
			EnableTraceAndQueries = true;

			ClearCollisionLayers();
			AddCollisionLayer( CollisionLayer.Trigger );

			if ( Index > LastIndex )
				LastIndex = Index;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}

		public static readonly SoundEvent Badge = new( "sounds/ball/badge.vsnd" )
		{
			Pitch = 1f,
			Volume = 0.4f,
			DistanceMax = 1024,
			UI = true
		};

		public static readonly SoundEvent Swoosh = new( "sounds/ball/swoosh.vsnd" )
		{
			Pitch = 1f,
			Volume = 0.4f,
			DistanceMax = 1024,
			UI = true
		};

		[ClientCmd]
		public static void PrintCheckpoints()
		{
			foreach ( var brush in All.OfType<CheckpointBrush>() )
				Log.Info( $"brush: {brush.Index}" );

			foreach ( var spawn in All.OfType<BallSpawn>() )
				Log.Info( $"spawn: {spawn.Index}" );
		}
	}
}
