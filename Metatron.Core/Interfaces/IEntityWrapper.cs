using System;
namespace Metatron.Core
{
	public interface IEntityWrapper
	{
		void Activate();
		void AlignTo();
		void Approach();
		double ArmorPct { get; }
	    double Bounty { get; }
		bool BeingTargeted { get; }
		int CategoryID { get; }
		void CreateBookmark(string label);
		double Distance { get; }
		void Dock();
		int GroupID { get; }
		long ID { get; }
		bool IsLockedTarget { get; }
		bool IsNPC { get; }
		bool IsPC { get; }
		bool IsTargetingMe { get; }
		void Jump();
		void LockTarget();
		void MakeActiveTarget();
		string Name { get; }
		void Open();
		void RequestObjectRefresh();
		void SetName(string name);
		double ShieldPct { get; }
		void StackAllCargo();
		double StructurePct { get; }
		EVE.ISXEVE.Entity ToEntity { get; }
		int TypeID { get; }
		void UnlockTarget();
		void WarpTo(int warpToMeters);
		double X { get; }
		double Y { get; }
		double Z { get; }
	    bool HaveLootRights { get; }
	    double Radius { get; }

	    /// <summary>
		/// Orbit this entity at the given distance.
		/// </summary>
		/// <param name="distance"></param>
		void Orbit(int distance);

        /// <summary>
        /// Keep this entity at the given range.
        /// </summary>
        /// <param name="distance"></param>
	    void KeepAtRange(int distance);
	}
}
