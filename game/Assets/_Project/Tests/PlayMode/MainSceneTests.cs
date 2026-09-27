using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // Smoke test: the game's entry scene loads and runs without logging errors.
    // Any Debug.LogError or exception during the test fails it automatically.
    public class MainSceneTests
    {
        [UnityTest]
        public IEnumerator MainScene_LoadsAndRunsForOneSecond()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            Assert.AreEqual("Main", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindAnyObjectByType<Camera>(), "Main scene has no camera");

            yield return new WaitForSeconds(1f);
        }
    }
}
