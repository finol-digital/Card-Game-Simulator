using System.Collections;
using Cgs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class CardGameManagerTests
    {
        [UnityTest]
        public IEnumerator CardGameManagerStarts()
        {
            var manager = new GameObject();
            manager.AddComponent<CardGameManager>();
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(CardGameManager.Current.HasReadProperties);
        }
    }
}
