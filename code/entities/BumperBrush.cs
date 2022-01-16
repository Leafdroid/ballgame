using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{
	// TODO: Add clientside duplicate for each client to predict client simulation

	[Library( "func_bumper" )]
	public partial class BumperBrush : BrushEntity
	{
		[Property( "force" )]
		[Net] public float Force { get; private set; }

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
			Transmit = TransmitType.Always;
		}

		private void SharedSpawn()
		{
			ClearCollisionLayers();
			RemoveCollisionLayer( CollisionLayer.All );
			AddCollisionLayer( CollisionLayer.STATIC_LEVEL );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
