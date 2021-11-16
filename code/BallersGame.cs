﻿
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
				new BallersHudEntity();
		}

		public override void Simulate( Client client )
		{
			base.Simulate( client );

			foreach ( MovingBrush move in MovingBrush.All )
				move.Simulate();
		}

		public override void FrameSimulate( Client client )
		{
			base.FrameSimulate( client );

			foreach ( MovingBrush move in MovingBrush.All )
				move.FrameSimulate();
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			if ( IsServer )
				Ball.DeliverClothing( client );

			var player = new BallPlayer();
			client.Pawn = player;

			player.Respawn();
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{
			if ( cl.Pawn is BallPlayer player )
			{
				Ball ball = player.Ball;
				if ( ball.IsValid() )
					ball.Delete();
			}

			base.ClientDisconnect( cl, reason );
		}

	}
}
