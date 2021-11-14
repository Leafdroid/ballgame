
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Ballers
{

	/// <summary>
	/// This is your game class. This is an entity that is created serverside when
	/// the game starts, and is replicated to the client. 
	/// 
	/// You can use this to create things like HUDs and declare which player class
	/// to use for spawned players.
	/// </summary>
	public partial class BallersGame : Sandbox.Game
	{
		public BallersGame()
		{
			if ( IsServer )
			{
				// Create a HUD entity. This entity is globally networked
				// and when it is created clientside it creates the actual
				// UI panels. You don't have to create your HUD via an entity,
				// this just feels like a nice neat way to do it.
				new BallersHudEntity();
			}
		}

		public static float StartTime = 0f;
		static HashSet<int> finished = new();

		[ClientRpc]
		public static void ClientStartTime(float time)
		{
			StartTime = time;
		}

		[ServerCmd("RaceTest")]
		public static void KillPlayers()
		{
			StartTime = Time.Now + 5f;
			ClientStartTime( StartTime );

			finished.Clear();
			foreach ( BallPlayer player in Entity.All.Where( e => e is BallPlayer ) )
			{
				player.Kill();
				player.RespawnDelay();
			}
		}

		static float lastTime = 0;

		[Event.Tick]
		public static void Ticky()
		{
			
			foreach ( BallPlayer player in Entity.All.Where( e => e is BallPlayer ) )
			{
				if (player.LifeState == LifeState.Alive && player.Position.x > 2000 && !finished.Contains( player.Client.NetworkIdent ) )
				{
					if ( finished.Count == 0 )
						lastTime = Time.Now;

					finished.Add( player.Client.NetworkIdent );
					float finishTime = Time.Now - StartTime;
					float seconds = Time.Now - lastTime;
					string behind = $"({seconds} seconds behind first place)";
					Log.Info($"{player.Client.Name} finished in {finishTime} seconds! {behind}");
				}
			}
		}


		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new BallPlayer();
			client.Pawn = player;

			player.Respawn();
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{
			base.ClientDisconnect( cl, reason );

			Ball ball = Ball.Find( cl.NetworkIdent );
			if ( ball.IsValid() )
				ball.Delete();
		}

	}
}
