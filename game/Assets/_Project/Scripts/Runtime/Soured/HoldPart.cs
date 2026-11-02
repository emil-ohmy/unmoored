using UnityEngine;

namespace MyGame
{
    // A part of the Hold's Boss (a hand, or the face): no body of its own (the Boss's model is one rig in the wall), only a
    // place on the floor that follows its bone (TheHoldWall moves it), struck there when it's in reach (Open: a hand lying
    // stunned after a slam, the face sagged down) and passing every blow on to the whole (TheHoldWall listens to Struck).
    // It never dies, never notices anyone, never moves by itself.
    public sealed class HoldPart : Soured
    {
        public bool Open { get; set; }
        public override bool Targetable => base.Targetable && Open;
        public override bool OutOfReach => !Open;
        protected override bool Floats => true;

        public event System.Action Reformed; // (a Rest: its Boss asleep in the wall again)

        public override void Reform()
        {
            base.Reform();
            Open = false;
            Reformed?.Invoke();
        }

        public void Follow(Vector3 onFloor)
        {
            if ((transform.position - onFloor).sqrMagnitude > 0.0004f)
                PlaceAt(onFloor);
        }
    }
}
