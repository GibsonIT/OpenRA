using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA
{
	public class ActorCloudsCreator
	{
		/// <summary>
		/// A divider on "normal" position & length properties.
		///
		/// Should be changed to a mod constant
		/// </summary>
		public const int ActorCloudsResDivider = 256;

		/// <summary>
		/// Resolution per map cell
		/// </summary>
		public const int ActorCloudsResPerMapCell = 1024 / ActorCloudsResDivider;

		struct MapCell
		{
			public byte Value;
			public List<Actor> Actors;
		}

		// ReSharper disable once SA1025
		static readonly int[,] CustomAdjacentLookup =
		{
			{ -1, -1 },			// Top left
			{ 0, -1 },			// Top
			{ 1, -1 },			// Top right
			{ -1, 0 },			// Mid left
			{ 1, 0 },			// Mid right
			{ -1, 1 },			// Bottom left
			{ 0, 1 },			// Bottom
			{ 1, 1 },			// Bottom right
		};

		readonly int mapCellsWidth;
		readonly int mapCellsWidthMaxIndex;
		readonly int mapCellsHeight;
		readonly int mapCellsHeightMaxIndex;
		readonly MapCell[][] mapCells;

		public ActorCloudsCreator(int mapSizeX, int mapSizeY)
		{
			mapCellsWidth = mapSizeX * ActorCloudsResPerMapCell;
			mapCellsWidthMaxIndex = mapCellsWidth - 1;
			mapCellsHeight = mapSizeY * ActorCloudsResPerMapCell;
			mapCellsHeightMaxIndex = mapCellsHeight - 1;
			mapCells = new MapCell[mapCellsWidth][];

			for (var x = 0; x < mapCellsWidth; x++)
			{
				mapCells[x] = new MapCell[mapCellsHeight];

				for (var y = 0; y < mapCellsHeight; y++)
				{
					mapCells[x][y].Actors = new List<Actor>();
				}
			}
		}

		// Can be made faster e.g. by using List pools. Currently a lot of allocations are done each tick
		public List<HashSet<Actor>> CalculateClouds(IEnumerable<TraitPair<IActorCloudMember>> actorsWithTrait)
		{
			var clouds = new List<HashSet<Actor>>();
			WPos pos;

			// Enter actors and their AoPE in the grid
			foreach (var traitPair in actorsWithTrait)
			{
				var actor = traitPair.Actor;
				if (traitPair.Trait.UseRadius())
				{
					var aopeRadius = traitPair.Trait.GetAopeRadius();
					pos = traitPair.Trait.GetActorCenterPosition();
					AddCircleAopeToMap(pos, aopeRadius);
				}
				else
				{
					var aopeDimensions = traitPair.Trait.GetAopeDimensions();
					pos = traitPair.Trait.GetActorInitTopLeftPosition();
					AddRectAopeToMap(pos, aopeDimensions);
				}

				// Doesn't really matter where in the cloud we add the actor
				mapCells[pos.X][pos.Y].Actors.Add(actor);
			}

			if (mapCellsHeight < 100)
				return clouds;

			for (var y = 0; y < mapCellsHeight; y++)
			{
				for (var x = 0; x < mapCellsWidth; x++)
				{
					if (mapCells[x][y].Value == 0) continue;

					// Faster to create list here than if check inside function
					var cloud = SearchAndAddForCloudAt(x, y, new List<Actor>());
					if (cloud.Count == 0) continue;
					clouds.Add(new HashSet<Actor>(cloud));
					x++;
				}
			}

			return clouds;
		}

		void AddRectAopeToMap(WPos topLeft, WVec dim)
		{
			var maxY = topLeft.Y + dim.Y;
			var maxX = topLeft.X + dim.X;

			for (var y = topLeft.Y; y < maxY; y++)
			{
				for (var x = topLeft.X; x < maxX; x++)
				{
					mapCells[x][y].Value = 1;
				}
			}
		}

		void AddCircleAopeToMap(WPos centerPosition, WDist aopeRadius)
		{
			// This implementation might be problematic..
			var radius = aopeRadius.Length;
			var radiusSquared = aopeRadius.LengthSquared;

			// All actors should at least have position (0, 0)
			var x = centerPosition.X - radius > 0 ? -radius : 0;
			var minY = centerPosition.Y - radius > 0 ? -radius : 0;
			for (; x <= radius; x++)
			{
				var tempXPos = centerPosition.X + x;
				var tempXSquared = x * x;
				for (var y = minY; y <= radius; y++)
					if (tempXSquared + y * y <= radiusSquared)
						mapCells[tempXPos][centerPosition.Y + y].Value = 1;
			}
		}

		/// <summary>
		/// Should only ever be called on non 0 cells
		/// 1 = yes, 0 = no
		/// </summary>
		List<Actor> SearchAndAddForCloudAt(int x, int y, List<Actor> actors, int firstPass = 1)
		{
			mapCells[x][y].Value = 0;
			if (mapCells[x][y].Actors.Count > 0)
			{
				actors.AddRange(mapCells[x][y].Actors);
				mapCells[x][y].Actors.Clear();
			}

			for (var i = 0 + (firstPass * 3); i < 8; i++)
			{
				var offsetX = x + CustomAdjacentLookup[i, 0];
				var offsetY = y + CustomAdjacentLookup[i, 1];

				// This check could be faster. E.g. split up the functions for first pass and remove some checks.
				if (offsetX < 0 || offsetY < 0 || offsetX > mapCellsWidthMaxIndex || offsetY > mapCellsHeightMaxIndex)
					continue;

				if (mapCells[offsetX][offsetY].Value == 0) continue;
				SearchAndAddForCloudAt(offsetX, offsetY, actors, 0);
			}

			return actors;
		}
	}
}
