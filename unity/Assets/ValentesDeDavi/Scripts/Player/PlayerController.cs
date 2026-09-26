using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Davi em primeira pessoa: movimento, olhar, balanço da mira (ligado à Coragem), vida e armadura.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public const float EyeHeight = 1.6f;
        const float MouseSensitivity = 0.0022f;
        const float TouchSensitivity = 0.0048f;

        public Camera cam;
        public World world;
        public bool controlling;
        /// <summary>Multiplicador de velocidade (escudo erguido, oração).</summary>
        public float speedMultiplier = 1f;
        /// <summary>Altura dos olhos (baixa quando Samá se ajoelha para orar).</summary>
        public float eyeHeight = EyeHeight;
        /// <summary>Quanto o balanço da mira pesa (a espada treme menos que a funda).</summary>
        public float swayScale = 1f;

        public float yaw, pitch;
        public Vector3 velocity;
        public float health = 100f;
        public float courage = 70f;
        public bool armor;
        public float shake;

        /// <summary>Limita a posição (área do treino ou do vale).</summary>
        public Func<Vector3, Vector3> clampPosition;
        /// <summary>Quanto o tremor extra da funda (girar demais) soma ao balanço, em graus.</summary>
        public float extraSwayDegrees;

        public Vector3 Position { get { return transform.position; } }

        public void Place(Vector3 pos, float yawRad, float pitchRad)
        {
            pos.y = world.Height(pos.x, pos.z);
            transform.position = pos;
            yaw = yawRad;
            pitch = pitchRad;
            velocity = Vector3.zero;
        }

        /// <summary>yaw em radianos; 0 olha para +Z (em direção a Golias no vale).</summary>
        public Vector3 Forward { get { return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)); } }
        public Vector3 Right { get { return new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw)); } }

        void Update()
        {
            float dt = Time.deltaTime;
            shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime * 1.6f);
            if (!controlling || dt <= 0f) return;

            Vector2 md = GameInput.MouseDelta();
            if (Mathf.Abs(md.x) < 180f && Mathf.Abs(md.y) < 180f)
            {
                yaw += md.x * MouseSensitivity;
                pitch = Mathf.Clamp(pitch + md.y * MouseSensitivity, -1.35f, 1.35f);
            }
            // Arrasto na tela (pixels do painel, y para baixo): arrastar para baixo olha para baixo.
            Vector2 td = TouchControls.ConsumeLook();
            yaw += td.x * TouchSensitivity;
            pitch = Mathf.Clamp(pitch - td.y * TouchSensitivity, -1.35f, 1.35f);
            Vector2 al = GameInput.ArrowLook();
            yaw += al.x * 1.8f * dt;
            pitch = Mathf.Clamp(pitch + al.y * 1.2f * dt, -1.35f, 1.35f);

            Vector2 mv = GameInput.Move();
            float mag = Mathf.Min(1f, mv.magnitude);
            if (mag < 0.12f) mag = 0f;   // zona morta do direcional de toque
            Vector3 wish = Forward * mv.y + Right * mv.x;
            float speed = GameInput.Sprint() || mag > 0.92f && TouchControls.Active ? 7.2f : 4.6f;
            if (armor) speed *= 0.58f;
            speed *= speedMultiplier;
            wish = mag > 0f ? wish.normalized * speed * mag : Vector3.zero;
            velocity = Vector3.Lerp(velocity, wish, Mathf.Clamp01(dt * 10f));
            Vector3 p = transform.position + velocity * dt;
            if (clampPosition != null) p = clampPosition(p);
            p.y = world.Height(p.x, p.z);
            transform.position = p;
        }

        public float SwayRadians()
        {
            float a = Difficulty.Current.swayMultiplier * (0.25f + 2.1f * (1f - courage / 100f));
            if (armor) a += 0.8f;
            a += extraSwayDegrees;
            return a * swayScale * Mathf.Deg2Rad;
        }

        void LateUpdate()
        {
            if (!controlling || cam == null) return;
            float t = Time.time, A = SwayRadians();
            float bob = Mathf.Sin(t * 9f) * 0.03f * Mathf.Min(1f, velocity.magnitude / 4f);
            float sy = A * (Mathf.Sin(t * 1.1f) * 0.7f + Mathf.Sin(t * 2.3f + 1f) * 0.3f);
            float sp = A * (Mathf.Sin(t * 1.7f + 2f) * 0.6f + Mathf.Sin(t * 0.9f) * 0.4f);
            float jx = (UnityEngine.Random.value - 0.5f) * shake * 0.04f, jy = (UnityEngine.Random.value - 0.5f) * shake * 0.04f;
            cam.transform.position = transform.position + Vector3.up * (eyeHeight + bob);
            cam.transform.rotation = Quaternion.Euler(-(pitch + sp + jx) * Mathf.Rad2Deg, (yaw + sy + jy) * Mathf.Rad2Deg, 0f);
        }

        public void Damage(float amount, UI ui, string message)
        {
            float d = amount * (armor ? 0.5f : 1f);
            health -= d;
            courage = Mathf.Clamp(courage - 6f, 0f, 100f);
            shake = Mathf.Max(shake, 0.8f);
            Sfx.Play("hurt");
            ui.Hurt();
            ui.Toast(message);
        }
    }
}
