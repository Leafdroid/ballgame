using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	[Library( "trigger_death", Description = "Immediately kills all ballers inside" )]
	[Hammer.Solid]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public partial class HurtBrush : BrushEntity
	{
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
	}
}
