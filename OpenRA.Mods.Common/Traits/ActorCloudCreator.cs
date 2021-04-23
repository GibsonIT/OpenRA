using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("An actor with this trait is a spawner for actor clouds. I.e. have a local cloud")]
	public class ActorCloudCreatorInfo : TraitInfo, Requires<AttackBaseInfo>
	{
		[Desc("Type of ActorSpawner with which it connects.")]
		public readonly HashSet<string> Types = new HashSet<string>() { };

		public override object Create(ActorInitializer init) { return new Traits.ActorCloudCreator(init, this); }
	}

	public class ActorCloudCreator : IActorCloudCreator
	{
		readonly Actor self;
		readonly ActorCloudCreatorInfo info;
		readonly IEnumerable<AttackBase> activeAttackBases;
		readonly WDist fromRadius;

		public ActorCloudCreator(ActorInitializer init, ActorCloudCreatorInfo info)
		{
			self = init.Self;
			this.info = info;
			activeAttackBases = self.TraitsImplementing<AttackBase>().ToArray().Where(Exts.IsTraitEnabled);
			var scanRadius = self.TraitOrDefault<AutoTarget>()?.Info?.ScanRadius ?? 0;
			if (scanRadius != 0)
				fromRadius = WDist.FromCells(scanRadius);
		}

		public IEnumerable<Actor> GetActorsInLocalCloud()
		{
			// More logic here for "layers" and speed ups (exclusion)

			// Start by finding finding the weapon with the longest range
			// TODO: Can range calculation be placed in constructor?
			var range = WDist.Zero;
			foreach (var ab in activeAttackBases)
			{
				// If we can't attack right now, there's no need to try and find a target.
				var attackStances = ab.UnforcedAttackTargetStances();
				if (attackStances == OpenRA.Traits.PlayerRelationship.None) continue;
				var attackRange = ab.GetMaximumRange();
				var newRange = fromRadius > attackRange ? fromRadius : attackRange;
				if (newRange > range)
					range = newRange;
			}

			var targetsInRange = self.World.FindActorsInCircle(self.CenterPosition, range);
			return targetsInRange;
		}

		public Actor Actor => self;
	}
}
