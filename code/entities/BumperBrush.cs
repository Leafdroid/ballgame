using Sandbox;

namespace Ballers
{

	[Library( "func_bumper" )]
	[SandboxEditor.SupportsSolid]
	public partial class BumperBrush : ModelEntity
	{
		[Property( "force", Title = "Force" )]
		[Net] public float Force { get; private set; } = 500f;

		[Property( "pitch", Title = "Pitch" )]
		[Net] public float Pitch { get; private set; } = 1f;


		private TimeSince timeSinceBonk = 0f;
		private bool justBonked = false;

		public override void Spawn()
		{
			base.Spawn();

			SharedSpawn();
			Transmit = TransmitType.Always;
		}


		public static readonly SoundEvent BoingSound = new( "sounds/ball/boing.vsnd" )
		{
			Pitch = 0.95f,
			PitchRandom = 0.05f,
			Volume = 0.65f,
			//DistanceMax = 2048f,
		};


		public void Bonk( Ball bonker, Vector3 pos )
		{
			if ( IsServer )
				ClientImpactSound( this, bonker, pos );
			else if ( Local.Pawn == bonker )
				ImpactSound( pos );
		}

		private float Bezier( float a, float b, float c, float d, float t )
		{
			float n = 1f - t;
			float ab = a * n + b * t;
			float bc = b * n + c * t;
			float cd = c * n + d * t;
			float abbc = ab * n + bc * t;
			float bccd = bc * n + cd * t;
			return abbc * n + bccd * t;
		}

		[Event.Frame]
		public void Frame()
		{
			if ( SceneObject == null )
				return;

			if ( justBonked )
			{
				timeSinceBonk = 0f;
				justBonked = false;
			}

			float[] animationKeys = new float[6] { 1f, 1.4f, 0.6f, 1.2f, 0.9f, 1f };
			float scale;

			if ( timeSinceBonk < 0.25f )
				scale = Bezinterp( animationKeys, timeSinceBonk * 4f );
			else
				scale = 1f;

			if ( scale != SceneObject.Transform.Scale )
				SceneObject.Transform = new Transform( Position, Rotation, scale );
		}

		public static float Bezinterp( float[] values, float t )
		{
			int valueCount = values.Length;

			switch ( valueCount )
			{
				case 0:
					return t;
				case 1:
					return values[0] * t;
				case 2:
					return values[0] * (1f - t) + values[1] * t;
				default:
					int iteration = 1;
					while ( iteration != valueCount )
					{
						for ( int i = 0; i < valueCount - iteration; i++ )
						{
							float val = values[i];
							float nextVal = values[i + 1];

							values[i] = val * (1f - t) + nextVal * t;
						}
						iteration++;
					}
					return values[0];
			}
		}

		private void ImpactSound( Vector3 pos )
		{
			justBonked = true;

			Sound.FromWorld( BoingSound.ResourceName, pos ).SetPitch( Pitch );
		}

		[ClientRpc]
		public static void ClientImpactSound( BumperBrush bumper, Ball ball, Vector3 pos )
		{
			if ( ball != Local.Pawn || ball.Controller == Ball.ControlType.Replay )
			{
				bumper.ImpactSound( pos );
			}
		}

		private void SharedSpawn()
		{
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			EnableDrawing = true;
			Tags.Add("solid");
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			SharedSpawn();
		}
	}
}
