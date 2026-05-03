using JiXingFlashTool.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JiXingFlashTool.Utils
{
    public class KeycodeHelper
    {

        private static Dictionary<Key, AndroidKeycode> InputKeyAndroidKeycodeMap = new Dictionary<Key, AndroidKeycode>()
            {
                { Key.Oem1,AndroidKeycode.AKEYCODE_SEMICOLON},
                { Key.OemComma,AndroidKeycode.AKEYCODE_COMMA},
                { Key.OemPeriod,AndroidKeycode.AKEYCODE_PERIOD},
                { Key.OemQuestion,AndroidKeycode.AKEYCODE_NUMPAD_DIVIDE},
                { Key.OemOpenBrackets,AndroidKeycode.AKEYCODE_LEFT_BRACKET},
                { Key.Oem5,AndroidKeycode.AKEYCODE_BACKSLASH},
                { Key.Oem6,AndroidKeycode.AKEYCODE_RIGHT_BRACKET},
                { Key.OemPlus,AndroidKeycode.AKEYCODE_EQUALS},
                { Key.OemMinus,AndroidKeycode.AKEYCODE_MINUS},
                { Key.OemQuotes,AndroidKeycode.AKEYCODE_APOSTROPHE}
        };

        private static readonly Dictionary<Key, AndroidKeycode> keycodeDict = new Dictionary<Key, AndroidKeycode>
        {
            { Key.Space, AndroidKeycode.AKEYCODE_SPACE },
            { Key.Back, AndroidKeycode.AKEYCODE_DEL },
            { Key.Left, AndroidKeycode.AKEYCODE_DPAD_LEFT },
            { Key.Up, AndroidKeycode.AKEYCODE_DPAD_UP },
            { Key.Right, AndroidKeycode.AKEYCODE_DPAD_RIGHT },
            { Key.Down, AndroidKeycode.AKEYCODE_DPAD_DOWN },
            { Key.Delete, AndroidKeycode.AKEYCODE_FORWARD_DEL },
            { Key.Tab, AndroidKeycode.AKEYCODE_TAB },
            { Key.Enter, AndroidKeycode.AKEYCODE_ENTER },
        };

        public static AndroidKeycode ConvertKey(Key key)
        {
            AndroidKeycode androidKeycode = AndroidKeycode.AKEYCODE_UNKNOWN;
            // 字母 A - Z
            if (key >= Key.A && key <= Key.Z)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_A - (int)Key.A;
                androidKeycode = (AndroidKeycode)((int)key + offset);
            }
            // 数字 0-9
            else if (key >= Key.D0 && key <= Key.D9)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_0 - (int)Key.D0;
                androidKeycode = (AndroidKeycode)((int)key + offset);
            }
            //小数字键盘
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_0 - (int)Key.NumPad0;
                androidKeycode = (AndroidKeycode)((int)key + offset);
            }
            //符合
            else if (key >= Key.Oem1 && key <= Key.ImeProcessed)
            {
                InputKeyAndroidKeycodeMap.TryGetValue(key, out var androidKey);
                androidKeycode = androidKey;
            }
            else if (key == Key.Decimal)
            {
                androidKeycode = AndroidKeycode.AKEYCODE_PERIOD;
            }
            else if (key == Key.Divide)
            {
                androidKeycode = AndroidKeycode.AKEYCODE_NUMPAD_DIVIDE;
            }
            else if (key == Key.Multiply)
            {
                androidKeycode = AndroidKeycode.AKEYCODE_NUMPAD_MULTIPLY;
            }
            else if (key == Key.Delete)
            {
                androidKeycode = AndroidKeycode.AKEYCODE_DEL;
            }
            //其它可以适配的
            else if (keycodeDict.TryGetValue(key, out var androidKey))
            {
                androidKeycode = androidKey;
            }

            return androidKeycode;
        }

        public static AndroidMetastate ConvertModifiers(ModifierKeys keyModifiers)
        {
            AndroidMetastate metastate = AndroidMetastate.AMETA_NONE;

            if (keyModifiers.HasFlag(ModifierKeys.Shift))
                metastate |= AndroidMetastate.AMETA_SHIFT_ON;

            if (keyModifiers.HasFlag(ModifierKeys.Control))
                metastate |= AndroidMetastate.AMETA_CTRL_ON;

            if (keyModifiers.HasFlag(ModifierKeys.Alt))
                metastate |= AndroidMetastate.AMETA_ALT_ON;

            return metastate;
        }


        private static AndroidKeycode SymbolInputKeyToAndroidKeycode(Key key)
        {
            AndroidKeycode androidKeycode = AndroidKeycode.AKEYCODE_UNKNOWN;

            if (InputKeyAndroidKeycodeMap.ContainsKey(key))
                androidKeycode = InputKeyAndroidKeycodeMap[key];

            return androidKeycode;
        }
    }
}
