using Sandbox;
using System.ComponentModel.DataAnnotations;

namespace Ballers
{
	[Library( "info_ball_start" )]
	[EditorModel( "models/editor/ball.vmdl", FixedBounds = true )]
	//[EntityTool( "Ball Spawnpoint", "Balls", "Spawnpoint for balls" )]
	[Display( Name = "Ball Spawnpoint" ), Icon( "place" )]
	public partial class BallSpawn : Entity
	{
		/// <summary>
		/// Used for linking checkpoint, set to 0 for initial spawn.
		/// </summary>
		[Property( "index", Title = "Index" )]
		[Net] public int Index { get; private set; } = 0;

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
		}

		private void SharedSpawn()
		{
			Transmit = TransmitType.Always;
			EnableDrawing = false;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
