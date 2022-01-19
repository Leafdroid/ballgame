
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class Ball : Player
	{
		public enum ControlType
		{
			Player,
			Replay
		}

		[Net] public ControlType Controller { get; set; }
		public Vector3 MoveDirection { get; set; }
		public ReplayData ReplayData { get; set; } = new ReplayData();
		[Net, Predicted] public float PredictedStart { get; private set; }
		[Net] public int ActiveTick { get; private set; }
		public float SimulationTime => Time.Now - PredictedStart;
		public int PredictionTick => (int)(Global.TickRate * SimulationTime);

		public BallInput ActiveInput { get; private set; } = new BallInput();

		public override void Simulate( Client cl )
		{
			if ( Popped )
				return;

			if ( ActiveTick == 0 )
				PredictedStart = Time.Now;

			EyeRot = Input.Rotation;

			if ( Controller == ControlType.Player )
			{
				ActiveInput.Update();
				ReplayData.AddData( ActiveInput );
			}
			else if ( Controller == ControlType.Replay )
			{
				ushort input = ReplayData.GetNext( out bool finished );
				ActiveInput.Update( input );
			}

			ActiveInput.Parse( out Vector3 moveDirection, out bool reset );
			MoveDirection = moveDirection;

			SimulatePhysics();

			ActiveTick++;
		}

		public static readonly SoundEvent PopSound = new( "sounds/ball/pop.vsnd" );
	}

	public class BallInput
	{
		private const float angToByte = 255f / 360f;
		private const float byteToAng = 360f / 255f;

		public ushort data { get; private set; } = 0;

		/* ushort data structure
		bits: [dddddd][c][b][aaaaaaaa]
		a: movement angle in 1.4~ degree increments [0-255]
		b: is moving? [0-1]
		c: kill/reset button [0-1]
		d: repetitions for the same input [0-63]
		*/

		public void Update()
		{
			float forward = Input.Forward;
			float left = Input.Left;
			float yaw = Input.Rotation.Yaw();
			bool reset = Input.Pressed( InputButton.Reload );

			bool moving = forward != 0 || left != 0;

			data = (ushort)(moving ? 256 : 0);

			if ( !moving )
				return;

			Vector3 rawDirection = new Vector3( forward, left, 0 ).Normal * Rotation.FromYaw( yaw );
			float directionYaw = rawDirection.EulerAngles.yaw;

			data += (byte)(MathF.Round( directionYaw * angToByte ) % 255);
		}

		public void Update( ushort data ) => this.data = data;

		public void Parse( out Vector3 direction, out bool reset )
		{
			bool moving = (data & 256) == 256;
			reset = (data & 512) == 512;

			if ( moving )
			{
				float yaw = (data & 255) * byteToAng;
				direction = Rotation.FromYaw( yaw ).Forward;
			}
			else
			{
				direction = Vector3.Zero;
			}
		}

		public static bool operator ==( BallInput a, BallInput b ) => a.data == b.data;
		public static bool operator !=( BallInput a, BallInput b ) => a.data != b.data;
	}
}
