using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ballers
{

	[Library( "gumby" )]
	public partial class CamPhaseBrush : BrushEntity
	{

		//[Property( "movedir" )]
		//[Net] public Angles MoveAngles { get; private set; }

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
		}

		private void SharedSpawn()
		{
			AddCollisionLayer( CollisionLayer.STATIC_LEVEL );
			Tags.Add( "cameraPhase" );
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
