
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public class ReplayData
	{
		private ushort? latestData = null;
		private List<ushort> inputs = new List<ushort>();
		private List<float> times = new List<float>();

		public float FinishTime => GetTime( times.Count - 1 );
		public float GetTime( int index )
		{
			if ( index >= times.Count )
			{
				Log.Error( "Tried to retrieve non-existing time from replay!" );
				return 0f;
			}
			else return times[index];
		}

		private int readIndex = 0;
		private int readRepeats = 0;
		private int writeRepeats = 0;

		public ushort NextInput( out bool finished )
		{
			finished = false;
			int count = inputs.Count;

			if ( readIndex == count )
			{
				finished = true;
				return 0;
			}

			ushort data = inputs[readIndex];
			ushort repeats = (ushort)(data >> 10);

			if ( readRepeats == repeats )
			{
				readRepeats = 0;
				readIndex++;
			}
			else
				readRepeats++;

			return data;
		}

		public void AddInput( BallInput input )
		{
			ushort data = input.data;

			if ( latestData != null )
			{
				bool unique = data != latestData;

				if ( unique || writeRepeats == 63 )
					AddLatest();
				else if ( !unique )
					writeRepeats++;
			}

			latestData = data;
		}

		public void AddTime( float time ) => times.Add( time );

		public void AddLatest()
		{
			ushort repeatData = (ushort)(latestData + (writeRepeats << 10));
			//Log.Info( $"Added {writeRepeats + 1} samples of {UShortString( repeatData )}" );
			inputs.Add( repeatData );
			writeRepeats = 0;
		}
		private static string UShortString( ushort data )
		{
			string text = "";

			for ( int i = 15; i > -1; i-- )
			{
				int val = 1 << i;
				text += (data & val) == val ? "1" : "0";
			}

			return text;
		}

		public void Write( Client client )
		{
			AddLatest();

			string fileName = $"replays/{Global.MapName}/{client.PlayerId}.replay";
			FileSystem.Data.CreateDirectory( $"replays/{Global.MapName}" );

			using ( var writer = new BinaryWriter( FileSystem.Data.OpenWrite( fileName ) ) )
			{
				writer.Write( times.Count );
				for ( int i = 0; i < times.Count; i++ )
					writer.Write( times[i] );

				writer.Write( inputs.Count );
				for ( int i = 0; i < inputs.Count; i++ )
					writer.Write( inputs[i] );
			}
		}

		public static ReplayData FromClient( Client client ) => FromFile( client.PlayerId );

		public static ReplayData FromFile( long steamId )
		{
			string fileName = $"replays/{Global.MapName}/{steamId}.replay";
			if ( !FileSystem.Data.FileExists( fileName ) )
				return null;

			List<float> times = new List<float>();
			List<ushort> inputs = new List<ushort>();

			using ( var reader = new BinaryReader( FileSystem.Data.OpenRead( fileName ) ) )
			{
				int timeCount = reader.ReadInt32();
				for ( int i = 0; i < timeCount; i++ )
					times.Add( reader.ReadSingle() );

				int inputCount = reader.ReadInt32();
				for ( int i = 0; i < inputCount; i++ )
					inputs.Add( reader.ReadUInt16() );
			}

			ReplayData replay = new ReplayData();
			replay.times = times;
			replay.inputs = inputs;

			return replay;
		}

		public static void PlayReplay( Client client )
		{
			if ( client == null )
				return;

			long id = client.PlayerId;

			ReplayData container = FromFile( id );
			if ( container == null )
				return;

			Ball replayGhost = new Ball();
			replayGhost.Controller = Ball.ControlType.Replay;
			replayGhost.ReplayData = container;
			replayGhost.Create();
			replayGhost.Respawn();
		}


		[ServerCmd( "playreplay" )]
		public static void PlayReplay()
		{
			if ( ConsoleSystem.Caller.Pawn is not Ball player )
				return;

			Stopwatch watch = new Stopwatch();

			long id = ConsoleSystem.Caller.PlayerId;

			ReplayData container = FromFile( id );
			if ( container == null )
				return;

			Log.Info( $"Took {watch.Stop()}ms to fetch replay from file!" );

			Ball replayGhost = new Ball();
			replayGhost.Controller = Ball.ControlType.Replay;
			replayGhost.ReplayData = container;
			replayGhost.Owner = player;
			replayGhost.Create();
			replayGhost.Respawn();
		}



		/*

		[ServerCmd( "savereplay" )]
		public static void SaveReplay()
		{
			if ( ConsoleSystem.Caller.Pawn is not Ball player )
				return;

			player.ReplayData.Write( ConsoleSystem.Caller );
		}
		*/

		[ServerCmd( "stopreplays" )]
		public static void RemoveReplays()
		{
			int ghostCount = Ball.ReplayGhosts.Count;
			for ( int i = 0; i < ghostCount; i++ )
				Ball.ReplayGhosts[0].Delete();
		}
	}
}
