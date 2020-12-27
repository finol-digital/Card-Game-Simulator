using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class CardGameLoadTests
    {
        [Test]
        public void CardGameDefTestsSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator CardGameDefTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
