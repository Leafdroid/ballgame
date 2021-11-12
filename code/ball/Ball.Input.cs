
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

		public void SimulateInput( )
		{
			currentInput.forwards = (Input.Down( InputButton.Forward ) ? 1 : 0) + (Input.Down( InputButton.Back ) ? -1 : 0);
			currentInput.sidewards = (Input.Down( InputButton.Left ) ? 1 : 0) + (Input.Down( InputButton.Right ) ? -1 : 0);

			Vector3 moveDir = new Vector3( currentInput.forwards, currentInput.sidewards, 0 );
			MoveDirection = (moveDir * Input.Rotation).WithZ( 0 ).Normal;
		}
	}
}
