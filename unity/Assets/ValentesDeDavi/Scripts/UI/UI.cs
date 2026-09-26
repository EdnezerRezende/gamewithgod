using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Toda a interface da fase (HUD, cenas animadas e telas de menu), montada em código com UI Toolkit.
    /// Paleta: terra escura, pergaminho e bronze, a mesma do protótipo em navegador.
    /// </summary>
    public class UI
    {
        public static readonly Color Earth = U.Hex(0x140f0a), Earth2 = U.Hex(0x211810), Parch = U.Hex(0xeadcbd),
            ParchDim = U.Hex(0xb3a483), Bronze = U.Hex(0xc8903f), BronzeHi = U.Hex(0xecbd6a), Olive = U.Hex(0x93a35a),
            Blood = U.Hex(0xc0533a);
        static readonly Color Line = new Color(0.917f, 0.862f, 0.741f, 0.16f);

        public readonly VisualElement root;
        VisualElement crossH, crossV;
        VisualElement statCampo, campoFill, statArma, holdBar, holdFill;
        Label armaLabel, waveLabel, zoneLabel;
        VisualElement hud, cine, overlay, hurt, stonesRow, vidaFill, corFill, statStones, statVida, statCor, statScore, gauge;
        VisualElement statFad, fadFill, stuckFx, statAgua, aguaFill, statAlarme, alarmeFill;
        float stuckAlpha;
        bool stuckOn;
        Label esc, cineHint;
        Label obj, sub, toast, opening, score, cineQuote, cineRef, gaugeText;
        ScrollView overlayScroll;
        Texture2D gaugeTex;
        Color32[] gaugePx;
        float toastT, hurtT;

        public bool OverlayOpen { get { return overlay.style.display == DisplayStyle.Flex; } }

        public UI(Transform parent, PanelSettings panelSettings)
        {
            GameObject go = new GameObject("Interface");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            }
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1600, 900);
            panelSettings.match = 0.5f;
            UIDocument doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            go.SetActive(true);
            root = doc.rootVisualElement;
            root.style.flexGrow = 1f;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(f));
            root.style.color = Parch;

            BuildHud();
            TouchControls.Build(root);
            BuildCine();
            BuildOverlay();
            ShowHud(false);
            ShowCine(false);
            CloseOverlay();
        }

        // ------------------------------------------------------------------ helpers de estilo

        public static void Abs(VisualElement e, float? left, float? top, float? right, float? bottom)
        {
            e.style.position = Position.Absolute;
            if (left.HasValue) e.style.left = left.Value;
            if (top.HasValue) e.style.top = top.Value;
            if (right.HasValue) e.style.right = right.Value;
            if (bottom.HasValue) e.style.bottom = bottom.Value;
        }

        public static void Pad(VisualElement e, float v, float h)
        {
            e.style.paddingTop = v; e.style.paddingBottom = v; e.style.paddingLeft = h; e.style.paddingRight = h;
        }

        public static void Border(VisualElement e, Color c, float w, float radius)
        {
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopLeftRadius = radius; e.style.borderTopRightRadius = radius;
            e.style.borderBottomLeftRadius = radius; e.style.borderBottomRightRadius = radius;
        }

        public static Label Text(VisualElement parent, string text, float size, Color color, bool bold = false, bool italic = false)
        {
            Label l = new Label(text);
            l.style.fontSize = size;
            l.style.color = color;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 0f; l.style.marginBottom = 0f; l.style.marginLeft = 0f; l.style.marginRight = 0f;
            l.style.paddingLeft = 0f; l.style.paddingRight = 0f;
            FontStyle fs = bold && italic ? FontStyle.BoldAndItalic : bold ? FontStyle.Bold : italic ? FontStyle.Italic : FontStyle.Normal;
            l.style.unityFontStyleAndWeight = fs;
            if (parent != null) parent.Add(l);
            return l;
        }

        static VisualElement Box(VisualElement parent)
        {
            VisualElement v = new VisualElement();
            v.pickingMode = PickingMode.Ignore;
            if (parent != null) parent.Add(v);
            return v;
        }

        static VisualElement Layer(VisualElement parent)
        {
            VisualElement v = Box(parent);
            Abs(v, 0, 0, 0, 0);
            return v;
        }

        // ------------------------------------------------------------------ HUD

        void BuildHud()
        {
            hud = Layer(root);

            VisualElement tl = Box(hud);
            Abs(tl, 24, 20, null, null);
            tl.style.maxWidth = 620f;
            obj = Text(tl, "", 26, Parch);
            sub = Text(tl, "", 18, ParchDim);

            VisualElement tr = Box(hud);
            Abs(tr, null, 20, 24, null);
            tr.style.alignItems = Align.FlexEnd;
            statStones = Stat(tr, "PEDRAS"); stonesRow = Box(statStones); stonesRow.style.flexDirection = FlexDirection.Row;
            for (int i = 0; i < 5; i++)
            {
                VisualElement pip = Box(stonesRow);
                pip.style.width = 14f; pip.style.height = 14f; pip.style.marginLeft = 5f;
                Border(pip, Line, 1, 7);
            }
            statVida = Stat(tr, "VIDA"); vidaFill = Bar(statVida, Blood);
            statCor = Stat(tr, "CORAGEM"); corFill = Bar(statCor, BronzeHi);
            statScore = Stat(tr, "PONTOS"); score = Text(statScore, "0", 24, Parch, true);
            statCampo = Stat(tr, "CAMPO"); campoFill = Bar(statCampo, Olive);
            statArma = Stat(tr, "ARMA"); armaLabel = Text(statArma, "", 16, Parch, true);
            statFad = Stat(tr, "CANSAÇO"); fadFill = Bar(statFad, U.Hex(0xd08a4a));
            statAgua = Stat(tr, "ÁGUA"); aguaFill = Bar(statAgua, U.Hex(0x6aa6cf));
            statAlarme = Stat(tr, "ALARME"); alarmeFill = Bar(statAlarme, Blood);
            statAgua.style.display = DisplayStyle.None; statAlarme.style.display = DisplayStyle.None;
            statCampo.style.display = DisplayStyle.None; statArma.style.display = DisplayStyle.None; statFad.style.display = DisplayStyle.None;

            VisualElement tc = Box(hud);
            Abs(tc, 0, 14, 0, null);
            tc.style.alignItems = Align.Center;
            waveLabel = Text(tc, "", 22, Parch);
            waveLabel.style.letterSpacing = 2f;
            holdBar = Box(tc);
            holdBar.style.width = 360f; holdBar.style.height = 12f; holdBar.style.marginTop = 6f;
            holdBar.style.backgroundColor = new Color(0, 0, 0, 0.45f);
            Border(holdBar, BronzeHi, 1, 0);
            holdFill = Box(holdBar);
            holdFill.style.height = Length.Percent(100);
            holdFill.style.backgroundColor = BronzeHi;
            holdBar.style.display = DisplayStyle.None;

            VisualElement zoneWrap = Box(hud);
            Abs(zoneWrap, 0, null, 0, null);
            zoneWrap.style.top = Length.Percent(40);
            zoneWrap.style.alignItems = Align.Center;
            zoneLabel = Text(zoneWrap, "Samá se pôs no meio do campo. Volte para as lentilhas.", 18, U.Hex(0xf3c7a0), true);
            zoneLabel.style.backgroundColor = new Color(0.47f, 0.12f, 0.06f, 0.6f);
            Pad(zoneLabel, 8, 14);
            zoneLabel.style.display = DisplayStyle.None;

            VisualElement cross = Layer(hud);
            cross.style.alignItems = Align.Center;
            cross.style.justifyContent = Justify.Center;
            VisualElement ch = Box(cross);
            ch.style.width = 20f; ch.style.height = 20f;
            VisualElement h1 = crossV = Box(ch); Abs(h1, 9, 0, null, null); h1.style.width = 2f; h1.style.height = 20f; h1.style.backgroundColor = new Color(0.92f, 0.86f, 0.74f, 0.85f);
            VisualElement h2 = crossH = Box(ch); Abs(h2, 0, 9, null, null); h2.style.width = 20f; h2.style.height = 2f; h2.style.backgroundColor = new Color(0.92f, 0.86f, 0.74f, 0.85f);

            VisualElement centerTop = Box(hud);
            Abs(centerTop, 0, null, 0, null);
            centerTop.style.top = Length.Percent(22);
            centerTop.style.alignItems = Align.Center;
            toast = Text(centerTop, "", 24, Parch, true);
            toast.style.unityTextAlign = TextAnchor.MiddleCenter;
            toast.style.maxWidth = 900f;
            opening = Text(centerTop, "ABERTURA", 36, BronzeHi, true);
            opening.style.marginTop = 60f;
            opening.style.letterSpacing = 8f;

            VisualElement bottom = Box(hud);
            Abs(bottom, 0, null, 0, 20);
            bottom.style.alignItems = Align.Center;
            gauge = Box(bottom);
            gauge.style.width = 140f; gauge.style.height = 140f;
            gauge.style.alignItems = Align.Center; gauge.style.justifyContent = Justify.Center;
            gaugeTex = new Texture2D(140, 140, TextureFormat.RGBA32, false);
            gaugeTex.filterMode = FilterMode.Bilinear;
            gaugePx = new Color32[140 * 140];
            gauge.style.backgroundImage = new StyleBackground(Background.FromTexture2D(gaugeTex));
            gaugeText = Text(gauge, "segure para girar", 13, ParchDim);
            gaugeText.style.unityTextAlign = TextAnchor.MiddleCenter;

            esc = Text(hud, "Esc · pausa e menu · H · ajuda de mira · M · música", 14, ParchDim);
            Abs(esc, 24, null, null, 20);

            // Borda escura da "mão pegada à espada" (fase 3).
            stuckFx = Layer(hud);
            Texture2D vig = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 32f, dy = (y - 31.5f) / 32f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    vig.SetPixel(x, y, new Color(0.16f, 0.04f, 0.02f, U.SStep(0.55f, 1.25f, r) * 0.8f));
                }
            vig.wrapMode = TextureWrapMode.Clamp;
            vig.Apply();
            stuckFx.style.backgroundImage = new StyleBackground(Background.FromTexture2D(vig));
            stuckFx.style.opacity = 0f;

            hurt = Layer(hud);
            hurt.style.backgroundColor = new Color(0.63f, 0.12f, 0.06f, 1f);
            hurt.style.opacity = 0f;
        }

        VisualElement Stat(VisualElement parent, string label)
        {
            VisualElement row = Box(parent);
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8f;
            Label l = Text(row, label, 13, ParchDim);
            l.style.letterSpacing = 3f;
            l.style.marginRight = 10f;
            return row;
        }

        VisualElement Bar(VisualElement parent, Color c)
        {
            VisualElement b = Box(parent);
            b.style.width = 160f; b.style.height = 9f;
            b.style.backgroundColor = new Color(0, 0, 0, 0.45f);
            Border(b, Line, 1, 0);
            VisualElement fill = Box(b);
            fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = c;
            return fill;
        }

        public void ShowHud(bool v) { hud.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; }

        /// <summary>Mostra os controles de toque (só quando o toque está ativo) e ajusta as dicas.</summary>
        public void ShowTouch(bool playing)
        {
            bool t = TouchControls.Active;
            TouchControls.Show(t && playing);
            esc.style.display = t ? DisplayStyle.None : DisplayStyle.Flex;
            cineHint.text = t ? "Toque: avançar · ❚❚: pausa" : "Clique: avançar · Enter: pular · Esc: pausa";
        }

        /// <summary>Mira dourada quando está sobre um alvo que conta.</summary>
        public void SetCrosshair(bool onTarget)
        {
            Color c = onTarget ? U.Hex(0xffd166) : new Color(0.92f, 0.86f, 0.74f, 0.85f);
            crossH.style.backgroundColor = c;
            crossV.style.backgroundColor = c;
        }

        public void SetObjective(string title, string detail) { obj.text = title; sub.text = detail; }

        public void Toast(string msg, float dur = 2.2f) { toast.text = msg; toast.style.opacity = 1f; toastT = dur; }

        public void Hurt() { hurtT = 0.45f; }

        public void SetTrainingStats(int points)
        {
            statStones.style.display = DisplayStyle.None; statVida.style.display = DisplayStyle.None;
            statCor.style.display = DisplayStyle.None; statScore.style.display = DisplayStyle.Flex;
            statCampo.style.display = DisplayStyle.None; statArma.style.display = DisplayStyle.None;
            waveLabel.text = ""; holdBar.style.display = DisplayStyle.None; zoneLabel.style.display = DisplayStyle.None;
            statFad.style.display = DisplayStyle.None;
            score.text = points.ToString();
            opening.style.display = DisplayStyle.None;
        }

        public void SetDuelStats(int stonesLeft, float health, float courage, bool showOpening)
        {
            statStones.style.display = DisplayStyle.Flex; statVida.style.display = DisplayStyle.Flex;
            statCor.style.display = DisplayStyle.Flex; statScore.style.display = DisplayStyle.None;
            statCampo.style.display = DisplayStyle.None; statArma.style.display = DisplayStyle.None; statFad.style.display = DisplayStyle.None;
            for (int i = 0; i < 5; i++)
                stonesRow[i].style.backgroundColor = i < stonesLeft ? U.Hex(0x9a9a92) : new Color(0, 0, 0, 0);
            vidaFill.style.width = Length.Percent(Mathf.Clamp(health, 0, 100));
            corFill.style.width = Length.Percent(Mathf.Clamp(courage, 0, 100));
            opening.style.display = showOpening ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>HUD da fase 2: vida, coragem, campo, pedras, arma, onda e barra de "Permaneça".</summary>
        public void SetBattleStats(float health, float courage, float field, int stonesLeft, string weapon, string wave, float hold01, bool zoneWarning)
        {
            statStones.style.display = DisplayStyle.Flex; statVida.style.display = DisplayStyle.Flex;
            statCor.style.display = DisplayStyle.Flex; statScore.style.display = DisplayStyle.None;
            statCampo.style.display = DisplayStyle.Flex; statArma.style.display = DisplayStyle.Flex; statFad.style.display = DisplayStyle.None;
            opening.style.display = DisplayStyle.None;
            for (int i = 0; i < 5; i++)
                stonesRow[i].style.backgroundColor = i < stonesLeft ? U.Hex(0x9a9a92) : new Color(0, 0, 0, 0);
            vidaFill.style.width = Length.Percent(Mathf.Clamp(health, 0, 100));
            corFill.style.width = Length.Percent(Mathf.Clamp(courage, 0, 100));
            campoFill.style.width = Length.Percent(Mathf.Clamp(field, 0, 100));
            armaLabel.text = weapon;
            waveLabel.text = wave;
            holdBar.style.display = hold01 >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (hold01 >= 0f) holdFill.style.width = Length.Percent(Mathf.Clamp01(hold01) * 100f);
            zoneLabel.style.display = zoneWarning ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// HUD da fase 3. No treino: pontos e cansaço. Na batalha: vida, coragem, cansaço, a linha
        /// atual, a barra da resistência final e o aviso de recuo para trás do estandarte.
        /// </summary>
        public void SetFase3Stats(bool battle, int points, float health, float courage, float fatigue, string wave, float hold01, bool zoneWarning, bool stuck)
        {
            statStones.style.display = DisplayStyle.None; statCampo.style.display = DisplayStyle.None; statArma.style.display = DisplayStyle.None;
            statVida.style.display = battle ? DisplayStyle.Flex : DisplayStyle.None;
            statCor.style.display = battle ? DisplayStyle.Flex : DisplayStyle.None;
            statScore.style.display = battle ? DisplayStyle.None : DisplayStyle.Flex;
            statFad.style.display = DisplayStyle.Flex;
            opening.style.display = DisplayStyle.None;
            score.text = points.ToString();
            vidaFill.style.width = Length.Percent(Mathf.Clamp(health, 0, 100));
            corFill.style.width = Length.Percent(Mathf.Clamp(courage, 0, 100));
            fadFill.style.width = Length.Percent(Mathf.Clamp(fatigue, 0, 100));
            waveLabel.text = wave;
            holdBar.style.display = hold01 >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (hold01 >= 0f) holdFill.style.width = Length.Percent(Mathf.Clamp01(hold01) * 100f);
            zoneLabel.text = "Você recuou para trás do estandarte. Eleazar não voltou atrás.";
            zoneLabel.style.display = zoneWarning ? DisplayStyle.Flex : DisplayStyle.None;
            stuckOn = stuck;
        }

        /// <summary>
        /// HUD da fase 4. No treino: pontos (e água no percurso do cântaro). Na missão: vida, coragem,
        /// água (com o cântaro cheio), alarme do arraial e o aviso sobre os companheiros.
        /// </summary>
        public void SetFase4Stats(bool mission, int points, float health, float courage, bool carrying, float water, bool showAlarm, float alarm, string wave, string warning)
        {
            statStones.style.display = DisplayStyle.None; statCampo.style.display = DisplayStyle.None; statArma.style.display = DisplayStyle.None;
            statFad.style.display = DisplayStyle.None; opening.style.display = DisplayStyle.None;
            statVida.style.display = mission ? DisplayStyle.Flex : DisplayStyle.None;
            statCor.style.display = mission ? DisplayStyle.Flex : DisplayStyle.None;
            statScore.style.display = mission ? DisplayStyle.None : DisplayStyle.Flex;
            statAgua.style.display = carrying ? DisplayStyle.Flex : DisplayStyle.None;
            statAlarme.style.display = showAlarm ? DisplayStyle.Flex : DisplayStyle.None;
            score.text = points.ToString();
            vidaFill.style.width = Length.Percent(Mathf.Clamp(health, 0, 100));
            corFill.style.width = Length.Percent(Mathf.Clamp(courage, 0, 100));
            aguaFill.style.width = Length.Percent(Mathf.Clamp(water, 0, 100));
            alarmeFill.style.width = Length.Percent(Mathf.Clamp(alarm, 0, 100));
            waveLabel.text = wave;
            holdBar.style.display = DisplayStyle.None;
            zoneLabel.text = warning ?? "";
            zoneLabel.style.display = string.IsNullOrEmpty(warning) ? DisplayStyle.None : DisplayStyle.Flex;
            stuckOn = false;
        }

        /// <summary>Indicador simples em anel (espada carregando o golpe forte, oração).</summary>
        public void DrawRing(float fill01, Color color, string text)
        {
            Color32 track = new Color32(234, 220, 189, 50), clear = new Color32(0, 0, 0, 0), c32 = color;
            float deg = Mathf.Clamp01(fill01) * 360f;
            for (int y = 0; y < 140; y++)
                for (int x = 0; x < 140; x++)
                {
                    float dx = x - 69.5f, dy = y - 69.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; if (ang < 0f) ang += 360f;
                    Color32 c = clear;
                    if (r > 41f && r < 47f) c = (fill01 > 0f && ang <= deg) ? c32 : track;
                    gaugePx[y * 140 + x] = c;
                }
            gaugeTex.SetPixels32(gaugePx);
            gaugeTex.Apply(false);
            gaugeText.text = text;
            gaugeText.style.color = ParchDim;
        }

        public void SetHint(string text) { esc.text = text; }

        public void Tick(float unscaledDt)
        {
            if (toastT > 0f)
            {
                toastT -= unscaledDt;
                if (toastT <= 0f) toast.style.opacity = 0f;
            }
            stuckAlpha = Mathf.MoveTowards(stuckAlpha, stuckOn ? 1f : 0f, unscaledDt / 1.2f);
            stuckFx.style.opacity = stuckAlpha;
            if (hurtT > 0f) hurtT -= unscaledDt;
            hurt.style.opacity = Mathf.Clamp01(hurtT / 0.45f) * 0.4f;
        }

        /// <summary>Desenha o indicador do giro da funda: faixa dourada, ponto da pedra e anel de força.</summary>
        public void DrawGauge(Sling s)
        {
            bool arcVisible = Difficulty.Current.sweetArcVisible;
            float half = s.SweetArcDegrees() / 2f;
            float dotAng = (s.Phase - 0.75f) * 360f;
            float dotR = s.inZone ? 8f : 6f;
            Vector2 dot = new Vector2(Mathf.Sin(dotAng * Mathf.Deg2Rad), Mathf.Cos(dotAng * Mathf.Deg2Rad)) * 44f;
            Color32 track = new Color32(234, 220, 189, 50), gold = new Color32(236, 189, 106, 255),
                powerC = s.chargeTime > 3f ? new Color32(192, 83, 58, 255) : new Color32(147, 163, 90, 255),
                dotC = s.inZone ? new Color32(255, 241, 196, 255) : new Color32(234, 220, 189, 255), clear = new Color32(0, 0, 0, 0);
            float powerDeg = s.Power * 360f;
            for (int y = 0; y < 140; y++)
            {
                for (int x = 0; x < 140; x++)
                {
                    float dx = x - 69.5f, dy = y - 69.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    if (ang < 0f) ang += 360f;
                    Color32 c = clear;
                    if (r > 41f && r < 47f) c = track;
                    if (arcVisible && r > 40f && r < 48f && Mathf.Min(ang, 360f - ang) <= half) c = gold;
                    if (s.charging)
                    {
                        if (r > 53f && r < 57f && ang <= powerDeg) c = powerC;
                        if (arcVisible && (new Vector2(dx, dy) - dot).sqrMagnitude < dotR * dotR) c = dotC;
                    }
                    gaugePx[y * 140 + x] = c;
                }
            }
            gaugeTex.SetPixels32(gaugePx);
            gaugeTex.Apply(false);
            gaugeText.text = s.charging ? (!s.reach ? "longe: gire mais" : arcVisible ? "" : "ouça o giro") : "segure para girar";
            gaugeText.style.color = s.charging && !s.reach ? U.Hex(0xe07a5f) : ParchDim;
        }

        // ------------------------------------------------------------------ cenas animadas

        void BuildCine()
        {
            cine = Layer(root);
            VisualElement top = Box(cine); Abs(top, 0, 0, 0, null); top.style.height = Length.Percent(11); top.style.backgroundColor = Color.black;
            VisualElement bot = Box(cine); Abs(bot, 0, null, 0, 0); bot.style.height = Length.Percent(11); bot.style.backgroundColor = Color.black;
            VisualElement text = Box(cine);
            Abs(text, 24, null, 24, null);
            text.style.bottom = Length.Percent(13);
            text.style.alignItems = Align.Center;
            cineQuote = Text(text, "", 28, Parch, false, true);
            cineQuote.style.unityTextAlign = TextAnchor.MiddleCenter;
            cineQuote.style.maxWidth = 1000f;
            cineRef = Text(text, "", 15, BronzeHi);
            cineRef.style.letterSpacing = 4f;
            cineRef.style.marginTop = 8f;
            cineHint = Text(cine, "Clique: avançar · Enter: pular · Esc: pausa", 14, ParchDim);
            Abs(cineHint, null, null, 24, 14);
        }

        public void ShowCine(bool v) { cine.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; }

        public void SetCineLine(string quote, string reference)
        {
            cineQuote.text = quote;
            cineRef.text = reference.ToUpperInvariant();
        }

        // ------------------------------------------------------------------ telas

        void BuildOverlay()
        {
            overlay = new VisualElement();
            Abs(overlay, 0, 0, 0, 0);
            overlay.style.backgroundColor = new Color(0.078f, 0.059f, 0.039f, 0.86f);
            root.Add(overlay);
            overlayScroll = new ScrollView(ScrollViewMode.Vertical);
            overlayScroll.style.flexGrow = 1f;
            overlay.Add(overlayScroll);
        }

        public Card OpenCard()
        {
            overlayScroll.Clear();
            VisualElement wrap = new VisualElement();
            wrap.style.alignItems = Align.Center;
            Pad(wrap, 56, 24);
            overlayScroll.Add(wrap);
            VisualElement card = new VisualElement();
            card.style.width = Length.Percent(100);
            card.style.maxWidth = 860f;
            wrap.Add(card);
            overlay.style.display = DisplayStyle.Flex;
            overlayScroll.scrollOffset = Vector2.zero;
            return new Card(card);
        }

        public void CloseOverlay() { overlay.style.display = DisplayStyle.None; }
    }

    /// <summary>Monta o conteúdo de uma tela (menu, resultado, escolhas).</summary>
    public class Card
    {
        public readonly VisualElement el;
        public Card(VisualElement e) { el = e; }

        VisualElement Gap(VisualElement v, float g) { v.style.marginBottom = g; el.Add(v); return v; }

        public void Eyebrow(string t)
        {
            Label l = UI.Text(null, t.ToUpperInvariant(), 14, UI.BronzeHi);
            l.style.letterSpacing = 4f;
            Gap(l, 10);
        }

        public void Title(string t, float size = 46) { Gap(UI.Text(null, t, size, UI.Parch), 18); }

        public void Lede(string t) { Label l = UI.Text(null, t, 20, UI.ParchDim); l.style.maxWidth = 760f; Gap(l, 20); }

        public void Note(string t) { Gap(UI.Text(null, t, 15, UI.ParchDim), 12); }

        public void Verse(string key)
        {
            Verses.Verse v = Verses.Get(key);
            VisualElement b = new VisualElement();
            b.style.borderLeftColor = UI.Bronze; b.style.borderLeftWidth = 3f; b.style.paddingLeft = 16f;
            b.style.maxWidth = 760f;
            UI.Text(b, v.text, 20, UI.Parch, false, true);
            Label r = UI.Text(b, v.reference.ToUpperInvariant(), 13, UI.BronzeHi);
            r.style.letterSpacing = 3f; r.style.marginTop = 6f;
            Gap(b, 22);
        }

        public void Medal(string t, Color c) { Gap(UI.Text(null, t, 30, c), 14); }

        public void Stars(int n)
        {
            VisualElement row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
            for (int i = 0; i < 3; i++) UI.Text(row, "★", 44, i < n ? UI.BronzeHi : new Color(0.92f, 0.86f, 0.74f, 0.2f));
            Gap(row, 12);
        }

        /// <summary>Linhas rótulo/valor. A última linha é o total quando highlightLast = true.</summary>
        public void Tally(string[] labels, string[] values, bool highlightLast)
        {
            VisualElement t = new VisualElement(); t.style.maxWidth = 520f;
            for (int i = 0; i < labels.Length; i++)
            {
                bool total = highlightLast && i == labels.Length - 1;
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom = 6f;
                if (total) { row.style.borderTopWidth = 1f; row.style.borderTopColor = new Color(0.92f, 0.86f, 0.74f, 0.2f); row.style.paddingTop = 8f; }
                UI.Text(row, labels[i], total ? 26 : 18, total ? UI.Parch : UI.ParchDim);
                UI.Text(row, values[i], total ? 26 : 18, UI.Parch, total);
                t.Add(row);
            }
            Gap(t, 18);
        }

        public void Check(bool ok, string text, string reference)
        {
            VisualElement row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 6f;
            Label m = UI.Text(row, ok ? "✓" : "✗", 20, ok ? UI.Olive : UI.Blood, true); m.style.width = 26f;
            UI.Text(row, text, 18, UI.Parch);
            if (!string.IsNullOrEmpty(reference)) { Label r = UI.Text(row, "  " + reference, 15, UI.ParchDim); r.style.alignSelf = Align.FlexEnd; }
            el.Add(row);
        }

        public void Space(float h) { VisualElement v = new VisualElement(); v.style.height = h; el.Add(v); }

        public VisualElement Row()
        {
            VisualElement r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row; r.style.flexWrap = Wrap.Wrap;
            Gap(r, 18);
            return r;
        }

        public static Button Btn(VisualElement row, string text, bool primary, Action onClick)
        {
            Button b = new Button(onClick);
            b.text = text;
            b.style.fontSize = 18f;
            b.style.color = primary ? U.Hex(0x1b120a) : UI.Parch;
            b.style.backgroundColor = primary ? UI.Bronze : new Color(0.92f, 0.86f, 0.74f, 0.07f);
            b.style.unityFontStyleAndWeight = primary ? FontStyle.Bold : FontStyle.Normal;
            UI.Border(b, primary ? UI.Bronze : new Color(0.92f, 0.86f, 0.74f, 0.25f), 1, 3);
            UI.Pad(b, 12, 22);
            b.style.marginRight = 12f; b.style.marginBottom = 10f; b.style.marginLeft = 0f; b.style.marginTop = 0f;
            row.Add(b);
            return b;
        }

        /// <summary>Grade de opções grandes (dificuldade, armadura).</summary>
        public VisualElement Choices()
        {
            VisualElement g = new VisualElement();
            g.style.flexDirection = FlexDirection.Row; g.style.flexWrap = Wrap.Wrap;
            Gap(g, 16);
            return g;
        }

        public static Button Choice(VisualElement grid, string title, string desc, bool pressed, Action onClick)
        {
            Button b = new Button(onClick);
            b.text = "";
            b.style.flexDirection = FlexDirection.Column;
            b.style.alignItems = Align.FlexStart;
            b.style.width = 260f; b.style.minHeight = 120f;
            b.style.backgroundColor = pressed ? new Color(0.78f, 0.56f, 0.25f, 0.18f) : new Color(0.13f, 0.094f, 0.063f, 0.85f);
            UI.Border(b, pressed ? UI.BronzeHi : new Color(0.92f, 0.86f, 0.74f, 0.2f), pressed ? 2 : 1, 3);
            UI.Pad(b, 14, 16);
            b.style.marginRight = 12f; b.style.marginBottom = 12f; b.style.marginLeft = 0f; b.style.marginTop = 0f;
            UI.Text(b, title, 24, UI.Parch);
            Label d = UI.Text(b, desc, 16, UI.ParchDim); d.style.marginTop = 6f;
            grid.Add(b);
            return b;
        }

        /// <summary>Rótulo pequeno em maiúsculas que separa as partes do menu.</summary>
        public void Section(string t)
        {
            Label l = UI.Text(null, t.ToUpperInvariant(), 13, UI.ParchDim);
            l.style.letterSpacing = 3f;
            Gap(l, 8);
        }

        /// <summary>
        /// Mapa das fases: atual, liberada (clicar leva à fase), concluída (estrelas e recorde),
        /// trancada ou ainda não montada na Unity.
        /// </summary>
        public void PhaseMap(int current)
        {
            Section("Escolha a fase");
            VisualElement g = new VisualElement();
            g.style.flexDirection = FlexDirection.Row; g.style.flexWrap = Wrap.Wrap;
            Gap(g, 14);
            foreach (Progress.Phase ph in Progress.All)
            {
                Progress.Phase p = ph;
                bool cur = p.n == current, open = Progress.IsOpen(p.n), won = Progress.Won(p.n);
                bool clickable = !cur && open && p.Built;
                Button b = new Button(() => { if (clickable) Progress.Load(p); });
                b.text = "";
                b.focusable = clickable;
                b.style.flexDirection = FlexDirection.Column; b.style.alignItems = Align.FlexStart;
                b.style.width = 240f; b.style.minHeight = 118f;
                b.style.backgroundColor = cur ? new Color(0.78f, 0.56f, 0.25f, 0.18f) : new Color(0.13f, 0.094f, 0.063f, 0.85f);
                UI.Border(b, cur ? UI.BronzeHi : new Color(0.92f, 0.86f, 0.74f, 0.2f), cur ? 2 : 1, 3);
                UI.Pad(b, 12, 14);
                b.style.marginRight = 10f; b.style.marginBottom = 10f; b.style.marginLeft = 0f; b.style.marginTop = 0f;
                if (!open || !p.Built) b.style.opacity = 0.5f;
                Label eb = UI.Text(b, ("Fase " + p.n + (cur ? " · atual" : "")).ToUpperInvariant(), 12, UI.BronzeHi);
                eb.style.letterSpacing = 3f;
                Label t = UI.Text(b, p.title, 20, UI.Parch);
                t.style.whiteSpace = WhiteSpace.Normal; t.style.marginTop = 4f;
                UI.Text(b, p.reference, 14, UI.ParchDim);
                string status = !open ? "Trancada: vença a fase " + (p.n - 1) + " para liberar"
                    : won ? new string('★', Progress.Stars(p.n)) + " (" + Progress.Stars(p.n) + " de 3) · recorde " + Progress.Best(p.n)
                    : !p.Built ? "Em breve na Unity (já jogável no navegador)"
                    : cur ? "Você está aqui" : "Liberada";
                Label st = UI.Text(b, status, 14, won ? UI.BronzeHi : UI.ParchDim);
                st.style.whiteSpace = WhiteSpace.Normal; st.style.marginTop = 6f;
                g.Add(b);
            }
        }

        /// <summary>Aviso no lugar dos botões de jogar quando a fase ainda está trancada.</summary>
        public void Locked(int n)
        {
            Progress.Phase prev = Progress.Get(n - 1);
            VisualElement box = new VisualElement();
            box.style.maxWidth = 760f;
            box.style.backgroundColor = new Color(0.13f, 0.094f, 0.063f, 0.7f);
            UI.Border(box, UI.Bronze, 1, 3);
            UI.Pad(box, 14, 16);
            Label l = UI.Text(box, "Esta fase ainda está trancada. Vença a Fase " + prev.n + ", " + prev.title + ", para liberar.", 19, UI.Parch);
            l.style.whiteSpace = WhiteSpace.Normal; l.style.marginBottom = 12f;
            VisualElement row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
            Btn(row, "Ir para a Fase " + prev.n, true, () => Progress.Load(prev));
            box.Add(row);
            Gap(box, 18);
        }

        /// <summary>Na tela de resultados: avisa a liberação e leva à fase seguinte.</summary>
        public void NextPhase(int n, bool fresh)
        {
            Progress.Phase next = Progress.Get(n + 1);
            if (next == null) return;
            if (fresh) Gap(UI.Text(null, "Fase " + next.n + " liberada!", 22, UI.BronzeHi, true), 8);
            if (next.Built) Btn(Row(), "Ir para a Fase " + next.n + ": " + next.title + " →", false, () => Progress.Load(next));
            else Note("A Fase " + next.n + " (" + next.title + ") ainda não foi montada na Unity.");
        }

        /// <summary>Grade de pedras do ribeiro, cada uma com sua imagem gerada.</summary>
        public List<Button> Stones(Texture2D[] images, Action<int> onClick)
        {
            VisualElement g = new VisualElement();
            g.style.flexDirection = FlexDirection.Row; g.style.flexWrap = Wrap.Wrap; g.style.maxWidth = 600f;
            List<Button> list = new List<Button>();
            for (int i = 0; i < images.Length; i++)
            {
                int idx = i;
                Button b = new Button(() => onClick(idx));
                b.text = "";
                b.style.width = 128f; b.style.height = 128f;
                b.style.marginRight = 12f; b.style.marginBottom = 12f; b.style.marginLeft = 0f; b.style.marginTop = 0f;
                b.style.backgroundColor = new Color(0.24f, 0.31f, 0.32f, 0.35f);
                UI.Border(b, new Color(0.92f, 0.86f, 0.74f, 0.2f), 1, 3);
                VisualElement img = new VisualElement();
                img.pickingMode = PickingMode.Ignore;
                img.style.flexGrow = 1f;
                img.style.backgroundImage = new StyleBackground(Background.FromTexture2D(images[i]));
                b.Add(img);
                Label n = UI.Text(b, "", 15, UI.BronzeHi, true);
                UI.Abs(n, 6, 4, null, null);
                n.name = "ordem";
                g.Add(b);
                list.Add(b);
            }
            Gap(g, 18);
            return list;
        }

        public static void MarkStone(Button b, int order)
        {
            bool on = order > 0;
            b.style.backgroundColor = on ? new Color(0.78f, 0.56f, 0.25f, 0.22f) : new Color(0.24f, 0.31f, 0.32f, 0.35f);
            UI.Border(b, on ? UI.BronzeHi : new Color(0.92f, 0.86f, 0.74f, 0.2f), on ? 2 : 1, 3);
            Label n = b.Q<Label>("ordem");
            if (n != null) n.text = on ? order.ToString() : "";
        }

        public static void Controls(Card c, string[] keys, string[] actions)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                VisualElement row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 4f;
                Label k = UI.Text(row, keys[i], 16, UI.Parch, true); k.style.width = 230f;
                UI.Text(row, actions[i], 16, UI.ParchDim);
                c.el.Add(row);
            }
            c.Space(14);
        }
    }
}
