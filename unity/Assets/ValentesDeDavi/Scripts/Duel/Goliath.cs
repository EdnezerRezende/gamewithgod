using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Golias e seu escudeiro. Ele avança, ruge, arremessa o dardo e golpeia com a lança.
    /// Depois de cada ação ergue a cabeça e deixa a testa exposta por alguns segundos (a "abertura").
    /// </summary>
    public class Goliath : MonoBehaviour
    {
        public enum State { Idle, Walk, Roar, Throw, Attack, Opening, Fallen }

        public Rig rig, bearer;
        public State state = State.Idle;
        public Duel duel;
        public PlayerController player;
        public World world;
        public UI ui;

        public HitZone forehead;
        float stateT, nextAction, openT, stun, headTilt = 20f, fallT;
        bool firstRoar = true, acted;

        public Vector3 HeadCenter { get { return rig.skull.position; } }
        public bool IsOpen { get { return state == State.Opening; } }

        public static Goliath Build(Transform parent)
        {
            GameObject g = new GameObject("Golias e escudeiro");
            g.transform.SetParent(parent, false);
            Goliath go = g.AddComponent<Goliath>();
            go.rig = Models.Goliath(g.transform);
            go.bearer = Models.ShieldBearer(g.transform);
            return go;
        }

        /// <summary>Posiciona os dois (Golias atrás, escudeiro à frente) olhando para o ponto indicado.</summary>
        public void Place(Vector3 pos, Vector3 lookAt)
        {
            pos.y = World.ValleyHeight(pos.x, pos.z);
            rig.root.position = pos;
            float yaw = U.YawTo(lookAt.x - pos.x, lookAt.z - pos.z);
            rig.root.rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 b = pos + rig.root.forward * 1.9f;
            b.y = World.ValleyHeight(b.x, b.z);
            bearer.root.position = b;
            bearer.root.rotation = rig.root.rotation;
            fallT = 0f;
            headTilt = 20f;
            rig.head.localRotation = Quaternion.Euler(headTilt, 0f, 0f);
            foreach (Transform a in rig.arms) a.localRotation = Quaternion.identity;
            rig.foreheadMat.SetColor("_EmissionColor", Color.black);
            rig.foreRing.enabled = false;
        }

        public void BeginFight()
        {
            state = State.Walk;
            stateT = 0f;
            nextAction = 2.5f;
            firstRoar = true;
            stun = 0f;
        }

        public void Stun(float t) { stun = Mathf.Max(stun, t); }

        public void Fall() { state = State.Fallen; fallT = 0f; if (rig.skin != null) rig.skin.Fall(); }

        /// <summary>Andar de um lado para outro no vale (menu e abertura da fase).</summary>
        public void Pace(float t)
        {
            Vector3 p = new Vector3(Mathf.Sin(t * 0.25f) * 7f, 0f, 6f);
            Place(p, p + Vector3.back);
            AnimateLegs(t, 18f);
        }

        void AnimateLegs(float t, float amp)
        {
            if (rig.skin != null) rig.skin.SetSpeed(2f);
            if (bearer.skin != null) bearer.skin.SetSpeed(2f);
            for (int i = 0; i < 2; i++) rig.legs[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 3f + i * Mathf.PI) * amp, 0f, 0f);
            for (int i = 0; i < 2; i++) bearer.legs[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 5f + i * Mathf.PI) * amp, 0f, 0f);
        }

        public void RoarPose(float k)
        {
            rig.arms[0].localRotation = Quaternion.Euler(0f, 0f, -35f * k);
            rig.arms[1].localRotation = Quaternion.Euler(0f, 0f, 35f * k);
            rig.head.localRotation = Quaternion.Euler(Mathf.Lerp(20f, -15f, k), 0f, 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (state == State.Fallen) { UpdateFall(dt); return; }
            if (state == State.Idle) return;

            Transform g = rig.root;
            Vector3 to = U.Flat(player.Position - g.position);
            float dist = to.magnitude;
            Difficulty d = Difficulty.Current;
            float tilt = 20f;

            g.rotation = Quaternion.Euler(0f, U.YawTo(to.x, to.z), 0f);
            if (stun > 0f) stun -= dt;
            else
            {
                stateT += dt;
                switch (state)
                {
                    case State.Walk:
                        if (dist > 3.4f)
                        {
                            g.position += to / dist * 0.9f * d.goliathSpeed * dt;
                            AnimateLegs(Time.time, 20f);
                        }
                        else { if (rig.skin != null) rig.skin.SetSpeed(0f); if (bearer.skin != null) bearer.skin.SetSpeed(0f); }
                        nextAction -= dt;
                        rig.arms[1].localRotation = Quaternion.identity;
                        if (dist < 4.4f) Enter(State.Attack);
                        else if (nextAction <= 0f) { Enter(dist > 12f && !firstRoar ? State.Throw : State.Roar); firstRoar = false; }
                        break;

                    case State.Roar:
                        if (!acted)
                        {
                            acted = true;
                            Sfx.Play("roar");
                            player.shake = 1.2f;
                            player.courage = Mathf.Clamp(player.courage - d.roarCourageLoss, 0f, 100f);
                            ui.Toast("Golias ruge. A coragem cai (−" + d.roarCourageLoss + ").");
                        }
                        tilt = -12f;
                        RoarPose(1f);
                        if (stateT > 1.3f) { RoarPose(0f); OpenUp(1f); }
                        break;

                    case State.Throw:
                        rig.arms[1].localRotation = Quaternion.Euler(stateT < 0.75f ? stateT * 150f : Mathf.Lerp(112f, -35f, Mathf.Min(1f, (stateT - 0.75f) * 5f)), 0f, 0f);
                        if (stateT > 0.8f && !acted) { acted = true; Javelin.Throw(this); }
                        if (stateT > 1.15f) { rig.arms[1].localRotation = Quaternion.identity; OpenUp(1f); }
                        break;

                    case State.Attack:
                        rig.arms[1].localRotation = Quaternion.Euler(stateT < 0.9f ? stateT * 70f : Mathf.Lerp(63f, -80f, Mathf.Min(1f, (stateT - 0.9f) * 6f)), 0f, 0f);
                        if (stateT > 1.0f && !acted)
                        {
                            acted = true;
                            if (dist < 5.2f)
                            {
                                player.Damage(d.spearDamage, ui, "A lança de Golias acertou Davi.");
                                Vector3 p = player.Position + to / Mathf.Max(dist, 0.1f) * 4f;
                                player.transform.position = new Vector3(p.x, world.Height(p.x, p.z), p.z);
                                player.courage = Mathf.Clamp(player.courage - 4f, 0f, 100f);
                                duel.CheckPlayerDown();
                            }
                            else ui.Toast("Você desviou da lança.", 1.2f);
                        }
                        if (stateT > 1.5f) { rig.arms[1].localRotation = Quaternion.identity; OpenUp(0.7f); }
                        break;

                    case State.Opening:
                        tilt = -24f;
                        openT -= dt;
                        if (openT <= 0f)
                        {
                            state = State.Walk;
                            nextAction = (d.level == DifficultyLevel.Valente ? 2.4f : 3.2f) + Random.Range(0f, 2f);
                        }
                        break;
                }
            }

            g.position = new Vector3(g.position.x, World.ValleyHeight(g.position.x, g.position.z), g.position.z);
            headTilt = Mathf.Lerp(headTilt, tilt, Mathf.Clamp01(dt * 6f));
            if (state != State.Roar) rig.head.localRotation = Quaternion.Euler(headTilt, 0f, 0f);
            float glow = state == State.Opening ? (d.level == DifficultyLevel.Pastor ? 1.4f : d.level == DifficultyLevel.Guerreiro ? 1f : 0.3f) : 0f;
            Mats.SetEmission(rig.foreheadMat, U.Hex(0xffc15a) * glow);
            // Anel de mira em volta da testa: só na abertura e onde a faixa dourada aparece.
            bool ring = state == State.Opening && d.sweetArcVisible;
            rig.foreRing.enabled = ring;
            if (ring) rig.foreRing.widthMultiplier = 0.04f + 0.02f * Mathf.Sin(Time.time * 8f);

            // O escudeiro fica entre Golias e Davi; durante a abertura ele dá um passo para o lado.
            Vector3 n = to / Mathf.Max(dist, 0.01f), side = new Vector3(n.z, 0f, -n.x) * (state == State.Opening ? 1.9f : 0f);
            Vector3 target = g.position + n * 1.9f + side;
            Vector3 bp = Vector3.Lerp(bearer.root.position, target, Mathf.Clamp01(dt * 3f));
            bp.y = World.ValleyHeight(bp.x, bp.z);
            bearer.root.position = bp;
            Vector3 tb = U.Flat(player.Position - bp);
            bearer.root.rotation = Quaternion.Euler(0f, U.YawTo(tb.x, tb.z), 0f);
        }

        void Enter(State s)
        {
            state = s; stateT = 0f; acted = false;
            if (rig.skin != null && (s == State.Attack || s == State.Throw || s == State.Roar)) rig.skin.Attack();
        }

        void OpenUp(float k)
        {
            state = State.Opening;
            openT = Difficulty.Current.openingSeconds * k;
        }

        void UpdateFall(float dt)
        {
            fallT += dt;
            float a = Mathf.Min(90f, Mathf.Pow(fallT / 1.6f, 2f) * 90f);
            Vector3 e = rig.root.eulerAngles;
            if (rig.skin == null) rig.root.rotation = Quaternion.Euler(a, e.y, 0f);
            Mats.SetEmission(rig.foreheadMat, Color.black);
            rig.foreRing.enabled = false;
            // O escudeiro foge.
            Vector3 away = U.Flat(bearer.root.position - player.Position).normalized;
            Vector3 bp = bearer.root.position + away * 3f * dt;
            bp.y = World.ValleyHeight(bp.x, bp.z);
            bearer.root.position = bp;
            bearer.root.rotation = Quaternion.Euler(0f, U.YawTo(away.x, away.z), 0f);
            AnimateLegs(Time.time * 2f, 30f);
        }
    }
}
