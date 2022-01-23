using Sandbox.UI;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Ballers
{
	/// <summary>
	/// This is the HUD entity. It creates a RootPanel clientside, which can be accessed
	/// via RootPanel on this entity, or Local.Hud.
	/// </summary>
	public partial class BallersHudEntity : Sandbox.HudEntity<RootPanel>
	{
		public BallersHudEntity()
		{
			if ( IsClient )
			{
				RootPanel.SetTemplate( "ui/ballershud.html" );

				RootPanel.AddChild<NameTags>();
				RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
				RootPanel.AddChild<Timer>();
			}
		}
	}

}
