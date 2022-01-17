using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "func_falldamage" )]
	public partial class FallDamageBrush : BrushEntity
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
			ClearCollisionLayers();
			RemoveCollisionLayer( CollisionLayer.All );
			AddCollisionLayer( CollisionLayer.Trigger );
			Tags.Add( "fallDamage" );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
