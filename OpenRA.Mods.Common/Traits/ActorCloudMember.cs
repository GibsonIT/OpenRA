using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("An actor with this trait is a spawner for actor clouds. I.e. have a local cloud")]
	public class ActorCloudMemberInfo : TraitInfo
	{
		[Desc("Area of potential effect radius. Can be 0. The own body is included in this")]
		public readonly int AopeRadius = -1;

		[Desc("Only relevant if radius is not used")]
		public readonly CVec AopeDimensions = new CVec(0, 0);

		public override object Create(ActorInitializer init) { return new Traits.ActorCloudMember(init, this); }
	}

	public class ActorCloudMember : IActorCloudMember
	{
		readonly Actor self;
		readonly bool useRadius;
		readonly WDist aopeRadius;
		readonly WVec aopeDimensions;
		readonly WPos initTopLeft;
		readonly ActorCloudMemberInfo info;

		public ActorCloudMember(ActorInitializer init, ActorCloudMemberInfo info)
		{
			self = init.Self;
			this.info = info;

			var x = self.Location.X * ActorCloudsCreator.ActorCloudsResPerMapCell;
			var y = self.Location.Y * ActorCloudsCreator.ActorCloudsResPerMapCell;
			initTopLeft = new WPos(x, y, 0);

			// Use circle aope if radius exists, else use rect aope
			if (info.AopeRadius != -1)
			{
				aopeRadius = new WDist((int)Math.Ceiling(((decimal)info.AopeRadius) / ActorCloudsCreator.ActorCloudsResDivider));
				useRadius = true;
			}
			else
			{
				x = info.AopeDimensions.X * ActorCloudsCreator.ActorCloudsResPerMapCell;
				y = info.AopeDimensions.Y * ActorCloudsCreator.ActorCloudsResPerMapCell;
				aopeDimensions = new WVec(x, y, 0);
				useRadius = false;
			}
		}

		public bool UseRadius()
		{
			return useRadius;
		}

		/// <summary>
		/// In actor clouds resolution
		/// </summary>
		public WDist GetAopeRadius()
		{
			return aopeRadius;
		}

		public WVec GetAopeDimensions()
		{
			return aopeDimensions;
		}

		/// <summary>
		/// In actor clouds resolution
		/// </summary>
		public WPos GetActorCenterPosition()
		{
			return new WPos(
				self.CenterPosition.X / ActorCloudsCreator.ActorCloudsResDivider,
				self.CenterPosition.Y / ActorCloudsCreator.ActorCloudsResDivider,
				0);
		}

		/// <summary>
		/// Returns the initial top left position of the actor
		/// </summary>
		public WPos GetActorInitTopLeftPosition()
		{
			return initTopLeft;
		}

		public Actor Actor => self;
	}
}
