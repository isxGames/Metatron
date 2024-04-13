using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatron.Core.Interfaces
{
    public interface IDogmaAttributeCache
    {
        ReadOnlyCollection<CachedDogma.Attribute> CachedDogmaAttributes { get; }
        Dictionary<int, CachedDogma.Attribute> CachedDogmaAttributesById { get; }
        void GetDogmaAttributeInfo(int attributeId);
        void RemoveDogmaAttribute(int attributeId);
    }

    public interface IDogmaEffectCache
    {
        ReadOnlyCollection<CachedDogma.Effect> CachedDogmaEffects { get; }
        Dictionary<int, CachedDogma.Effect> CachedDogmaEffectsById { get; }
        void GetDogmaEffectInfo(int attributeId);
        void RemoveDogmaEffect(int attributeId);
    }
}
