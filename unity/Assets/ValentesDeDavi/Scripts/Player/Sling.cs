using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// A funda de Davi. Segurar o botão gira a funda (a força cresce com o tempo); soltar com a
    /// pedra passando pela faixa dourada do giro produz o tiro perfeito.
    /// A pedra é lançada no ângulo que a leva exatamente ao ponto sob a mira (se a força alcançar).
    /// </summary>
    public class Sling : MonoBehaviour
    {
        public const float SpinRate = 1.2f;       // voltas por segundo
        const float SweetCenterDeg = 270f;        // ponto do giro em que a pedra está à frente
        const int PreviewPoints = 80;

        public PlayerController player;
        public World world;
        public UI ui;

        /// <summary>O modo atual (treino ou duelo) decide se há pedra disponível e qual é a lisura dela.</summary>
        public Func<bool> canThrow;
        public Func<float> takeStone;
        public Action<Stone> onThrow;

        public bool charging;
        public float chargeTime;
        public bool inZone;
        /// <summary>false quando o ponto da mira está longe demais para a força atual.</summary>
        public bool reach = true;
        /// <summary>A mira está sobre um alvo que conta (jarro, leão, testa de Golias na abertura).</summary>
        public bool aimOnTarget;

        Transform viewModel, hand, pouch, marker;
        Material markerMat;
        LineRenderer cord, preview;
        int lastRev;
        float whip;

        public float Phase { get { return (chargeTime * SpinRate) % 1f; } }
        public float Power { get { return Mathf.Min(1f, 0.15f + chargeTime / 1.15f); } }
        public float Speed { get { return 16f + 26f * Power; } }

        bool ShowAimHelp { get { return Settings.AimHelp || Difficulty.Current.trajectoryPreview; } }

        public float SweetArcDegrees()
        {
            return Difficulty.Current.sweetArcDegrees * (0.7f + 0.6f * player.courage / 100f);
        }

        float ZoneDistanceDeg()
        {
            float pd = Phase * 360f;
            return Mathf.Abs(((pd - SweetCenterDeg + 540f) % 360f) - 180f);
        }

        public void Build()
        {
            viewModel = new GameObject("Mãos de Davi").transform;
            viewModel.SetParent(player.cam.transform, false);
            hand = U.Box(viewModel, new Vector3(0.26f, -0.3f, 0.55f), new Vector3(0.08f, 0.1f, 0.14f), U.Hex(0x9a6b48)).transform;
            GameObject staff = U.Cyl(viewModel, new Vector3(-0.34f, -0.55f, 0.7f), 0.02f, 1.4f, U.Hex(0x6b4a2a));
            staff.transform.localRotation = Quaternion.Euler(-14f, 0f, 9f);
            pouch = U.Sph(viewModel, Vector3.zero, 0.045f, U.Hex(0x5a3a22)).transform;
            foreach (Renderer r in viewModel.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            cord = NewLine("Corda", U.Hex(0x3a2616), 0.008f, 3);
            cord.transform.SetParent(viewModel, false);
            cord.useWorldSpace = false;
            preview = NewLine("Trajetória", U.Hex(0xf3d27a), 0.03f, PreviewPoints);
            preview.enabled = false;

            // Marcador de impacto: um disco com um ponto no meio, sempre virado para a câmera.
            marker = new GameObject("Marcador de impacto").transform;
            markerMat = Mats.New(Color.white);
            GameObject disc = U.Cyl(marker, Vector3.zero, 0.22f, 0.01f, Color.white);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject dot = U.Sph(marker, new Vector3(0f, 0f, -0.02f), 0.05f, Color.white);
            foreach (GameObject g in new[] { disc, dot })
            {
                Renderer r = g.GetComponent<Renderer>();
                r.sharedMaterial = markerMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            marker.gameObject.SetActive(false);
        }

        static LineRenderer NewLine(string name, Color c, float width, int points)
        {
            LineRenderer l = new GameObject(name).AddComponent<LineRenderer>();
            l.positionCount = points;
            l.widthMultiplier = width;
            l.sharedMaterial = Mats.New(c);
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return l;
        }

        public void SetVisible(bool v)
        {
            if (viewModel != null) viewModel.gameObject.SetActive(v);
            if (!v) { charging = false; HideAim(); aimOnTarget = false; }
        }

        public void Cancel()
        {
            charging = false;
            player.extraSwayDegrees = 0f;
            HideAim();
        }

        void HideAim()
        {
            if (preview != null) preview.enabled = false;
            if (marker != null) marker.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (player == null || !player.controlling || dt <= 0f) { AnimatePouch(); return; }

            if (Settings.AimHelp)
            {
                bool good;
                AimPoint(out good);
                aimOnTarget = good;
            }
            else aimOnTarget = false;

            if (GameInput.FireDown())
            {
                if (canThrow != null && canThrow()) { charging = true; chargeTime = 0f; lastRev = -1; inZone = false; reach = true; }
                else ui.Toast("Sem pedras no alforje.");
            }
            if (charging)
            {
                chargeTime += dt;
                int rev = Mathf.FloorToInt(chargeTime * SpinRate + 0.25f);
                if (rev != lastRev) { lastRev = rev; Sfx.Play("whoosh", 0.4f + 0.6f * Power, 0.8f + 0.5f * Power); }
                bool z = ZoneDistanceDeg() <= SweetArcDegrees() / 2f;
                if (z && !inZone && !Difficulty.Current.sweetArcVisible) Sfx.Play("tick", 0.6f);
                inZone = z;
                player.extraSwayDegrees = chargeTime > 3f ? (chargeTime - 3f) * 1.2f : 0f;
                if (ShowAimHelp) UpdatePreview(); else HideAim();
                if (!GameInput.FireHeld()) Release();
            }
            else whip = Mathf.Max(0f, whip - dt);
            AnimatePouch();
        }

        static bool Counts(HitZone hz) { return hz != null && hz.counts != null && hz.counts(); }

        /// <summary>Ponto sob a mira: o primeiro alvo ou o chão atravessado pelo raio do centro da tela.</summary>
        public Vector3 AimPoint(out bool good)
        {
            Transform c = player.cam.transform;
            Vector3 o = c.position, d = c.forward;
            const float MaxDist = 150f;
            Physics.SyncTransforms();
            RaycastHit rh;
            float hitDist = MaxDist;
            HitZone hz = null;
            if (Physics.Raycast(o, d, out rh, MaxDist, ~0, QueryTriggerInteraction.Collide))
            {
                hz = rh.collider.GetComponent<HitZone>();
                if (hz != null) hitDist = rh.distance;
            }
            // Chão: avança em passos e refina por bisseção.
            float prev = 0.3f;
            for (float s = 0.3f; s < hitDist; s += 0.5f)
            {
                Vector3 p = o + d * s;
                if (p.y < world.Height(p.x, p.z))
                {
                    float a = prev, b = s;
                    for (int i = 0; i < 12; i++)
                    {
                        float m = (a + b) * 0.5f;
                        Vector3 q = o + d * m;
                        if (q.y < world.Height(q.x, q.z)) b = m; else a = m;
                    }
                    good = false;
                    Vector3 g = o + d * b;
                    g.y = world.Height(g.x, g.z);
                    return g;
                }
                prev = s;
            }
            good = hz != null && hitDist < MaxDist && Counts(hz);
            return o + d * hitDist;
        }

        /// <summary>Direção de lançamento (arco baixo) que leva a pedra, com a velocidade dada, até o alvo.</summary>
        public static Vector3 SolveDirection(Vector3 start, Vector3 target, float speed, Vector3 fallback, out bool reachable)
        {
            Vector3 flat = new Vector3(target.x - start.x, 0f, target.z - start.z);
            float h = flat.magnitude, dy = target.y - start.y, v2 = speed * speed, g = Stone.Gravity;
            reachable = true;
            if (h < 0.01f) return fallback;
            float disc = v2 * v2 - g * (g * h * h + 2f * dy * v2);
            float th;
            if (disc >= 0f) th = Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (g * h));
            else { th = Mathf.PI / 4f; reachable = false; }
            Vector3 f = flat / h;
            return new Vector3(f.x * Mathf.Cos(th), Mathf.Sin(th), f.z * Mathf.Cos(th));
        }

        void UpdatePreview()
        {
            bool aimGood;
            Vector3 aim = AimPoint(out aimGood);
            Vector3 p = LaunchPoint(player.cam.transform.forward);
            Vector3 dir = SolveDirection(p, aim, Speed, player.cam.transform.forward, out reach);
            Vector3 v = dir * Speed;

            const float h = 0.01f;
            int n = 0;
            bool hit = false, good = false;
            preview.SetPosition(n++, p);
            Physics.SyncTransforms();
            for (int i = 0; i < PreviewPoints - 1 && !hit; i++)
            {
                for (int k = 0; k < 3 && !hit; k++)
                {
                    v.y -= Stone.Gravity * h;
                    Vector3 step = v * h, next = p + step;
                    RaycastHit rh;
                    float len = step.magnitude;
                    if (len > 0f && Physics.SphereCast(p, 0.05f, step / len, out rh, len, ~0, QueryTriggerInteraction.Collide)
                        && rh.collider.GetComponent<HitZone>() != null)
                    {
                        next = rh.point;
                        hit = true;
                        good = Counts(rh.collider.GetComponent<HitZone>());
                    }
                    else if (next.y < world.Height(next.x, next.z))
                    {
                        next.y = world.Height(next.x, next.z);
                        hit = true;
                    }
                    p = next;
                }
                preview.SetPosition(n++, p);
            }
            for (int i = n; i < PreviewPoints; i++) preview.SetPosition(i, p);

            Color c = !reach ? U.Hex(0xc0533a) : good ? U.Hex(0xffd166) : U.Hex(0xf3d27a);
            Mats.SetColor(preview.sharedMaterial, c);
            preview.enabled = true;
            if (hit)
            {
                marker.gameObject.SetActive(true);
                marker.position = p;
                Transform cam = player.cam.transform;
                marker.rotation = Quaternion.LookRotation(marker.position - cam.position);
                marker.localScale = Vector3.one * Mathf.Max(1f, Vector3.Distance(p, cam.position) / 12f);
                Color mc = !reach ? U.Hex(0xc0533a) : good ? U.Hex(0xffd166) : U.Hex(0xeadcbd);
                Mats.SetColor(markerMat, mc);
                Mats.SetEmission(markerMat, mc * 0.8f);
            }
            else marker.gameObject.SetActive(false);
        }

        Vector3 LaunchPoint(Vector3 dir)
        {
            Transform c = player.cam.transform;
            return c.position + c.right * 0.22f + c.up * 0.05f + dir * 0.4f;
        }

        void Release()
        {
            charging = false;
            HideAim();
            player.extraSwayDegrees = 0f;
            if (canThrow == null || !canThrow()) return;

            float dz = ZoneDistanceDeg(), half = SweetArcDegrees() / 2f;
            bool perfect = dz <= half;
            float err = 0.12f;
            if (!perfect) err += Mathf.Min(4f, (dz - half) / 180f * 6f);
            if (chargeTime > 3f) err += (chargeTime - 3f) * 2f;
            float smooth = takeStone != null ? takeStone() : 0.85f;
            err += (1f - smooth) * 2.4f;

            Transform c = player.cam.transform;
            Vector3 start = LaunchPoint(c.forward);
            bool good, ok;
            Vector3 aim = AimPoint(out good);
            Vector3 dir = SolveDirection(start, aim, Speed, c.forward, out ok);
            float th = UnityEngine.Random.Range(0f, Mathf.PI * 2f), e = err * Mathf.Sqrt(UnityEngine.Random.value);
            dir = Quaternion.AngleAxis(Mathf.Cos(th) * e, c.up) * Quaternion.AngleAxis(Mathf.Sin(th) * e, c.right) * dir;
            Stone s = Stone.Launch(world, start, dir.normalized * Speed, smooth);
            Sfx.Play("throw");
            whip = 0.25f;
            if (perfect)
            {
                Sfx.Play("perfect", 0.7f);
                if (Difficulty.Current.sweetArcVisible) ui.Toast("Tiro perfeito", 0.9f);
            }
            if (onThrow != null) onThrow(s);
        }

        void AnimatePouch()
        {
            if (hand == null) return;
            Vector3 hp = hand.localPosition, pp;
            if (charging)
            {
                // A bolsa gira acima da mão, à direita, sem cobrir a mira.
                float a = (Phase - 0.75f) * Mathf.PI * 2f;
                pp = hp + new Vector3(0.1f + Mathf.Sin(a) * 0.15f, 0.16f, Mathf.Cos(a) * 0.15f - 0.05f);
            }
            else pp = hp + new Vector3(whip * 0.6f, -0.26f + whip * 0.8f, whip * 0.8f);
            pouch.localPosition = pp;
            cord.SetPosition(0, hp);
            cord.SetPosition(1, pp);
            cord.SetPosition(2, hp + new Vector3(-0.02f, 0.01f, -0.02f));
        }
    }
}
