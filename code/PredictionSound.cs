using Sandbox;
using System.Linq;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using Sandbox.Internal.Globals;

public static partial class PredictionSound
{
	public static void World( Client client, string soundName, Vector3 position )
	{
		if ( Host.IsServer )
			WorldRpc( client, soundName, position );
		else if ( Local.Client == client )
			PlayWorld( soundName, position );
	}

	private static void PlayWorld( string soundName, Vector3 position )
	{
		Sandbox.Sound.FromWorld( soundName, position );
	}

	[ClientRpc]
	public static void WorldRpc( Client client, string soundName, Vector3 position )
	{
		if ( client != Local.Client )
			PlayWorld( soundName, position );
	}
}
