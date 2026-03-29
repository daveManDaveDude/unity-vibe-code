using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VibeCode.Platformer;

namespace VibeCode.Tests.PlayMode
{
    public class MainScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator MainSceneLoadsWithCameraAndPlayer()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            Assert.That(Camera.main, Is.Not.Null, "Expected Main scene to provide a Main Camera.");
            Assert.That(Object.FindAnyObjectByType<PlayerController2D>(), Is.Not.Null,
                "Expected Main scene to contain a PlayerController2D instance.");
        }
    }
}
