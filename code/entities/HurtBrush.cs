using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	[Library( "trigger_death", Description = "Used for killing ballers" )]
	[Hammer.SupportsSolid]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public partial class HurtBrush : BrushEntity
	{
		/// <summary>
		/// Set to true if baller has to collide with a solid to die in this zone, use for fall damage deaths etc.
		/// </summary>
		[Property( "requireCollision", Title = "Require Collision" )]
		[Net] public bool RequireCollision { get; private set; } = false;

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
