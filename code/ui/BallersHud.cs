using Sandbox.UI;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Ballers
{
	public partial class BallersHudEntity : RootPanel
	{
		public static BallersHudEntity Instance;

		public BallersHudEntity()
		{
			//If the hud already exists, delete and make it null for a new one
			if(Instance != null)
            {
				Instance?.Delete();
				Instance = null;
			}

			Instance = this;

			SetTemplate( "ui/ballershud.html" );

			AddChild<NameTags>();
			AddChild<Scoreboard<ScoreboardEntry>>();
			AddChild<Timer>();
		}
	}
}
