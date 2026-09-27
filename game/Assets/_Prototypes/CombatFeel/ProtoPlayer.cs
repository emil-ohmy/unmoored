using UnityEngine;
using UnityEngine.InputSystem;

// PROTOTYPE (wayfinder ticket "Combat feel: core verbs and pace"). Throwaway code, not production.
namespace MyGame.Prototypes.CombatFeel
{
    [RequireComponent(typeof(CharacterController))]
    public class ProtoPlayer : MonoBehaviour
    {
        public enum State { Free, Rolling, Attacking, Staggered, Dead }
        public enum Act { None, Roll, Light, Heavy }

        public Transform oarPivot;
        public Renderer body;
        public float maxHealth = 100f;
        public float staggerDuration = 0.35f;
        public float postHitInvulnerability = 0.5f;

        public State CurrentState { get; private set; }
        public string Phase { get; private set; } = "";
        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; } = -10f;
        public bool Invulnerable =>
            CurrentState == State.Dead
            || Time.time < invulnerableUntil
            || (CurrentState == State.Rolling && stateTime >= P.iFrameStart && stateTime <= P.iFrameEnd);

        PacePreset P => CombatTuning.Instance.P;

        CharacterController controller;
        InputAction move, roll, lightAttack, heavyAttack, prevPreset, nextPreset, preset1, preset2, preset3, reset;

        Act buffered;
        float bufferedAt;
        float stateTime;
        float lastStaminaUse = -10f;
        float invulnerableUntil;
        Vector3 rollDirection;
        bool isHeavy;
        int comboStep;
        bool hitLanded;
        Vector3 knockback;
        float verticalVelocity;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        ProtoSoured enemy;
        Material material;
        Color baseColor;
        float flashUntil;
        Color flashColor;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            roll = Button("Roll", "<Keyboard>/space", "<Gamepad>/buttonEast");
            lightAttack = Button("Light", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest", "<Gamepad>/rightShoulder");
            heavyAttack = Button("Heavy", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/buttonNorth", "<Gamepad>/rightTrigger");
            prevPreset = Button("PrevPreset", "<Gamepad>/dpad/left");
            nextPreset = Button("NextPreset", "<Gamepad>/dpad/right");
            preset1 = Button("Preset1", "<Keyboard>/1", "<Keyboard>/numpad1");
            preset2 = Button("Preset2", "<Keyboard>/2", "<Keyboard>/numpad2");
            preset3 = Button("Preset3", "<Keyboard>/3", "<Keyboard>/numpad3");
            reset = Button("Reset", "<Keyboard>/r", "<Gamepad>/select");
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var binding in bindings)
                action.AddBinding(binding);
            return action;
        }

        InputAction[] AllActions => new[] { move, roll, lightAttack, heavyAttack, prevPreset, nextPreset, preset1, preset2, preset3, reset };

        void OnEnable() { foreach (var a in AllActions) a.Enable(); }
        void OnDisable() { foreach (var a in AllActions) a.Disable(); }
        void OnDestroy() { foreach (var a in AllActions) a.Dispose(); }

        void Start()
        {
            enemy = FindAnyObjectByType<ProtoSoured>();
            material = body.material;
            baseColor = material.GetColor("_BaseColor");
            Health = maxHealth;
            Stamina = P.maxStamina;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            HandleMetaInput();

            if (roll.WasPressedThisFrame()) Buffer(Act.Roll);
            if (lightAttack.WasPressedThisFrame()) Buffer(Act.Light);
            if (heavyAttack.WasPressedThisFrame()) Buffer(Act.Heavy);

            stateTime += dt;
            Vector3 planar = Vector3.zero;

            switch (CurrentState)
            {
                case State.Free:
                    Phase = "";
                    Vector3 input = InputDirection();
                    planar = input * P.moveSpeed;
                    if (input.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(input), 1440f * dt);
                    SetOarAngle(30f);
                    TryConsumeBuffer();
                    break;

                case State.Rolling:
                    Phase = Invulnerable ? "i-frames" : (stateTime < P.rollDuration ? "rolling" : "roll recovery");
                    if (stateTime < P.rollDuration)
                        planar = rollDirection * (P.rollDistance / P.rollDuration);
                    if (stateTime >= P.rollDuration + P.rollRecovery)
                    {
                        EnterState(State.Free);
                        TryConsumeBuffer();
                    }
                    break;

                case State.Attacking:
                    planar = UpdateAttack();
                    break;

                case State.Staggered:
                    Phase = "staggered";
                    if (stateTime >= staggerDuration)
                        EnterState(State.Free);
                    break;

                case State.Dead:
                    Phase = "dead";
                    if (stateTime >= 1.5f)
                        ResetArena();
                    break;
            }

            RegenerateStamina(dt);

            knockback *= Mathf.Exp(-10f * dt);
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * dt;
            controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);

            material.SetColor("_BaseColor", Time.time < flashUntil ? flashColor : baseColor);
        }

        Vector3 UpdateAttack()
        {
            float windup = isHeavy ? P.heavyWindup : P.lightWindup;
            float active = isHeavy ? P.heavyActive : P.lightActive;
            float recovery = isHeavy ? P.heavyRecovery : P.lightRecovery;
            float lunge = isHeavy ? P.heavyLunge : P.lightLunge;
            float side = comboStep % 2 == 0 ? 1f : -1f;
            float startAngle = side * (isHeavy ? -120f : -75f);
            float endAngle = side * (isHeavy ? 110f : 75f);
            string label = isHeavy ? "Heavy" : $"Light {comboStep + 1}/{P.lightComboLength}";
            Vector3 planar = Vector3.zero;

            if (stateTime < windup)
            {
                Phase = $"{label}: windup";
                SetOarAngle(Mathf.Lerp(30f, startAngle, stateTime / windup));
            }
            else if (stateTime < windup + active)
            {
                Phase = $"{label}: ACTIVE";
                float t = (stateTime - windup) / active;
                SetOarAngle(Mathf.Lerp(startAngle, endAngle, t));
                planar = transform.forward * (lunge / active);
                CheckHit();
            }
            else if (stateTime < windup + active + recovery)
            {
                Phase = $"{label}: recovery";
                SetOarAngle(Mathf.Lerp(endAngle, 30f, (stateTime - windup - active) / recovery));

                bool comboAvailable = !isHeavy && comboStep < P.lightComboLength - 1;
                if (HasBuffered(Act.Light) && comboAvailable)
                    StartAttack(false, comboStep + 1);
                else if (HasBuffered(Act.Roll) && P.rollCancelsRecovery)
                    TryConsumeBuffer();
            }
            else
            {
                EnterState(State.Free);
                TryConsumeBuffer();
            }
            return planar;
        }

        void CheckHit()
        {
            if (hitLanded || enemy == null || !enemy.IsAlive)
                return;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float reach = isHeavy ? P.heavyReach : P.lightReach;
            float arc = isHeavy ? 55f : 70f;
            if (toEnemy.magnitude > reach + enemy.radius || Vector3.Angle(transform.forward, toEnemy) > arc)
                return;

            hitLanded = true;
            float knock = isHeavy ? P.knockbackHeavy : P.knockbackLight;
            enemy.TakeHit(isHeavy ? P.heavyDamage : P.lightDamage, toEnemy.normalized * knock, isHeavy);
            CombatTuning.Instance.HitStop(isHeavy ? P.hitStopHeavy : P.hitStopLight);
            FixedAngleCamera.Instance?.Shake(isHeavy ? 0.25f : 0.1f);
            Announce(isHeavy ? "HEAVY HIT" : "hit");
        }

        public bool TakeHit(float damage, Vector3 knock)
        {
            if (Invulnerable)
            {
                if (CurrentState == State.Rolling)
                    Announce("DODGED");
                return false;
            }

            Health = Mathf.Max(0f, Health - damage);
            knockback = knock;
            invulnerableUntil = Time.time + postHitInvulnerability;
            buffered = Act.None;
            Flash(new Color(1f, 0.2f, 0.2f), 0.15f);
            CombatTuning.Instance.HitStop(0.08f);
            FixedAngleCamera.Instance?.Shake(0.3f);
            Announce($"took {damage:0} damage");
            EnterState(Health <= 0f ? State.Dead : State.Staggered);
            if (Health <= 0f)
                Announce("YOU DIED");
            return true;
        }

        // Also used by the smoke test in the project copy; the prototype itself only calls it from input.
        public void Buffer(Act act)
        {
            buffered = act;
            bufferedAt = Time.time;
        }

        bool HasBuffered(Act act) => buffered == act && Time.time - bufferedAt <= P.inputBuffer;

        void TryConsumeBuffer()
        {
            if (buffered == Act.None || Time.time - bufferedAt > P.inputBuffer)
                return;

            Act act = buffered;
            switch (act)
            {
                case Act.Roll:
                    if (!CanSpend(P.rollStaminaCost)) { Announce("no stamina"); break; }
                    Spend(P.rollStaminaCost);
                    Vector3 input = InputDirection();
                    rollDirection = input.sqrMagnitude > 0.001f ? input.normalized : transform.forward;
                    transform.rotation = Quaternion.LookRotation(rollDirection);
                    EnterState(State.Rolling);
                    break;
                case Act.Light:
                    StartAttack(false, 0);
                    break;
                case Act.Heavy:
                    StartAttack(true, 0);
                    break;
            }
            buffered = Act.None;
        }

        void StartAttack(bool heavy, int step)
        {
            float cost = heavy ? P.heavyStaminaCost : P.lightStaminaCost;
            buffered = Act.None;
            if (!CanSpend(cost))
            {
                Announce("no stamina");
                return;
            }

            Spend(cost);
            Vector3 input = InputDirection();
            if (input.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(input); // attacks go where you push the stick
            isHeavy = heavy;
            comboStep = step;
            hitLanded = false;
            EnterState(State.Attacking);
        }

        // Souls rule: any stamina left lets you act; the cost can take you down to zero.
        bool CanSpend(float cost) => cost <= 0f || Stamina > 0f;

        void Spend(float cost)
        {
            Stamina = Mathf.Max(0f, Stamina - cost);
            lastStaminaUse = Time.time;
        }

        void RegenerateStamina(float dt)
        {
            bool busy = CurrentState == State.Rolling || CurrentState == State.Attacking;
            if (!busy && Time.time - lastStaminaUse >= P.staminaRegenDelay)
                Stamina = Mathf.Min(P.maxStamina, Stamina + P.staminaRegenPerSec * dt);
            Stamina = Mathf.Min(Stamina, P.maxStamina);
        }

        void EnterState(State state)
        {
            CurrentState = state;
            stateTime = 0f;
        }

        Vector3 InputDirection()
        {
            // The camera never rotates and looks along +Z, so stick directions map straight onto the world.
            Vector2 v = move.ReadValue<Vector2>();
            return Vector3.ClampMagnitude(new Vector3(v.x, 0f, v.y), 1f);
        }

        void SetOarAngle(float yaw)
        {
            if (oarPivot != null)
                oarPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void Flash(Color color, float seconds)
        {
            flashColor = color;
            flashUntil = Time.time + seconds;
        }

        void Announce(string text)
        {
            LastEvent = text;
            LastEventTime = Time.unscaledTime;
        }

        void HandleMetaInput()
        {
            var tuning = CombatTuning.Instance;
            if (preset1.WasPressedThisFrame()) tuning.Select(0);
            if (preset2.WasPressedThisFrame()) tuning.Select(1);
            if (preset3.WasPressedThisFrame()) tuning.Select(2);
            if (prevPreset.WasPressedThisFrame()) tuning.Select(tuning.current - 1);
            if (nextPreset.WasPressedThisFrame()) tuning.Select(tuning.current + 1);
            if (reset.WasPressedThisFrame()) ResetArena();
        }

        void ResetArena()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            controller.enabled = true;
            Health = maxHealth;
            Stamina = P.maxStamina;
            knockback = Vector3.zero;
            buffered = Act.None;
            EnterState(State.Free);
            enemy?.ResetEnemy();
            Announce("reset");
        }
    }
}
