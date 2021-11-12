
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ballers
{
	public partial class Ball
	{
		public bool QueueDeletion => queueDeletion;
		private bool queueDeletion = false;

		public Client Owner { get; private set; }
		public int NetworkIdent => Owner.NetworkIdent;

		public Vector3 Position { get; set; }
		public Vector3 Velocity { get; set; }
		public Vector3 RealPosition { get; set; }
		public Vector3 RealVelocity { get; set; }
		public Vector3 MoveDirection { get; set; }
		
		public bool IsClient => Host.IsClient;
		public bool IsServer => Host.IsServer;

		public void Delete()
		{
			queueDeletion = true;

			if ( IsServer )
				NetDelete( NetworkIdent );
			else if ( Model.IsValid() )
				Model.Delete();
		}

		public void Tick()
		{
			if ( IsClient )
				return;

			Vector3 pos = Position + Vector3.Up * (IsServer ? 1 : -1);
			//DebugOverlay.Sphere( pos, 40f, Color.Blue );
			//DebugOverlay.Line( pos, pos + MoveDirection * 80f, Color.Blue );
		}

		public void Frame()
		{
			Vector3 pos = Position + Vector3.Up * (IsServer ? 1 : -1);
			if ( Owner == Local.Client)
			{
				//DebugOverlay.Sphere( RealPosition, 40f, Color.Cyan.WithAlpha(0.25f) );
				//DebugOverlay.Line( Position, RealPosition, Color.Cyan.WithAlpha( 0.25f ) );
			}
		}

		public override int GetHashCode() => NetworkIdent;
		public override string ToString() => $"Ball {NetworkIdent} ({Owner.Name})";
	}
}
