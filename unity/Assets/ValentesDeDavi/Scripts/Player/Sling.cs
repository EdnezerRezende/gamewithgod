using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// A funda de Davi. Segurar o botão gira a funda (a força cresce com o tempo); soltar com a
    /// pedra passando pela faixa dourada do giro produz o tiro perfeito.
    /// </summary>
    public class Sling : MonoBehaviour
    {
        public const float SpinRate = 1.2f;       // voltas por segundo
        const float SweetCenterDeg = 270f;        // ponto do giro em que a pedra está à frente

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

        Transform viewModel, hand, pouch;
        LineRenderer cord, preview;
        int lastRev;
        float whip;

        public float Phase { get { return (chargeTime * SpinRate) % 1f; } }
        public float Power { get { return Mathf.Min(1f, 0.15f + chargeTime / 1.15f); } }
        public float Speed { get { return 16f + 26f * Power; } }

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
            preview = NewLine("Trajetória", U.Hex(0xf3d27a), 0.03f, 40);
            preview.enabled = false;
        }

        static LineRenderer NewLine(string name, Color c, float width, int points)
        {
            LineRenderer l = new GameObject(name).AddComponent<LineRenderer>();
            l.positionCount = points;
            l.widthMultiplier = width;
            l.sharedMaterial = Mats.Get(c);
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return l;
        }

        public void SetVisible(bool v)
        {
            if (viewModel != null) viewModel.gameObject.SetActive(v);
            if (!v) { charging = false; if (preview != null) preview.enabled = false; }
        }

        public void Cancel()
        {
            charging = false;
            player.extraSwayDegrees = 0f;
            if (preview != null) preview.enabled = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (player == null || !player.controlling || dt <= 0f) { AnimatePouch(); return; }

            if (GameInput.FireDown())
            {
                if (canThrow != null && canThrow()) { charging = true; chargeTime = 0f; lastRev = -1; inZone = false; }
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
                UpdatePreview();
                if (!GameInput.FireHeld()) Release();
            }
            else whip = Mathf.Max(0f, whip - dt);
            AnimatePouch();
        }

        void UpdatePreview()
        {
            if (!Difficulty.Current.trajectoryPreview) { preview.enabled = false; return; }
            Vector3 dir = player.cam.transform.forward, p = LaunchPoint(dir), v = dir * Speed;
            int last = 0;
            for (int i = 0; i < 40; i++)
            {
                preview.SetPosition(i, p);
                last = i;
                if (p.y < world.Height(p.x, p.z)) break;
                v.y -= Stone.Gravity * 0.06f;
                p += v * 0.06f;
            }
            Vector3 end = preview.GetPosition(last);
            for (int i = last + 1; i < 40; i++) preview.SetPosition(i, end);
            preview.enabled = true;
        }

        Vector3 LaunchPoint(Vector3 dir)
        {
            Transform c = player.cam.transform;
            return c.position + c.right * 0.22f + c.up * 0.05f + dir * 0.4f;
        }

        void Release()
        {
            charging = false;
            preview.enabled = false;
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
            float th = UnityEngine.Random.Range(0f, Mathf.PI * 2f), e = err * Mathf.Sqrt(UnityEngine.Random.value);
            Vector3 dir = Quaternion.AngleAxis(Mathf.Cos(th) * e, c.up) * Quaternion.AngleAxis(Mathf.Sin(th) * e, c.right) * c.forward;
            Stone s = Stone.Launch(world, LaunchPoint(dir), dir.normalized * Speed, smooth);
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
                // No espaço da câmera, +Z é para a frente: na fase 0,75 (centro da faixa dourada) a pedra está à frente.
                float a = (Phase - 0.75f) * Mathf.PI * 2f;
                pp = hp + new Vector3(Mathf.Sin(a) * 0.3f, 0.22f, Mathf.Cos(a) * 0.3f - 0.05f);
            }
            else pp = hp + new Vector3(whip * 0.6f, -0.26f + whip * 0.8f, whip * 0.8f);
            pouch.localPosition = pp;
            cord.SetPosition(0, hp);
            cord.SetPosition(1, pp);
            cord.SetPosition(2, hp + new Vector3(-0.02f, 0.01f, -0.02f));
        }
    }
}
