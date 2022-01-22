using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_falldamage" )]
	[Hammer.Solid]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public class FallDamageBrush : BrushEntity
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
