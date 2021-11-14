
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class Ball
	{
		private struct InputState
		{
			public int forwards;
			public int sidewards;

			public void Reset()
			{
				forwards = 0;
				sidewards = 0;
			}
		}

		private InputState currentInput;

		private bool hasPopped = false;
		private float popStart => BallersGame.StartTime - 2.1f;

		public async void PlayPop(int number)
		{
			await GameTask.DelaySeconds( number * 0.5f );

			Sound pop = Sound.FromScreen( PopSound.Name );
			pop.SetVolume( 1.5f );
			pop.SetPitch( 1.25f );

			if ( number == 4 )
			{
				pop.SetPitch( 1f );
				pop.SetVolume( 2f );
			}
				
		}

		public void Simulate( )
		{
			currentInput.forwards = (Input.Down( InputButton.Forward ) ? 1 : 0) + (Input.Down( InputButton.Back ) ? -1 : 0);
			currentInput.sidewards = (Input.Down( InputButton.Left ) ? 1 : 0) + (Input.Down( InputButton.Right ) ? -1 : 0);

			//string side = IsServer ? "SERVER" : "CLIENT";
			//Log.Info( $"[{side}] Tick {Time.Tick} had inputs ({currentInput.forwards},{currentInput.sidewards})" );

			bool starting = Time.Now < BallersGame.StartTime;
			if ( starting )
			{
				currentInput.Reset();

				if ( IsClient )
				{
					if ( Time.Now >= popStart && !hasPopped )
					{
						for ( int i = 1; i < 5; i++ )
							PlayPop(i);
						hasPopped = true;
					}
				}
			}
			else hasPopped = false;

			Vector3 moveDir = new Vector3( currentInput.forwards, currentInput.sidewards, 0 );
			MoveDirection = (moveDir * Input.Rotation).WithZ( 0 ).Normal;

			SimulatePhysics();
		}

		public static readonly SoundEvent PopSound = new( "sounds/ball/pop.vsnd" );
	}
}
