using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable InconsistentNaming
    internal abstract class EntityProviderBase : ModuleBase, IEntityProvider
    {
        protected List<IEntityWrapper> _entityWrappers = new List<IEntityWrapper>(); 

        /// <summary>
        /// List of IEntityWrapper objects available for use.
        /// </summary>
        public ReadOnlyCollection<IEntityWrapper> EntityWrappers
        {
            get { return _entityWrappers.AsReadOnly(); }
        }

        protected Dictionary<Int64, IEntityWrapper> _entityWrappersById = new Dictionary<long, IEntityWrapper>();  

        /// <summary>
        /// IEntityWrapper objects indexed by Entity ID.
        /// </summary>
        public Dictionary<Int64, IEntityWrapper> EntityWrappersById
        {
            get { return _entityWrappersById; }
        }

        /// <summary>
        /// Do whatever is necessary to poulate the IEntityWrapper list.
        /// </summary>
        public abstract void PopulateEntities();
    }

    internal sealed class EntityProvider : EntityProviderBase
    {
        private readonly IIsxeveProvider _isxeveProvider;

        internal EntityProvider(IIsxeveProvider isxeveProvider)
        {
            _isxeveProvider = isxeveProvider;

            ModuleName = "EntityProvider";
            IsEnabled = true;
            PulseFrequency = 1;
        }

        public override void Pulse()
        {
            var methodName = "Pulse";
			LogTrace(methodName);

        	if (!ShouldPulse()) 
				return;

			PopulateEntities();
        }

		public override bool Initialize()
		{
			IsInitialized = true;
			return true;
		}

		public override bool OutOfFrameCleanup()
		{
			IsCleanedUpOutOfFrame = true;
			return true;
		}

    	public override void InFrameCleanup()
		{
            _entityWrappersById.Clear();

            foreach (EntityWrapper wrapper in _entityWrappers)
			{
				wrapper.Dispose();
			}

			_entityWrappers.Clear();
		}

        public override void PopulateEntities()
        {
            var methodName = "PopulateEntities";
			LogTrace(methodName);

            if (_isxeveProvider.Eve.EntitiesCount == 0)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Error: ISXEVE sees 0 entities. Forcing entity population.");
                _isxeveProvider.Eve.PopulateEntities(true);
			}

        	StartMethodProfiling("EVE.QueryEntities");
            var entities = _isxeveProvider.Eve.QueryEntities();
            EndMethodProfiling();
            //StartMethodProfiling("SortEntities");
            //entities = entities.OrderBy<Entity, double>(x => x.Distance).ToList();
            //EndMethodProfiling();

            StartMethodProfiling("CreateEntityWrappers");
            if (entities != null)
            {
                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                    //methodName, String.Format("Got {0} entities from query.", entities.Count)));
                //StartMethodProfiling("BuildEntityWrappers");
                foreach (var entity in entities)
                {
					if (LavishScriptObject.IsNullOrInvalid(entity))
					{
						continue;
					}

					//Ignore certain entities
					if (entity.CategoryID == (int)CategoryIDs.Charge)
					{
						continue;
					}

                    var entityWrapper = new EntityWrapper(entity);
                    _entityWrappers.Add(entityWrapper);

                    try
                    {
                        _entityWrappersById.Add(entityWrapper.ID, entityWrapper);
                    }
                    catch (Exception)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Error: An entity matching entity \"{0}\" id {1} is already being tracked.",
                            entityWrapper.ID, entityWrapper.Name);
                    }
                }
                //EndMethodProfiling();
            }
            else
            {
				LogMessage(methodName, LogSeverityTypes.Debug, "Got null result from QueryEntities.");
            }
            EndMethodProfiling();
        }
    }
    // ReSharper restore InconsistentNaming
}
