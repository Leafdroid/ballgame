
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ballers
{
	public partial class Ball : ModelEntity
	{
		public enum ControlType
		{
			Player,
			Replay
		}

		public ControlType Controller { get; set; }
		public Vector3 MoveDirection { get; set; }
		public ReplayData ReplayData { get; set; } = new ReplayData();

		private const float angToByte = 255f / 360f;
		private const float byteToAng = 360f / 255f;

		public class BallInput
		{
			public ushort data { get; private set; } = 0;

			public void Update( float forward, float left, float yaw )
			{
				bool moving = forward != 0 || left != 0;

				data = (ushort)(moving ? 256 : 0);

				if ( !moving )
					return;

				Vector3 rawDirection = new Vector3( forward, left, 0 ).Normal * Rotation.FromYaw( yaw );
				float directionYaw = rawDirection.EulerAngles.yaw;

				data += (byte)(MathF.Round( directionYaw * angToByte ) % 255);
			}

			public void Update( ushort data ) => this.data = data;

			public void Parse( out Vector3 direction )
			{
				bool moving = (data & 256) == 256;

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

		public BallInput ActiveInput { get; private set; } = new BallInput();

		public void Simulate()
		{
			if ( Controller == ControlType.Player )
			{
				ActiveInput.Update( Input.Forward, Input.Left, Input.Rotation.Yaw() );
				ReplayData.AddData( ActiveInput );
			}
			else if ( Controller == ControlType.Replay )
			{
				ushort input = ReplayData.GetNext();
				ActiveInput.Update( input );
			}

			ActiveInput.Parse( out Vector3 moveDirection );
			MoveDirection = moveDirection;

			SimulatePhysics();
		}

		public static readonly SoundEvent PopSound = new( "sounds/ball/pop.vsnd" );
	}
}
