using System;
using EVE.ISXEVE;

namespace Metatron.Core
{
    /// <summary>
    /// Represents everything an IEntityWrapper must be able to set or cache and do.
    /// </summary>
    public abstract class EntityWrapperBase : IEntityWrapper
    {
        #region Entity members - to be set, cached, and/or updated
        public abstract string Name { get; protected set; }
        public abstract Int64 ID { get; protected set; }
        public abstract int GroupID { get; protected set; }
        public abstract int TypeID { get; protected set; }
        public abstract int CategoryID { get; protected set; }
        public abstract double ShieldPct { get; protected set; }
        public abstract double ArmorPct { get; protected set; }
        public abstract double StructurePct { get; protected set; }
        public abstract double X { get; protected set; }
        public abstract double Y { get; protected set; }
        public abstract double Z { get; protected set; }
        public abstract double Distance { get; protected set; } // override the inherited distance calc
        public abstract bool BeingTargeted { get; internal set; }
        public abstract bool IsLockedTarget { get; internal set; }
        public abstract bool IsNPC { get; protected set; }
        public abstract bool IsPC { get; protected set; }
        public abstract bool IsTargetingMe { get; internal set; }
        public abstract Entity ToEntity { get; protected set; }
        public abstract bool HaveLootRights { get; }
        public abstract double Radius { get; }
        public abstract double Bounty { get; }

        #endregion

        #region Entity methods - must be wrapped
        /// <summary>
        /// Make this entity the active target.
        /// </summary>
        public abstract void MakeActiveTarget();

        /// <summary>
        /// Lock this entity.
        /// </summary>
        public abstract void LockTarget();

        /// <summary>
        /// Unlock this entity.
        /// </summary>
        public abstract void UnlockTarget();

        /// <summary>
        /// Dock at this entity.
        /// </summary>
        public abstract void Dock();

        /// <summary>
        /// Jump at this entity.
        /// </summary>
        public abstract void Jump();

        /// <summary>
        /// Activate this entity.
        /// </summary>
        public abstract void Activate();

    	/// <summary>
    	/// Open the cargo of this entity.
    	/// </summary>
    	public abstract void Open();

        /// <summary>
        /// Stack the cargo of this entity.
        /// </summary>
        public abstract void StackAllCargo();

        /// <summary>
        /// Set the name of this entity to the specified name.
        /// </summary>
        /// <param name="name">Name to be used.</param>
        public abstract void SetName(string name);

        /// <summary>
        /// Warp to this entity at a specified distance.
        /// </summary>
        /// <param name="warpToMeters">_distance at which we warp.</param>
        public abstract void WarpTo(int warpToMeters);

        /// <summary>
        /// AlignTo to this entity.
        /// </summary>
        public abstract void AlignTo();

        /// <summary>
        /// Approach this entity.
        /// </summary>
        public abstract void Approach();

    	/// <summary>
    	/// Orbit this entity at the given distance.
    	/// </summary>
    	/// <param name="distance"></param>
    	public abstract void Orbit(int distance);

        /// <summary>
        /// Keep this entity at the given range.
        /// </summary>
        /// <param name="distance"></param>
        public abstract void KeepAtRange(int distance);

        /// <summary>
        /// Bookmark this entity with a specified label.
        /// </summary>
        /// <param name="label">Label of the bookmark.</param>
        public abstract void CreateBookmark(string label);

        public abstract void RequestObjectRefresh();
        #endregion
    }

	public sealed class EntityWrapper : EntityWrapperBase, IDisposable
    {
        #region Properties
        Entity _entity;

        public override string Name
        {
            get
            {
                return _entity.Name;
            }
            protected set { }
        }

		private Int64 _id;
        public override Int64 ID
        {
            get
            {
				return _id;
            }
			protected set { _id = value; }
        }

        public override int GroupID
        {
            get
            {
                return _entity.GroupID;
            }
            protected set { }
        }

        public override int TypeID
        {
            get
            {
                return _entity.TypeID;
            }
            protected set { }
        }

        public override int CategoryID
        {
            get
            {
                return _entity.CategoryID;
            }
            protected set { }
        }

        public override double ShieldPct
        {
            get
            {
                return _entity.ShieldPct;
            }
            protected set { }
        }

        public override double ArmorPct
        {
            get
            {
                return _entity.ArmorPct;
            }
            protected set { }
        }

        public override double StructurePct
        {
            get
            {
                return _entity.StructurePct;
            }
            protected set { }
        }

        public override double X
        {
            get
            {
                return _entity.X;
            }
            protected set { }
        }

        public override double Y
        {
            get
            {
                return _entity.Y;
            }
            protected set { }
        }

        public override double Z
        {
            get
            {
                return _entity.Z;
            }
            protected set { }
        }

        public override double Distance
        {
            get
            {
                return _entity.Distance;
            }
            protected set { }
        }

        public override bool BeingTargeted
        {
            get
            {
                return _entity.BeingTargeted;
            }
            internal set { }
        }

        public override bool IsLockedTarget
        {
            get
            {
                return _entity.IsLockedTarget;
            }
            internal set { }
        }

        public override bool IsNPC
        {
            get
            {
                return _entity.IsNPC;
            }
            protected set { }
        }

        public override bool IsPC
        {
            get
            {
                return _entity.IsPC;
            }
            protected set { }
        }

        public override bool IsTargetingMe
        {
            get
            {
                return _entity.IsTargetingMe;
            }
            internal set { }
        }

	    public override bool HaveLootRights
	    {
	        get { return _entity.HaveLootRights; }
	    }

	    public override double Radius
	    {
            get { return _entity.Radius; }
	    }

	    public override double Bounty
        {
            get { return _entity.Bounty; }
        }

        public override Entity ToEntity
        {
            get
            {
                return _entity;
            }
            protected set { }
        }
        #endregion

        internal EntityWrapper(Entity entityToCache)
        {
            _entity = entityToCache;
			ID = _entity.ID;
        }
		
		private bool _isDisposed;

		private void Dispose(bool isDisposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (isDisposing)
			{
                if (_entity != null)
                {
                    _entity.Dispose();
                    _entity = null;
                }
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

        #region methods
        public override void Activate()
        {
            _entity.Activate();
        }

        public override void AlignTo()
        {
            _entity.AlignTo();
        }

        public override void Approach()
        {
            _entity.Approach();
        }

	    public override void KeepAtRange(int distance)
	    {
	        _entity.KeepAtRange(distance);
	    }

	    public override void CreateBookmark(string label)
        {
            _entity.CreateBookmark(label);
        }

        public override void Dock()
        {
            _entity.Dock();
        }

        public override void Jump()
        {
            _entity.Jump();
        }

        public override void LockTarget()
        {
            _entity.LockTarget();
        }

        public override void MakeActiveTarget()
        {
            _entity.MakeActiveTarget();
        }

        public override void Open()
        {
            _entity.Open();
        }

        public override void SetName(string name)
        {
            _entity.SetName(name);
        }

        public override void StackAllCargo()
        {
            _entity.StackAllCargo();
        }

        public override void UnlockTarget()
        {
            _entity.UnlockTarget();
        }

        public override void WarpTo(int warpToMeters)
        {
            _entity.WarpTo(warpToMeters);
        }

		public override void Orbit(int distance)
		{
			_entity.Orbit(distance);
		}

        public override void RequestObjectRefresh() { }
        #endregion
    }
}
