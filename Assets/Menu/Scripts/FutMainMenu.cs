using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace FutMenu
{
    // Menu principal de FUT 27: pantalla de inicio, intro de focos y logo, y menu navegable
    // con teclado, mando y raton. Las opciones solo muestran un aviso (pantallas pendientes).
    public class FutMainMenu : MonoBehaviour
    {
        const float IntroLength = 5.6f;
        const int Columns = 3;

        enum Phase { Gate, Intro, Menu }

        [Header("Fondo")]
        public RectTransform world;
        public RawImage stage;
        public RawImage introPic;
        public Texture2D backgroundTexture;
        public RectTransform fogA;
        public RectTransform fogB;

        [Header("Menu")]
        public CanvasGroup logoGroup;
        public CanvasGroup iconsGroup;
        public Image musicIcon;
        public string[] itemNames;
        public CanvasGroup[] itemGroups;
        public RectTransform[] itemRects;
        public Text[] itemTexts;
        public RectTransform[] underlines;
        public Text toastText;
        public CanvasGroup toastGroup;

        [Header("Intro")]
        public CanvasGroup introGroup;
        public Image[] flares;
        public Image introVignette;
        public CanvasGroup introLogoGroup;
        public RectTransform introLogo;
        public Image flareLine;
        public RectTransform flareLineRect;
        public CanvasGroup gateGroup;
        public CanvasGroup hintGroup;

        [Header("Audio")]
        public AudioClip clipIntro;
        public AudioClip clipMenuAppears;
        public AudioClip clipMove;
        public AudioClip clipConfirm;
        public AudioClip clipAmbient;

        static readonly float[] FlareDelay = { 0.42f, 0.82f, 1.26f, 1.74f };
        static readonly float[] BrightKeys = { 0.10f, 0.14f, 0.28f, 0.33f, 0.48f, 0.53f, 0.70f };
        static readonly float[] BrightVals = { 0.25f, 0.08f, 0.45f, 0.20f, 0.65f, 0.38f, 0.80f };

        Phase phase = Phase.Gate;
        float clock;
        float introT;
        float menuT;
        float toastTimer;
        int idx;
        bool muted;
        bool prevL, prevR, prevU, prevD;
        int lastW, lastH;
        Vector2[] itemBase;
        AudioSource sfx;
        AudioSource introSrc;
        AudioSource ambientSrc;

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            introSrc = gameObject.AddComponent<AudioSource>();
            ambientSrc = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            introSrc.playOnAwake = false;
            introSrc.clip = clipIntro;
            ambientSrc.playOnAwake = false;
            ambientSrc.loop = true;
            ambientSrc.clip = clipAmbient;
            ambientSrc.volume = 0.7f;

            itemBase = new Vector2[itemRects.Length];
            for (int i = 0; i < itemRects.Length; i++) itemBase[i] = itemRects[i].anchoredPosition;

            AudioListener.volume = 1f;
            ShowGate();
        }

        // ---------------- Estados ----------------

        void ShowGate()
        {
            phase = Phase.Gate;
            HideMenu();
            introGroup.alpha = 1f;
            introGroup.blocksRaycasts = true;
            gateGroup.alpha = 1f;
            UpdateIntro(0f);
        }

        void HideMenu()
        {
            logoGroup.alpha = 0f;
            iconsGroup.alpha = 0f;
            for (int i = 0; i < itemGroups.Length; i++) itemGroups[i].alpha = 0f;
            toastGroup.alpha = 0f;
            toastTimer = 0f;
        }

        void BeginIntro()
        {
            phase = Phase.Intro;
            introT = 0f;
            gateGroup.alpha = 0f;
            introGroup.alpha = 1f;
            introGroup.blocksRaycasts = true;
            HideMenu();
            introSrc.Stop();
            introSrc.volume = 1f;
            if (clipIntro != null) introSrc.Play();
            UpdateIntro(0f);
        }

        void Ready(bool skipped)
        {
            phase = Phase.Menu;
            menuT = 0f;
            introGroup.blocksRaycasts = false;
            if (skipped && introSrc.isPlaying) StartCoroutine(FadeOut(introSrc, 0.3f));
            PlaySfx(clipMenuAppears, 0.7f);
            if (clipAmbient != null && !ambientSrc.isPlaying) ambientSrc.Play();
        }

        IEnumerator FadeOut(AudioSource src, float seconds)
        {
            float v = src.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                src.volume = Mathf.Lerp(v, 0f, t / seconds);
                yield return null;
            }
            src.Stop();
            src.volume = v;
        }

        // ---------------- Bucle principal ----------------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            clock += dt;
            AnimateBackground();
            FitCoverIfNeeded();

            if (phase == Phase.Gate)
            {
                gateGroup.alpha = Mathf.Lerp(0.45f, 1f, 0.5f + 0.5f * Mathf.Cos(clock * Mathf.PI));
                if (AnyPress()) BeginIntro();
            }
            else if (phase == Phase.Intro)
            {
                introT += dt;
                UpdateIntro(introT);
                if (introT >= IntroLength) Ready(false);
                else if (AnyPress()) Ready(true);
            }
            else
            {
                UpdateMenu(dt);
            }
        }

        void AnimateBackground()
        {
            float e = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(clock / 28f, 1f));
            world.localScale = Vector3.one * (1f + 0.05f * e);
            world.anchoredPosition = new Vector2(-0.01f * 1920f * e, 0.01f * 1080f * e);

            if (fogA != null)
            {
                fogA.anchoredPosition = new Vector2(Mathf.Sin(clock * 0.285f) * 120f - 20f, Mathf.Sin(clock * 0.37f) * 40f);
                float s = 1.06f + 0.06f * Mathf.Sin(clock * 0.2f);
                fogA.localScale = new Vector3(s, s, 1f);
            }
            if (fogB != null)
            {
                fogB.anchoredPosition = new Vector2(Mathf.Sin(clock * 0.2f + 1.5f) * 130f, Mathf.Sin(clock * 0.31f + 0.7f) * 35f);
                float s = 1.05f + 0.05f * Mathf.Sin(clock * 0.17f + 2f);
                fogB.localScale = new Vector3(s, s, 1f);
            }
        }

        // Recorta la foto como CSS background-size: cover, con foco en 50% / 68%
        void FitCoverIfNeeded()
        {
            if (backgroundTexture == null) return;
            if (lastW == Screen.width && lastH == Screen.height && lastW > 0) return;
            if (!FitCover(stage) || !FitCover(introPic)) return;
            lastW = Screen.width;
            lastH = Screen.height;
        }

        bool FitCover(RawImage img)
        {
            if (img == null) return true;
            Rect r = img.rectTransform.rect;
            if (r.width < 1f || r.height < 1f) return false;
            float tw = backgroundTexture.width;
            float th = backgroundTexture.height;
            float scale = Mathf.Max(r.width / tw, r.height / th);
            float uw = r.width / (tw * scale);
            float uh = r.height / (th * scale);
            img.uvRect = new Rect((1f - uw) * 0.5f, (1f - uh) * (1f - 0.68f), uw, uh);
            return true;
        }

        // ---------------- Intro ----------------

        static float EaseOut(float k)
        {
            float q = 1f - k;
            return 1f - q * q * q;
        }

        static float Brightness(float t)
        {
            float u = t / 2.2f;
            float v = 0f;
            for (int i = 0; i < BrightKeys.Length; i++)
                if (u >= BrightKeys[i]) v = BrightVals[i];
            return v;
        }

        static void FlareState(float u, out float alpha, out float scale)
        {
            if (u <= 0f) { alpha = 0f; scale = 0.5f; }
            else if (u < 0.08f) { float k = u / 0.08f; alpha = k; scale = Mathf.Lerp(0.5f, 1f, k); }
            else if (u < 0.25f) { float k = (u - 0.08f) / 0.17f; alpha = Mathf.Lerp(1f, 0.4f, k); scale = 1f; }
            else { float k = (u - 0.25f) / 0.75f; alpha = Mathf.Lerp(0.4f, 0.15f, k); scale = Mathf.Lerp(1f, 1.1f, k); }
        }

        void UpdateIntro(float t)
        {
            float b = Brightness(t - 0.2f);
            introPic.color = new Color(b, b, b, 1f);
            float push = Mathf.Lerp(1.12f, 1f, EaseOut(Mathf.Clamp01((t - 0.2f) / 7f)));
            introPic.rectTransform.localScale = new Vector3(push, push, 1f);

            for (int i = 0; i < flares.Length; i++)
            {
                float alpha, scale;
                FlareState(Mathf.Clamp01((t - FlareDelay[i]) / 1.8f), out alpha, out scale);
                flares[i].color = new Color(1f, 0.98f, 0.91f, alpha);
                flares[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            introVignette.color = new Color(1f, 1f, 1f, Mathf.Clamp01((t - 2.2f) / 1f));

            float e = EaseOut(Mathf.Clamp01((t - 2.3f) / 1.1f));
            introLogoGroup.alpha = e;
            float ls = Mathf.Lerp(1.3f, 1f, e);
            introLogo.localScale = new Vector3(ls, ls, 1f);

            float fu = Mathf.Clamp01((t - 3.3f) / 1.3f);
            float op, sx;
            if (fu < 0.3f) { op = fu / 0.3f; sx = fu / 0.3f; }
            else { float k = (fu - 0.3f) / 0.7f; op = 1f - k; sx = Mathf.Lerp(1f, 1.15f, k); }
            flareLine.color = new Color(1f, 1f, 1f, op);
            flareLineRect.localScale = new Vector3(sx, 1f, 1f);

            hintGroup.alpha = Mathf.Clamp01((t - 1f) / 0.6f);
        }

        // ---------------- Menu ----------------

        static float Fade(float t, float delay)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / 0.7f));
        }

        void UpdateMenu(float dt)
        {
            menuT += dt;
            introGroup.alpha = 1f - Mathf.Clamp01(menuT / 1.1f);

            logoGroup.alpha = Fade(menuT, 0.2f);
            iconsGroup.alpha = Fade(menuT, 0.3f);

            for (int i = 0; i < itemGroups.Length; i++)
            {
                float p = Fade(menuT, 0.5f + 0.1f * i);
                itemGroups[i].alpha = p;
                itemRects[i].anchoredPosition = itemBase[i] + new Vector2(0f, -10f * (1f - p));

                bool sel = i == idx;
                Text label = itemTexts[i];
                float full = label.preferredWidth + 0.8f * label.fontSize;
                RectTransform ul = underlines[i];
                float w = Mathf.Lerp(ul.sizeDelta.x, sel ? full * 0.6f : 0f, 1f - Mathf.Exp(-18f * dt));
                ul.sizeDelta = new Vector2(w, ul.sizeDelta.y);
                float sc = Mathf.Lerp(label.rectTransform.localScale.x, sel ? 1.05f : 1f, 1f - Mathf.Exp(-14f * dt));
                label.rectTransform.localScale = new Vector3(sc, sc, 1f);
            }

            if (toastTimer > 0f) toastTimer -= dt;
            toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, toastTimer > 0f ? 1f : 0f, dt * 4f);

            HandleMenuInput();
        }

        void HandleMenuInput()
        {
            int dx = 0;
            int dy = 0;
            bool ok = false;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dx = -1;
                else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx = 1;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dy = -1;
                else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dy = 1;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) ok = true;
                if (kb.fKey.wasPressedThisFrame) Screen.fullScreen = !Screen.fullScreen;
            }

            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 st = gp.leftStick.ReadValue();
                bool l = gp.dpad.left.isPressed || st.x < -0.6f;
                bool r = gp.dpad.right.isPressed || st.x > 0.6f;
                bool u = gp.dpad.up.isPressed || st.y > 0.6f;
                bool d = gp.dpad.down.isPressed || st.y < -0.6f;
                if (l && !prevL) dx = -1;
                if (r && !prevR) dx = 1;
                if (u && !prevU) dy = -1;
                if (d && !prevD) dy = 1;
                prevL = l; prevR = r; prevU = u; prevD = d;
                if (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame) ok = true;
            }

            int col = idx % Columns;
            if (dx < 0 && col > 0) SelectItem(idx - 1, true);
            else if (dx > 0 && col < Columns - 1 && idx + 1 < itemNames.Length) SelectItem(idx + 1, true);
            if (dy < 0 && idx - Columns >= 0) SelectItem(idx - Columns, true);
            else if (dy > 0 && idx + Columns < itemNames.Length) SelectItem(idx + Columns, true);

            if (ok) Confirm(idx);
        }

        void SelectItem(int i, bool sound)
        {
            i = Mathf.Clamp(i, 0, itemNames.Length - 1);
            if (i != idx && sound) PlaySfx(clipMove, 1f);
            idx = i;
        }

        void Confirm(int i)
        {
            PlaySfx(clipConfirm, 1f);
            ShowToast("Pantalla pendiente: " + itemNames[i]);
        }

        void ShowToast(string message)
        {
            toastText.text = message;
            toastTimer = 1.8f;
        }

        void ToggleMute()
        {
            PlaySfx(clipConfirm, 1f);
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;
            musicIcon.color = new Color(0.925f, 0.929f, 0.929f, muted ? 0.4f : 1f);
            ShowToast(muted ? "Sonido desactivado" : "Sonido activado");
        }

        void PlaySfx(AudioClip clip, float volume)
        {
            if (clip != null) sfx.PlayOneShot(clip, volume);
        }

        // ---------------- Eventos de raton / toque ----------------

        public void OnHover(int id)
        {
            if (phase == Phase.Menu && id >= 0 && id < itemNames.Length) SelectItem(id, true);
        }

        public void OnClick(int id)
        {
            if (phase != Phase.Menu) return;
            if (id >= 0 && id < itemNames.Length) { SelectItem(id, false); Confirm(id); }
            else if (id == 100) { PlaySfx(clipConfirm, 1f); ShowToast("Pantalla pendiente: Editar perfil"); }
            else if (id == 101) { PlaySfx(clipConfirm, 1f); ShowToast("Pantalla pendiente: Añadir amigos"); }
            else if (id == 102) ToggleMute();
            else if (id == 200) BeginIntro();
        }

        static bool AnyPress()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;

            Mouse ms = Mouse.current;
            if (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame)) return true;

            Touchscreen ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) return true;

            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                foreach (InputControl c in gp.allControls)
                {
                    ButtonControl b = c as ButtonControl;
                    if (b != null && b.wasPressedThisFrame) return true;
                }
            }
            return false;
        }
    }
}
