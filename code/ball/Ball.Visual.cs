
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ballers
{
	public partial class Ball
	{
		public ModelEntity Model { get; private set; }

		public void SetupModel()
		{
			if ( !IsClient )
				return;

			Model = new ModelEntity( "models/ball.vmdl" );

			if ( !Owner.IsValid() )
				return;

			float hue = 0;
			if ( Owner.IsBot )
			{
				hue = Rand.Float( 360f );
			}
			else
			{
				int id = (int)(Owner.PlayerId & 255);
				Random seedColor = new Random( id );
				hue = (float)seedColor.NextDouble() * 360f;
			}

			Color ballColor = new ColorHsv( hue, 0.8f, 1f );
			Color ballColor2 = new ColorHsv( (hue + 30f) % 360, 0.8f, 1f );

			Model.SceneObject.SetValue( "tint", ballColor );
			Model.SceneObject.SetValue( "tint2", ballColor2 );
		}

		public void UpdateModel()
		{
			if ( !IsClient )
				return;

			if ( Model.IsValid() )
			{
				Model.Position = Owner == Local.Client ? Position : RealPosition;

				if ( IsClient && Velocity.LengthSquared > 0.0f )
				{
					Vector3 vel = Owner == Local.Client ? Velocity : RealVelocity;

					var dir = vel.Normal;
					var axis = new Vector3( -dir.y, dir.x, 0.0f );
					var angle = (vel.Length * Time.Delta) / (50.0f * (float)Math.PI);
					Model.Rotation = Rotation.FromAxis( axis, 180.0f * angle ) * Model.Rotation;
				}
			}
		}
		
	}
}
