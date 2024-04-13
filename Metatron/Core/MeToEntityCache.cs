using System;
using System.Linq;
using EVE.ISXEVE;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    public sealed class MeToEntityCache : ModuleBase, IMeToEntityCache
    {
        private Entity _toEntity;
        private IEntityWrapper _myEntityWrapper;

        public IEntityWrapper Approaching { get; private set; }

        public string Name { get; private set; }
        public int Mode { get; private set; }
        public int TypeId { get; private set; }
        public int GroupId { get; private set; }
        public Int64 Id { get; private set; }
        public bool IsCloaked { get; private set; }
        public bool IsWarpScrambled { get; private set; }
        public double Velocity { get; private set; }
        public double MaxVelocity { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public double Radius { get; private set; }

        internal MeToEntityCache()
        {
            ModuleManager.CachesToPulse.Add(this);
            PulseFrequency = 1;
            base.ModuleName = "Me_ToEntityCache";
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            if (!ShouldPulse())
                return;

            if (!Metatron.MeCache.InSpace && Metatron.MeCache.InStation)
                return;

            if (Metatron.EntityProvider.EntityWrappers.Count == 0)
            {
                EndPulseProfiling();
                return;
            }
 
            //Find the matching IEntityWrapper for us
            //StartMethodProfiling("_getMyEntity");
            _myEntityWrapper = Metatron.EntityProvider.EntityWrappers.FirstOrDefault(entity => entity.Name == Metatron.MeCache.Name);
            //EndMethodProfiling();

            if (_myEntityWrapper == null)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Me_EntityWrapper null!");
                return;
            }

            //Set members from the wrapper class
            Id = _myEntityWrapper.ID;
            Name = _myEntityWrapper.Name;
            TypeId = _myEntityWrapper.TypeID;
            GroupId = _myEntityWrapper.GroupID;
            X = _myEntityWrapper.X;
            Y = _myEntityWrapper.Y;
            Z = _myEntityWrapper.Z;
            Radius = _myEntityWrapper.Radius;

            //Populate whatever we can from the main class, followed by cachedentity, then entity
            //update the Entity reference
            _toEntity = _myEntityWrapper.ToEntity;

            if (LavishScriptObject.IsNullOrInvalid(_toEntity))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "Error: Me.ToEntity was null or invalid.");
                EndPulseProfiling();
                return;
            }

            //Set what can only be set from Toentity
            Mode = _toEntity.Mode;
            IsWarpScrambled = _toEntity.IsWarpScrambled;
            IsCloaked = _toEntity.IsCloaked;
            MaxVelocity = _toEntity.MaxVelocity;
            Velocity = _toEntity.Velocity;

            if (!LavishScriptObject.IsNullOrInvalid(_toEntity.Approaching))
            {
                var approachingEntityId = _toEntity.Approaching.ID;
                if (Metatron.EntityProvider.EntityWrappersById.ContainsKey(approachingEntityId))
                {
                    Approaching = Metatron.EntityProvider.EntityWrappersById[approachingEntityId];
                }
            }

            EndPulseProfiling();
        }

        public override void InFrameCleanup()
        {
            if (!LavishScriptObject.IsNullOrInvalid(_toEntity))
            {
                _toEntity.Dispose();
            }

            Approaching = null;
        }

        public double DistanceTo(Int64 entityId)
        {
            var methodName = "DistanceTo";
            LogTrace(methodName, "EntityID: {0}", entityId);

            var entity = Metatron.EntityProvider.EntityWrappersById[entityId];
            return entity.Distance;
        }

        new public double DistanceTo(double x, double y, double z)
        {
            var methodName = "DistanceTo";
            LogTrace(methodName, "X: {0}, Y: {1}, Z: {2}", x, y, z);

            return Math.Sqrt(
                Math.Pow(X - x, 2) +
                Math.Pow(Y - y, 2) +
                Math.Pow(Z - z, 2));
        }

        public bool IsValid
        {
            get
            {
                return !LavishScriptObject.IsNullOrInvalid(_toEntity);
            }
        }
    }
}