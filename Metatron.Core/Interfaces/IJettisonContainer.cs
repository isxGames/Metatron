using System;
using System.Collections.Generic;
using EVE.ISXEVE.Interfaces;

namespace Metatron.Core.Interfaces
{
    public interface IJettisonContainer : ICargoContainer
    {
        Int64 CurrentContainerId { get; }
        void CreateJetCan(IItem itemToJettison);
        void SetActiveCan();
        List<string> GetFormattedCanNames();
        void RenameActiveCan(bool isFull);
        void RenameActiveCan();
        void MarkActiveCanFull();
        bool IsActiveCanFull();
        bool IsActiveContainerHalfFull();
    }
}
