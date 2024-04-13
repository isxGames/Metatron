using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Metatron.Core.Extensions
{
    public static class EnumExtensions
    {
        public static int GetSetBitCount(this Enum value)
        {
            var count = 0;
            var numericValue = (int) (object) value;

            while (numericValue != 0)
            {
                numericValue &= numericValue - 1;
                count++;
            }

            return count;
        }

        public static int GetMatchingBitCount(this Enum value, Enum toCompare)
        {
            var firstValue = (int) (object) value;
            var secondValue = (int) (object) toCompare;

            var result = firstValue & secondValue;

            var count = 0;

            while (result != 0)
            {
                result &= result - 1;
                count++;
            }

            return count;
        }

        public static bool IsFlagSet(this Enum value, Enum toCheckFor)
        {
            var firstValue = (int) (object) value;
            var secondValue = (int) (object) toCheckFor;

            var result = firstValue & secondValue;

            return result != 0;
        }
    }
}
