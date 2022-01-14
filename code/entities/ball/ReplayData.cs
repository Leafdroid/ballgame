
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
		private ushort latestData = 0;
		private List<ushort> inputs = new List<ushort>();

		private int firstTick = -1;
		private int latestTick = -1;

		private int index = 0;
		private int readRepeats = 0;

		public ushort GetNext( out bool finished )
		{
			finished = false;
			int count = inputs.Count();
			if ( index == count )
			{
				finished = true;
				return 0;
			}


			ushort data = inputs[index];
			ushort repeats = (ushort)(data >> 9);

			if ( readRepeats == repeats - 1 )
			{
				readRepeats = 0;
				index++;
			}
			else
			{
				readRepeats++;
			}

			return data;

			/*
			int count = inputs.Count();
			if ( index == count - 1 )
				return 0;

			ushort data = inputs[index];
			ushort repeats = (ushort)(data >> 9);

			if ( readRepeats >= repeats )
			{
				readRepeats = 0;
				index++;
				data = inputs[index];
			}

			readRepeats++;
			return data;
			*/
		}

		public void AddData( BallInput input )
		{
			if ( firstTick == -1 )
				firstTick = Time.Tick;
			if ( latestTick == -1 )
				latestTick = Time.Tick;

			ushort data = input.data;
			int repeats = Time.Tick - latestTick;

			if ( data != latestData || repeats == 127 )
			{
				AddLatest();
				latestData = data;
			}

		}

		public void AddLatest()
		{
			int repeats = Time.Tick - latestTick;
			if ( repeats == 0 )
				return;

			latestTick = Time.Tick;

			ushort repeatData = (ushort)(latestData + (repeats << 9));
			inputs.Add( repeatData );
		}

		public void Write( Client client )
		{
			AddLatest();

			Log.Info( $"Input container has {inputs.Count} ushorts stored, ranging over {latestTick - firstTick} ticks!" );

			string fileName = $"replays/{Global.MapName}/{client.PlayerId}.replay";
			FileSystem.Data.CreateDirectory( $"replays/{Global.MapName}" );

			using ( var writer = new BinaryWriter( FileSystem.Data.OpenWrite( fileName ) ) )
			{
				for ( int i = 0; i < inputs.Count; i++ )
				{
					ushort data = inputs[i];
					writer.Write( data );

					//ushort repeats = (ushort)(data >> 9);
					//ushort inputData = (ushort)(data & 511);
					//Ball.BallInput.Parse( inputData, out Vector3 moveDir );
					//Log.Info( $"{moveDir} repeats for {repeats} ticks" );
				}
			}
		}

		public static ReplayData FromFile( Client client )
		{
			string fileName = $"replays/{Global.MapName}/{client.PlayerId}.replay";
			if ( !FileSystem.Data.FileExists( fileName ) )
				return null;

			List<ushort> list = new List<ushort>();

			using ( var reader = new BinaryReader( FileSystem.Data.OpenRead( fileName ) ) )
			{
				while ( reader.BaseStream.Position < reader.BaseStream.Length )
				{
					ushort data = reader.ReadUInt16();
					list.Add( data );
				}
			}

			ReplayData container = new ReplayData();
			container.inputs = list;

			return container;
		}

		[ServerCmd( "playreplay" )]
		public static void GetReplay()
		{
			if ( ConsoleSystem.Caller.Pawn is not BallPlayer player )
				return;

			Stopwatch watch = new Stopwatch();

			ReplayData container = ReplayData.FromFile( ConsoleSystem.Caller );
			if ( container == null )
				return;

			Log.Info( $"Took {watch.Stop()}ms to fetch replay from file!" );

			Ball.Create( ConsoleSystem.Caller, Ball.ControlType.Replay );
		}

		[ServerCmd( "savereplay" )]
		public static void SaveReplay()
		{
			if ( ConsoleSystem.Caller.Pawn is not BallPlayer player )
				return;

			if ( !player.Ball.IsValid() )
				return;

			player.Ball.ReplayData.Write( ConsoleSystem.Caller );
		}

		//if (IsServer ) //|| Local.Client == Owner.Client )
		[ServerCmd( "stopreplays" )]
		public static void RemoveReplays()
		{
			foreach ( Ball ball in Entity.All.Where( b => b is Ball ball && ball.Controller == Ball.ControlType.Replay ) )
				ball.Delete();
		}
	}
}
