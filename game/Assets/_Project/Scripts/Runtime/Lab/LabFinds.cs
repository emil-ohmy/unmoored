using UnityEngine;

namespace MyGame
{
    // (The lab) Every kind of thing the Apprentice can find, set out in a row to look at and take. Most are placed by
    // the scene builder; the Hook is made here (it's freed from the Anchoress in the game, not placed). Resetting the
    // Apprentice (the Tester's Post) puts them all back.
    public sealed class LabFinds : MonoBehaviour
    {
        public GameObject hookModel;
        public Transform hookSpot;

        FerrymansHook hook;

        void Start() => SetDownTheHook();

        void SetDownTheHook()
        {
            if (hookSpot != null)
                hook = FerrymansHook.AtRest(hookModel, hookSpot.position);
        }

        public void PutBack()
        {
            foreach (var p in FindObjectsByType<UpgradePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.PutBack();
            foreach (var p in FindObjectsByType<ProvisionFind>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.PutBack();
            foreach (var p in FindObjectsByType<ChargePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.PutBack();
            foreach (var p in FindObjectsByType<ObolCache>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.PutBack();
            foreach (var p in FindObjectsByType<Keepsake>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.PutBack();
            if (hook == null || hook.Taken)
            {
                if (hook != null)
                    Destroy(hook.gameObject);
                SetDownTheHook();
            }
        }
    }
}
