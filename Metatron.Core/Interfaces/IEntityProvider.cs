using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Metatron.Core.Interfaces
{
    public interface IEntityProvider
    {
        /// <summary>
        /// Do whatever is necessary to poulate the IEntityWrapper list.
        /// </summary>
        void PopulateEntities();

        /// <summary>
        /// List of IEntityWrapper objects available for use.
        /// </summary>
        ReadOnlyCollection<IEntityWrapper> EntityWrappers { get; }

        /// <summary>
        /// IEntityWrapper objects indexed by Entity ID.
        /// </summary>
        Dictionary<Int64, IEntityWrapper> EntityWrappersById { get; }
    }
}
