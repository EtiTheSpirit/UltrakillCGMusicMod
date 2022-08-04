using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UltrakillCGMusicMod {

	/// <summary>
	/// Look at all that code bloat!
	/// </summary>
	public static class BloatUtils {

		private static readonly System.Random RNG = new System.Random();

		/// <summary>
		/// An alias to Unity's <see cref="Transform.Find(string)"/> method that operates and returns GameObjects.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static GameObject FindFirstChild(this GameObject parent, string name) => parent.transform.Find(name)?.gameObject;

		/// <summary>
		/// Returns a random element out of an array.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <returns></returns>
		public static T Random<T>(this T[] array) => array[RNG.Next(array.Length)];

	}
}
