using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_checkpoint" )]
	public partial class CheckpointBrush : BrushEntity
	{
		[Property( "index" )]
		[Net] public int Index { get; private set; } = 0;

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
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}

		public static readonly SoundEvent Badge = new( "sounds/ball/badge.vsnd" )
		{
			Pitch = 1f,
			Volume = 1f,
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
