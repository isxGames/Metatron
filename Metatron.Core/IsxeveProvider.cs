using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    using EVE = EVE.ISXEVE.EVE;

    /// <summary>
    /// Provide a means of acquiring ISXEVE objects and validating references
    /// </summary>
    public class IsxeveProvider : IIsxeveProvider
    {
        private ISXEVE _isxeve;
        private IEve _eve;
        private Me _me;

        public ISXEVE Isxeve
        {
            get
            {
                if (LavishScriptObject.IsNullOrInvalid(_isxeve))
                    _isxeve = new ISXEVE();

                return _isxeve;
            }
        }

        public IEve Eve
        {
            get
            {
                if (LavishScriptObject.IsNullOrInvalid(_eve))
                    _eve = new EVE();

                return _eve;
            }
        }

        public Me Me
        {
            get
            {
                if (LavishScriptObject.IsNullOrInvalid(_me))
                    _me = new Me();

                return _me;
            }
        }
    }
}
