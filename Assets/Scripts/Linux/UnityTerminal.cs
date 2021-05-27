using System.Text;
using System.Collections;
using UnityEngine;
using Linux.PseudoTerminal;

namespace Linux
{
    public class UnityTerminal : VirtualTerminal
    {
        Rect _window;
        TextEditor _textEditor;
        protected string InputBuffer;
        protected Vector2 InputBufferSize;

        float _currentOpenTerm;
        float _openTarget;
        Vector2 _scrollPosition;
        public float MaxHeight = 1f;
        public int ToggleSpeed = 360;
        public Font ConsoleFont;
        public GUIStyle LabelStyle;
        public GUIStyle InputStyle;
        public GUIStyle WindowStyle;
        public Color BackgroundColor = Color.black;
        public Color ForegroundColor    = Color.white;
        public Color ShellColor         = Color.white;
        public Color InputColor         = Color.cyan;
        public Color WarningColor       = Color.yellow;
        public Color ErrorColor         = Color.red;
        public float InputContrast = 1f;
        public float RealWindowSize { get; protected set; }

        public UnityTerminal(int bufferSize) : base(bufferSize) {
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

        protected override void OnEventRecv()
        {   
            base.OnEventRecv();
            // if (Event.current.Equals(Event.KeyboardEvent("return"))) {
            //     EnterCommand();
            // } else if (Event.current.Equals(Event.KeyboardEvent("up"))) {
            //     command_text = History.Previous();
            //     move_cursor = true;
            // } else if (Event.current.Equals(Event.KeyboardEvent("down"))) {
            //     command_text = History.Next();
            // } else if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey))) {
            //     ToggleState(TerminalState.OpenSmall);
            // } else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey))) {
            //     ToggleState(TerminalState.OpenFull);
            // } else if (Event.current.Equals(Event.KeyboardEvent("tab"))) {
            //     CompleteCommand();
            //     move_cursor = true; // Wait till next draw call
            // }
        }

        protected override bool HasReturnEvent()
        {
            return ((Event)LastEvent).Equals(Event.KeyboardEvent("return"));
        }

        public override void MoveCursorToEnd() {
            if (_textEditor == null) {
                _textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            }

            _textEditor.MoveCursorToPosition(new Vector2(999, 999));
        }

        void FocusTextField()
        {
            GUI.FocusControl("command_text_field");
        }

        public void Input(string label, System.Action<string> onRead) {
            InputBuffer = label;
            InputBufferSize = InputStyle.CalcSize(new GUIContent(label));

            System.Action<string> InputReadWrapper = textInput => {
                onRead(LastTextInput);
                LastTextInput = "";
            };

            SubscribeRead(InputReadWrapper);
        }

        public void ClearInput() {
            InputBuffer = null;
        }

        void Initialize() {
            if (ConsoleFont == null) {
                ConsoleFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
                Debug.LogWarning("Command Console Warning: Please assign a font.");
            }

            LastTextInput = "";

            SetupWindow();
            SetupInput();
            SetupLabels();

            SubscribeFirstDraw(FocusTextField);

            RealWindowSize = Screen.height * MaxHeight;
            _openTarget = RealWindowSize;
        }

        public void OnGUI() {
            HandleOpenness();
            _window = GUILayout.Window(88, _window, DrawConsole, "", WindowStyle);
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
            WindowStyle.padding = new RectOffset(4, 4, 4, 4);
            WindowStyle.normal.textColor = ForegroundColor;
            WindowStyle.font = ConsoleFont;
        }

        void SetupLabels() {
            LabelStyle = new GUIStyle();
            LabelStyle.font = ConsoleFont;
            LabelStyle.normal.textColor = ForegroundColor;
            LabelStyle.wordWrap = true;
        }

        void SetupInput() {
            InputStyle = new GUIStyle();
            InputStyle.padding = new RectOffset(4, 4, 4, 4);
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
            GUILayout.BeginVertical();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUIStyle.none, GUIStyle.none);
            GUILayout.FlexibleSpace();
            DrawTerm();
            GUILayout.EndScrollView();

            LastEvent = Event.current;

            GUILayout.BeginHorizontal();

            if (InputBuffer != null) {
                GUILayout.Label(InputBuffer, InputStyle, GUILayout.Width(InputBufferSize.x));
            }

            GUI.SetNextControlName("command_text_field");
            LastTextInput = GUILayout.TextField(LastTextInput, InputStyle);

            // if (input_fix && command_text.Length > 0) {
            //     command_text = cached_command_text; // Otherwise the TextField picks up the ToggleHotkey character event
            //     input_fix = false;                  // Prevents checking string Length every draw call
            // }

            // if (ShowGUIButtons && GUILayout.Button("| run", InputStyle, GUILayout.Width(Screen.width / 10))) {
            //     EnterCommand();
            // }

            GUILayout.EndHorizontal();
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

        // protected virtual void EnterCommand() {
        //     Log(TerminalLogType.Input, "{0}", command_text);
        //     Shell.RunCommand(command_text);
        //     History.Push(command_text);

        //     if (IssuedError) {
        //         Log(TerminalLogType.Error, "Error: {0}", Shell.IssuedErrorMessage);
        //     }

        //     command_text = "";
        //     ScrollAllDown();
        // }

        // void CompleteCommand() {
        //     string head_text = command_text;
        //     string[] completion_buffer = Autocomplete.Complete(ref head_text);
        //     int completion_length = completion_buffer.Length;

        //     if (completion_length == 1) {
        //         command_text = head_text + completion_buffer[0];
        //     } else if (completion_length > 1) {
        //         // Print possible completions
        //         Log(string.Join("    ", completion_buffer));
        //         ScrollAllDown();
        //     }
        // }

        // Color GetLogColor(TerminalLogType type) {
        //     switch (type) {
        //         case TerminalLogType.Message: return ForegroundColor;
        //         case TerminalLogType.Warning: return WarningColor;
        //         case TerminalLogType.Input: return InputColor;
        //         case TerminalLogType.ShellMessage: return ShellColor;
        //         default: return ErrorColor;
        //     }
        // }
    }
}
