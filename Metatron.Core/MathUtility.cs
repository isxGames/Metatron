using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Metatron.Core
{
    public class MathUtility
    {
        public double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(
                Math.Pow(x1 - x2, 2) +
                Math.Pow(y1 - y2, 2) +
                Math.Pow(z1 - z2, 2)
                );
        }
    }
}
