
using Sandbox;
using System;
using System.Collections.Generic;

namespace Ballers
{

	public partial class Ball : Player
	{
		private static Dictionary<int, Clothing> clothingResources = new();
		private static Clothing FindClothing( int id ) => clothingResources.TryGetValue( id, out Clothing clothing ) ? clothing : null;

		public SceneModel Terry { get; private set; }
		public ModelEntity TerryRagdoll { get; private set; }
		private bool dressed = false;

		[Net] public string ClothingData { get; set; }
		private ClothingContainer container = new();
		private List<SceneModel> clothingObjects = new();

		[ClientRpc]
		public static void RegisterClothing( int id, string model, string matGroup, int slotsOver, int slotsUnder, int hideBody )
		{
			if ( clothingResources.ContainsKey( id ) )
				return;

			Clothing clothing = new Clothing();
			clothing.Model = model;
			clothing.MaterialGroup = matGroup;
			clothing.SlotsOver = (Clothing.Slots)slotsOver;
			clothing.SlotsUnder = (Clothing.Slots)slotsUnder;
			clothing.HideBody = (Clothing.BodyGroups)hideBody;

			clothingResources.Add( id, clothing );
		}

		public static void DeliverClothing( Client cl )
		{
			/*
			foreach ( Clothing clothing in Clothing.All )
			{
				int id = clothing.ResourceId;
				string model = clothing.Model;
				string matGroup = clothing.MaterialGroup;
				int slotsOver = (int)clothing.SlotsOver;
				int slotsUnder = (int)clothing.SlotsUnder;
				int hideBody = (int)clothing.HideBody;

				RegisterClothing( id, model, matGroup, slotsOver, slotsUnder, hideBody );
			}
			*/
		}

		private void Deserialize()
		{
			container.Clothing.Clear();

			if ( string.IsNullOrWhiteSpace( ClothingData ) )
				return;

			try
			{
				var entries = System.Text.Json.JsonSerializer.Deserialize<ClothingContainer.Entry[]>( ClothingData );

				foreach ( var entry in entries )
				{
					var item = FindClothing( entry.Id );
					if ( item == null ) continue;
					container.Clothing.Add( item );
				}
			}
			catch ( Exception e )
			{
				Log.Warning( e, "Error deserailizing clothing" );
			}
		}

		public void DressTerry()
		{
			if ( clothingResources.Count == 0 )
				return;

			Deserialize();

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

				var anim = new SceneModel( Scene, c.Model, Terry.Transform );

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

			dressed = true;
		}

		public void DressRagdoll( ModelEntity ragdoll )
		{
			if ( clothingResources.Count == 0 )
				return;

			Deserialize();

			ragdoll.SetMaterialGroup( "Skin01" );

			foreach ( var c in container.Clothing )
			{
				if ( c.Model == "models/citizen/citizen.vmdl" )
				{
					ragdoll.SetMaterialGroup( c.MaterialGroup );
					continue;
				}
				var anim = new ModelEntity( c.Model, ragdoll );

				if ( !string.IsNullOrEmpty( c.MaterialGroup ) )
					anim.SetMaterialGroup( c.MaterialGroup );
			}

			foreach ( var group in container.GetBodyGroups() )
			{
				ragdoll.SetBodyGroup( group.name, group.value );
			}
		}

		public void SetupTerry()
		{
			Terry = new SceneModel( Scene, Model.Load( "models/citizen/citizen.vmdl" ), Transform.Zero );
		}

		public void UpdateTerry()
		{
			if ( Terry.IsValid() )
			{
				if ( !dressed )
					DressTerry();

				Terry.RenderingEnabled = EnableDrawing;

				Vector3 flatVelocity = Velocity - GetGravity().Normal * Velocity.Dot( GetGravity().Normal );
				Vector3 direction = flatVelocity.Normal;

				float speed = flatVelocity.Length;
				var forward = Terry.Rotation.Forward.Dot( flatVelocity );
				var sideward = Terry.Rotation.Right.Dot( flatVelocity );
				var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

				// base stance
				Terry.SetAnimParameter( "b_grounded", true );
				Terry.SetAnimParameter( "holdtype", 0 );
				Terry.SetAnimParameter( "aimat_weight", 0.5f ); // old
				Terry.SetAnimParameter( "aim_body_weight", 0.5f );

				if ( speed > 32f )
				{
					// rotation
					Rotation idealRotation = Rotation.LookAt( flatVelocity, -GetGravity().Normal );
					float turnSpeed = 0.02f;
					Terry.Rotation = Rotation.Slerp( Terry.Rotation, idealRotation, speed * Time.Delta * turnSpeed );
				}

				Terry.Position = Position - Terry.Rotation.Up * 35f;

				// look direction
				var lookDirection = direction;
				if ( Controller == ControlType.Player )
					lookDirection = (direction * 0.3f + EyeRotation.Forward * 0.7f);

				var aimPos = Position + lookDirection * 200f;
				var localPos = Terry.Transform.PointToLocal( aimPos );
				Terry.SetAnimParameter( "aim_eyes", localPos );
				Terry.SetAnimParameter( "aim_head", localPos );
				Terry.SetAnimParameter( "aim_body", localPos );

				// walk animation
				Terry.SetAnimParameter( "move_direction", angle );
				Terry.SetAnimParameter( "move_speed", speed );
				Terry.SetAnimParameter( "move_groundspeed", speed );
				Terry.SetAnimParameter( "move_y", sideward );
				Terry.SetAnimParameter( "move_x", forward );
				Terry.SetAnimParameter( "move_z", 0 );

				// update
				Terry.Update( RealTime.Delta );
				foreach ( var clothingObject in clothingObjects )
				{
					clothingObject.RenderingEnabled = EnableDrawing;
					clothingObject.Update( RealTime.Delta );
				}

			}
		}

		[ClientRpc]
		private void Ragdoll()
		{
			var ent = new ModelEntity();
			ent.Position = Terry.Position;
			ent.Rotation = Terry.Rotation;
			ent.MoveType = MoveType.Physics;
			ent.UsePhysicsCollision = true;
			ent.EnableAllCollisions = true;
			ent.CollisionGroup = CollisionGroup.Debris;
			ent.Model = Terry.Model;

			DressRagdoll( ent );

			int boneCount = Terry.Model.BoneCount;
			for ( int i = 0; i < boneCount; i++ )
				ent.SetBoneTransform( i, Terry.GetBoneWorldTransform( i ), true );

			ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;
			ent.PhysicsGroup.Velocity = Velocity;

			ent.SetInteractsAs( CollisionLayer.Debris );
			ent.SetInteractsWith( CollisionLayer.WORLD_GEOMETRY );
			ent.AddCollisionLayer( CollisionLayer.Debris );
			ent.SetInteractsWith( CollisionLayer.Debris );

			ent.DeleteAsync( 6.5f );

			TerryRagdoll = ent;
		}
		public void UpdateModel()
		{
			if ( Velocity.LengthSquared > 0.0f )
			{
				Vector3 flatVelocity = Velocity - GetGravity().Normal * Velocity.Dot( GetGravity().Normal );
				var axis = Vector3.Cross( flatVelocity.Normal, GetGravity().Normal ).Normal;
				var angle = (flatVelocity.Length * Time.Delta) / (40.0f * (float)Math.PI);
				Rotation = Rotation.FromAxis( axis, 180.0f * angle ) * Rotation;
			}
		}
	}
}
