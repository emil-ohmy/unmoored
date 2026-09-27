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

        [UnityTest]
        public IEnumerator Player_LandsOnGroundAndStaysThere()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "Main scene has no PlayerController (run MyGame > Build Prototype Scene)");

            yield return new WaitForSeconds(1f);

            Assert.IsTrue(player.IsGrounded, "Player is not grounded after 1 s");
            Assert.That(player.transform.position.y, Is.InRange(0.9f, 1.2f), "Player isn't standing on the ground");
        }
    }
}
