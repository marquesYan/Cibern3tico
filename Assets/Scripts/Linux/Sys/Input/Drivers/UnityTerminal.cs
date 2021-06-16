using System.Text;
using System.Collections;
using UnityEngine;
using Linux.PseudoTerminal;
using Linux.Devices.Input;

namespace Linux
{
    public class UnityTerminal : VirtualTerminal
    {
        Rect _window;
        TextEditor _textEditor;

        float _currentOpenTerm;
        float _openTarget;
        Vector2 _scrollPosition;
        Event _lastEvent;
        protected string InputBuffer;
        protected Vector2 InputBufferSize;
        public float MaxHeight = 1f;
        public int ToggleSpeed = 360;
        public Font ConsoleFont;
        public GUIStyle LabelStyle;
        public GUIStyle InputStyle;
        public GUIStyle CursorStyle;
        public GUIStyle WindowStyle;
        public Color BackgroundColor = Color.black;
        public Color ForegroundColor    = Color.white;
        public Color ShellColor         = Color.white;
        public Color InputColor         = Color.cyan;
        public Color WarningColor       = Color.yellow;
        public Color ErrorColor         = Color.red;
        public float InputContrast = 1f;
        public float RealWindowSize { get; protected set; }

        public UnityTerminal(
            int bufferSize
        ) : base(bufferSize) {
            Initialize();
        }

        protected override void MoveYAxis(int position) {
            _scrollPosition.y = position;
        }

        protected override void MoveXAxis(int position) {
            _scrollPosition.x = position;
        }

        protected override void DrawLine(string message) {
            // LabelStyle.normal.textColor = GetLogColor(log.type);
            GUILayout.Label(message, LabelStyle);
        }

        public void MoveCursorToEnd() {
            if (_textEditor == null) {
                _textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            }

            _textEditor.MoveCursorToPosition(new Vector2(999, 999));
        }

        protected override float GetScreenWidth() {
            return Screen.width;
        }

        protected override void HandleDraw() {
            HandleOpenness();
            _window = GUILayout.Window(88, _window, DrawConsole, "", WindowStyle);
        }

        protected override Vector CalcSize(string message) {
            Vector2 vector = LabelStyle.CalcSize(new GUIContent(message));
            return new Vector(vector.x, vector.y);
        }

        void Initialize() {
            if (ConsoleFont == null) {
                ConsoleFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
                Debug.LogWarning("Command Console Warning: Please assign a font.");
            }

            SetupWindow();
            SetupInput();
            SetupLabels();

            RealWindowSize = Screen.height * MaxHeight;
            _openTarget = RealWindowSize;
        }

        void SetupWindow() {
            RealWindowSize = Screen.height * MaxHeight / 3;
            _window = new Rect(0, _currentOpenTerm - RealWindowSize, Screen.width, RealWindowSize);

            // Set background color
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, BackgroundColor);
            backgroundTexture.Apply();

            WindowStyle = new GUIStyle();
            WindowStyle.normal.background = backgroundTexture;
            WindowStyle.padding = new RectOffset(10, 10, 10, 10);
            WindowStyle.normal.textColor = ForegroundColor;
            WindowStyle.font = ConsoleFont;
        }

        void SetupLabels() {
            LabelStyle = new GUIStyle();
            LabelStyle.padding = new RectOffset(0, 0, 0, 0);
            LabelStyle.font = ConsoleFont;
            LabelStyle.normal.textColor = ForegroundColor;
            LabelStyle.wordWrap = true;
        }

        void SetupInput() {
            InputStyle = new GUIStyle();
            InputStyle.padding = new RectOffset(20, 20, 20, 20);
            InputStyle.font = ConsoleFont;
            InputStyle.fixedHeight = ConsoleFont.fontSize * 1.6f;
            InputStyle.normal.textColor = InputColor;

            var darkBackground = new Color();
            darkBackground.r = BackgroundColor.r - InputContrast;
            darkBackground.g = BackgroundColor.g - InputContrast;
            darkBackground.b = BackgroundColor.b - InputContrast;
            darkBackground.a = 0.5f;

            Texture2D inputBackgroundTexture = new Texture2D(1, 1);
            inputBackgroundTexture.SetPixel(0, 0, darkBackground);
            inputBackgroundTexture.Apply();
            InputStyle.normal.background = inputBackgroundTexture;
        }

        void DrawConsole(int windowID) {
            _lastEvent = Event.current;

            GUILayout.BeginVertical();

            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition, 
                false, 
                false,
                GUIStyle.none,
                GUIStyle.none
            );

            DrawTerm();

            if (CursorSize != null) {
                Vector2 cursorPos = LabelStyle.GetCursorPixelPosition(
                    new Rect(0, 0, 0, 0),
                    new GUIContent(CursorLinesMngr.CurrentLine),
                    CursorLinesMngr.Cursor
                );

                LabelStyle.DrawCursor(
                    new Rect(cursorPos.x, (CursorSize.Y * CursorLinesMngr.LineIndex) - 20, 4, 4),
                    new GUIContent("|"), 0, 0
                );
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        void HandleOpenness() {
            float dt = ToggleSpeed * Time.deltaTime;

            if (_currentOpenTerm < _openTarget) {
                _currentOpenTerm += dt;
                if (_currentOpenTerm > _openTarget) _currentOpenTerm = _openTarget;
            } else if (_currentOpenTerm > _openTarget) {
                _currentOpenTerm -= dt;
                if (_currentOpenTerm < _openTarget) _currentOpenTerm = _openTarget;
            } else {
                return; // Already at target
            }

            _window = new Rect(0, _currentOpenTerm - RealWindowSize, Screen.width, RealWindowSize);
        }
    }
}
