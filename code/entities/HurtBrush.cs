using Sandbox;

namespace Ballers
{
	[Library( "trigger_death", Description = "Used for killing ballers" )]
	[SandboxEditor.SupportsSolid]
	[SandboxEditor.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	public partial class HurtBrush : BaseTrigger
	{
		/// <summary>
		/// Set to true if baller has to collide with a solid to die in this zone, use for fall damage deaths etc.
		/// </summary>
		[Property( "requirecollision", Title = "Require Collision" )]
		[Net] public bool RequireCollision { get; private set; } = false;

		public override void Spawn()
		{
			base.Spawn();
			SharedSpawn();
			Transmit = TransmitType.Always;
		}

		private void SharedSpawn()
		{
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

			EnableDrawing = false;
			EnableAllCollisions = false;
			EnableTraceAndQueries = true;

			Tags.Add("trigger");
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
