
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ballers
{
	public partial class Ball : Player
	{
		private static Dictionary<int, Clothing> clothingResources = new();
		private static Clothing FindClothing( int id ) => clothingResources.TryGetValue( id, out Clothing clothing ) ? clothing : null;

		public AnimSceneObject Terry { get; private set; }
		private bool dressed = false;

		[Net] public string ClothingData { get; private set; }
		private Clothing.Container container = new();
		private List<AnimSceneObject> clothingObjects = new();

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
		}

		private void Deserialize()
		{
			container.Clothing.Clear();

			if ( string.IsNullOrWhiteSpace( ClothingData ) )
				return;

			try
			{
				var entries = System.Text.Json.JsonSerializer.Deserialize<Clothing.Container.Entry[]>( ClothingData );

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

				var model = Model.Load( c.Model );

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
			Terry = new AnimSceneObject( Model.Load( "models/citizen/citizen.vmdl" ), Transform.Zero );
		}

		public void UpdateTerry()
		{
			if ( Terry.IsValid() )
			{
				if ( !dressed )
					DressTerry();

				Terry.RenderingEnabled = EnableDrawing;

				Terry.Position = Position - Vector3.Up * 35f;

				Vector3 velocity = Velocity.WithZ( 0 );
				Vector3 direction = velocity.Normal;

				float speed = velocity.Length;
				var forward = Terry.Rotation.Forward.Dot( velocity );
				var sideward = Terry.Rotation.Right.Dot( velocity );
				var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

				// base stance
				Terry.SetAnimBool( "b_grounded", true );
				Terry.SetAnimInt( "holdtype", 0 );
				Terry.SetAnimFloat( "aimat_weight", 0.5f ); // old
				Terry.SetAnimFloat( "aim_body_weight", 0.5f );

				if ( speed > 32f )
				{
					// rotation
					Rotation idealRotation = Rotation.LookAt( velocity, Vector3.Up );
					float turnSpeed = 0.01f;
					Terry.Rotation = Rotation.Slerp( Terry.Rotation, idealRotation, speed * Time.Delta * turnSpeed );
					Terry.Rotation = Terry.Rotation.Clamp( idealRotation, 90f, out var change );
				}

				// look direction
				var lookDirection = direction;
				if ( Controller == ControlType.Player )
					lookDirection = (direction * 0.3f + EyeRot.Forward * 0.7f);

				var aimPos = Position + lookDirection * 200f;
				var localPos = Terry.Transform.PointToLocal( aimPos );
				Terry.SetAnimVector( "aim_eyes", localPos );
				Terry.SetAnimVector( "aim_head", localPos );
				Terry.SetAnimVector( "aim_body", localPos );

				// walk animation
				Terry.SetAnimFloat( "move_direction", angle );
				Terry.SetAnimFloat( "move_speed", speed );
				Terry.SetAnimFloat( "move_groundspeed", speed );
				Terry.SetAnimFloat( "move_y", sideward );
				Terry.SetAnimFloat( "move_x", forward );
				Terry.SetAnimFloat( "move_z", 0 );

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

			/*
			ent.CopyBonesFrom( Terry );
			ent.CopyBodyGroups( Terry );
			ent.CopyMaterialGroup( Terry );
			ent.TakeDecalsFrom( Terry );
			*/

			ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;
			ent.PhysicsGroup.Velocity = Velocity;

			ent.SetInteractsAs( CollisionLayer.Debris );
			ent.SetInteractsWith( CollisionLayer.WORLD_GEOMETRY );
			ent.SetInteractsExclude( CollisionLayer.Player | CollisionLayer.Debris );

			ent.DeleteAsync( 6.5f );
		}

		private float mode = 0f;
		public void UpdateModel()
		{
			Vector3 spinVelocity = Velocity;

			/*
			bool isLocal = IsClient && Owner.IsValid() && Owner.Client == Local.Client;
			Vector3 hVel = Velocity.WithZ( 0 );
			Vector3 moveDir = (isLocal ? MoveDirection : NetDirection);
			Vector3 spinVel = hVel;
			if ( moveDir != Vector3.Zero )
				spinVel = (moveDir * Acceleration + hVel) * 0.5f;

			mode += (moveDir != Vector3.Zero ? Time.Delta : -Time.Delta) * 2f;
			mode = mode < 0f ? 0f : (mode > 1f ? 1f : mode);

			Vector3 spinVelocity = ((moveDir * Acceleration * 0.5f + hVel * 1.5f) * 0.5f) * mode + hVel * (1f - mode);
			*/
			if ( spinVelocity.LengthSquared > 0.0f )
			{
				var dir = spinVelocity.Normal;
				var axis = new Vector3( -dir.y, dir.x, 0.0f );
				var angle = (spinVelocity.Length * Time.Delta) / (40.0f * (float)Math.PI);
				Rotation = Rotation.FromAxis( axis, 180.0f * angle ) * Rotation;
			}
		}
	}
}
