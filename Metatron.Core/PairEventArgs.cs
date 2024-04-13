using System;

namespace Metatron.Core
{
    public class PairEventArgs<TA, TB> : EventArgs
    {
        public TA First { get; set; }
        public TB Second { get; set; }

        public PairEventArgs(TA first, TB second)
        {
            First = first;
            Second = second;
        }
    }
}
