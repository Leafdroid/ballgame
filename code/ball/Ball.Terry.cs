
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ballers
{
	public partial class Ball : ModelEntity
	{
		public AnimSceneObject Terry { get; private set; }

		private string clothesData;
		private Clothing.Container container = new();
		private List<AnimSceneObject> clothingObjects = new();

		[ClientCmd("findhat")]
		public static void ClothingTest()
		{
			Clothing item = Resource.FromId<Clothing>( 1918103397 );
			Log.Info( item != null ? item.Name : "null" );
		}

		public void DressTerry()
		{
			container.Deserialize( clothesData );

			Terry.SetMaterialGroup( "Skin01" );

			foreach ( var model in clothingObjects )
			{
				model?.Delete();
			}
			clothingObjects.Clear();

			foreach ( var c in container.Clothing )
			{
				if ( c.Model == "models/citizen/citizen.vmdl" )
				{
					Terry.SetMaterialGroup( c.MaterialGroup );
					continue;
				}

				var model = Sandbox.Model.Load( c.Model );

				var anim = new AnimSceneObject( model, Terry.Transform );

				if ( !string.IsNullOrEmpty( c.MaterialGroup ) )
					anim.SetMaterialGroup( c.MaterialGroup );

				Terry.AddChild( "clothing", anim );
				clothingObjects.Add( anim );

				anim.Update( 1.0f );
			}

			foreach ( var group in container.GetBodyGroups() )
			{
				Terry.SetBodyGroup( group.name, group.value );
			}
		}

		public void SetupModels()
		{
			if ( !IsClient )
				return;

			Terry = new AnimSceneObject( Sandbox.Model.Load( "models/citizen/citizen.vmdl"), Transform.Zero );

			DressTerry();
		}

		public void UpdateTerry()
		{
			if ( Terry.IsValid() )
			{
				Terry.Position = Position - Vector3.Up * 35f;

				float speed = Velocity.Length;
				var forward = Terry.Rotation.Forward.Dot( Velocity );
				var sideward = Terry.Rotation.Right.Dot( Velocity );
				var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

				// base stance
				Terry.SetAnimBool( "b_grounded", true );
				Terry.SetAnimInt( "holdtype", 0 );
				Terry.SetAnimFloat( "aimat_weight", 0.5f ); // old
				Terry.SetAnimFloat( "aim_body_weight", 0.5f );

				// rotation
				Rotation idealRotation = Rotation.LookAt( Velocity.WithZ( 0 ), Vector3.Up );
				float turnSpeed = 0.01f;
				Terry.Rotation = Rotation.Slerp( Terry.Rotation, idealRotation, speed * Time.Delta * turnSpeed );
				Terry.Rotation = Terry.Rotation.Clamp( idealRotation, 90f, out var change );

				// look direction
				if (speed > 64f)
				{
					var aimDir = Velocity.WithZ(0).Normal; // Owner == Local.Client ? Input.Rotation.Forward : Velocity.Normal;
					var aimPos = Position + aimDir * 200f;
					var localPos = Terry.Transform.PointToLocal( aimPos );
					Terry.SetAnimVector( "aim_eyes", localPos );
					Terry.SetAnimVector( "aim_head", localPos );
					Terry.SetAnimVector( "aim_body", localPos );
				}

				// walk animation
				Terry.SetAnimFloat( "move_direction", angle );
				Terry.SetAnimFloat( "move_speed", speed );
				Terry.SetAnimFloat( "move_groundspeed", Velocity.WithZ( 0 ).Length );
				Terry.SetAnimFloat( "move_y", sideward );
				Terry.SetAnimFloat( "move_x", forward );
				Terry.SetAnimFloat( "move_z", 0 );

				// update
				Terry.Update( RealTime.Delta );
				foreach ( var clothingObject in clothingObjects )
					clothingObject.Update( RealTime.Delta );
			}
		}

		public void UpdateModel()
		{
			if ( Velocity.LengthSquared > 0.0f )
			{
				var dir = Velocity.Normal;
				var axis = new Vector3( -dir.y, dir.x, 0.0f );
				var angle = (Velocity.Length * Time.Delta) / (50.0f * (float)Math.PI);
				Rotation = Rotation.FromAxis( axis, 180.0f * angle ) * Rotation;
			}
		}

		public void DeleteModels()
		{
			if ( !IsClient )
				return;

			if ( Terry.IsValid() )
				Terry.Delete();
		}

	}
}
