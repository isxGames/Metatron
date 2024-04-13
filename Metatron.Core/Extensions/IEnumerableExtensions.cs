using System;
using System.Collections.Generic;
using System.Linq;

namespace Metatron.Core.Extensions
{
	public static class IEnumerableExtensions
	{
		public static List<T> RandomizeOrder<T>(this List<T> list)
		{
			//Get a random object for randomizing
			var random = new Random((int)DateTime.Now.ToBinary());

			//Build a dictionary with the index of the object in the enumerable and a random number
			var randomIndexPairs = new Dictionary<int, int>();

			for (var index = 0; index < list.Count; index++)
			{
				var randomNumber = random.Next();
				randomIndexPairs.Add(randomNumber, index);
			}

			var returnValue = new List<T>();

			//So the keys are the random numbers generated to pair with each index in the source list.
			//Sort them ascending, then grab the source list item frm the index paired with that #, and
			//add it to the return value.
			foreach (var randomNumber in randomIndexPairs.Keys.ToList().OrderBy(x => x))
			{
				returnValue.Add(list[randomIndexPairs[randomNumber]]);
			}

			return returnValue;
		}
	}
}
