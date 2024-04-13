using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EVE.ISXEVE.Interfaces;
using Metatron.BehaviorModules.PartialBehaviors;
using Metatron.Core.Interfaces;

namespace Metatron.Actions
{
    public class DropOffToJetissonContainerAction : IPartialBehaviorBase
    {
        private readonly ILogging _logging;
        private readonly IInventoryProvider _inventoryProvider;
        private readonly IEntityProvider _entityProvider;
        private readonly IJettisonContainer _jettisonContainer;

        public DropOffToJetissonContainerAction(ILogging logging, IInventoryProvider inventoryProvider, IEntityProvider entityProvider, IJettisonContainer jettisonContainer)
        {
            _logging = logging;
            _inventoryProvider = inventoryProvider;
            _entityProvider = entityProvider;
            _jettisonContainer = jettisonContainer;
        }

        public BehaviorExecutionResults Execute()
        {


            //if I have no jetcan, I should create one.
            if (_jettisonContainer.CurrentContainer == null)
            {
                var wasItemJettisoned = CreateJettisonContainer();

                return wasItemJettisoned ? BehaviorExecutionResults.Incomplete : BehaviorExecutionResults.Error;
            }

            return BehaviorExecutionResults.Complete;
        }

        /// <summary>
        /// Create a jettison container.
        /// </summary>
        /// <returns>True if an item was jettisoned, otherwise false.</returns>
        private bool CreateJettisonContainer()
        {
            IItem itemToJettison = null;
            //If I have ore hold items, prefer them.
            if (Core.Metatron.Ship.HasOreHold())
            {
                itemToJettison = _inventoryProvider.OreHoldCargo
                    .OrderByDescending(item => item.Volume * item.Quantity)
                    .FirstOrDefault();

                if (itemToJettison == null)
                {
                    itemToJettison = GetMostMassiveStackInCargo();
                }
            }

            //Otherwise, try to get the an item from cargo.
            if (itemToJettison == null)
            {
                itemToJettison = GetMostMassiveStackInCargo();
            }

            //If, after all this, I couldn't find an item, error out. There's nothing to jettison.
            if (itemToJettison == null) return false;

            //Actually jettison the item.
            itemToJettison.Jettison();
            return true;
        }

        private IItem GetMostMassiveStackInCargo()
        {
            return _inventoryProvider.Cargo
                                     .Where(item => item.CategoryID == (int)CategoryIDs.Asteroid)
                                     .OrderByDescending(item => item.Volume * item.Quantity)
                                     .FirstOrDefault();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
