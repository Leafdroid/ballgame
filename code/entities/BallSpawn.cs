using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	[Library( "info_ball_start" )]
	[Hammer.EditorModel( "models/editor/ball.vmdl", 0, 0, 0 )]
	[Hammer.EntityTool( "Ball Spawnpoint", "Balls" )]
	public partial class BallSpawn : BrushEntity
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
			EnableTraceAndQueries = false;

			ClearCollisionLayers();
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
