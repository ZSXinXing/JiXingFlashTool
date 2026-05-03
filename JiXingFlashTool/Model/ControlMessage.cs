using JiXingFlashTool.Enums;
using JXAdbCore.Enums;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public enum ControlMessageType : byte
    {
        InjectKeycode,
        InjectText,
        InjectTouchEvent,
        InjectScrollEvent,
        BackOrScreenOn,
        ExpandNotificationPanel,
        ExpandSettingsPanel,
        CollapsePanels,
        GetClipboard,
        SetClipboard,
        SetScreenPowerMode,
        RotateDevice,
    }

    public class ScreenSize
    {
        public ushort Width;
        public ushort Height;
    }

    public class Position
    {
        public ScreenSize ScreenSize = new ScreenSize();
        public Point Point = new Point();
        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[12];
            BinaryPrimitives.WriteInt32BigEndian(b[0..], Point.X);
            BinaryPrimitives.WriteInt32BigEndian(b[4..], Point.Y);
            BinaryPrimitives.WriteUInt16BigEndian(b[8..], ScreenSize.Width);
            BinaryPrimitives.WriteUInt16BigEndian(b[10..], ScreenSize.Height);
            return b;
        }
    }

    public interface IControlMessage
    {
        public ControlMessageType Type { get; }
        public int size { get; }
        byte[] bytes();
    }

    public class KeycodeControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectKeycode;
        public AndroidKeyEventAction Action { get; set; }
        public JiXingFlashTool.Enums.AndroidKeycode KeyCode { get; set; }
        public uint Repeat { get; set; }
        public AndroidMetastate Metastate { get; set; }

        public int size => 14;

        public byte[] bytes()
        {
            Span<byte> b = new byte[14];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            BinaryPrimitives.WriteInt32BigEndian(b[2..], (int)KeyCode);
            BinaryPrimitives.WriteInt32BigEndian(b[6..], (int)Repeat);
            BinaryPrimitives.WriteInt32BigEndian(b[10..], (int)Metastate);
            return b.ToArray();
        }

    }

    public class BackOrScreenOnControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.BackOrScreenOn;
        public int size { get; }
        public AndroidKeyEventAction Action { get; set; }

        public byte[] bytes()
        {
            Span<byte> b = new byte[2];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            return b.ToArray();
        }
    }

    public class TouchEventControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectTouchEvent;
        public int size => 28;
        public AndroidMotionEventAction Action { get; set; }
        public AndroidMotionEventButtons Buttons { get; set; } = AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_PRIMARY;
        public ulong PointerId { get; set; } = 0xFFFFFFFFFFFFFFFF;
        public Position Position { get; set; } = new Position();

        public byte[] bytes()
        {
            Span<byte> b = new byte[28];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            BinaryPrimitives.WriteUInt64BigEndian(b[2..], PointerId);

            // Position
            BinaryPrimitives.WriteInt32BigEndian(b[10..], Position.Point.X);
            BinaryPrimitives.WriteInt32BigEndian(b[14..], Position.Point.Y);
            BinaryPrimitives.WriteUInt16BigEndian(b[18..], Position.ScreenSize.Width);
            BinaryPrimitives.WriteUInt16BigEndian(b[20..], Position.ScreenSize.Height);

            // TODO: Pressure
            b[22] = 0xFF;
            b[23] = 0xFF;

            BinaryPrimitives.WriteInt32BigEndian(b[24..], (int)Buttons);

            return b.ToArray();
        }
    }

    public class ScrollEventControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectScrollEvent;
        public AndroidMotionEventButtons Buttons { get; set; } = AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_PRIMARY;
        public int size => 25;

        public Position Position { get; set; } = new Position();
        public int HorizontalScroll { get; set; }
        public int VerticalScroll { get; set; }
        public byte[] bytes()
        {
            Span<byte> b = new byte[25];
            b[0] = (byte)Type;
            Position.ToBytes().CopyTo(b[1..]);
            BinaryPrimitives.WriteInt32BigEndian(b[13..], HorizontalScroll);
            BinaryPrimitives.WriteInt32BigEndian(b[17..], VerticalScroll);
            BinaryPrimitives.WriteInt32BigEndian(b[21..], (int)Buttons);
            return b.ToArray();
        }
    }

    public class TextControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectText;

        public string Text = "1";
        public int size => 2;

        public byte[] bytes()
        {
            byte[] textByte = System.Text.Encoding.GetEncoding("gb2312").GetBytes(Text);
            byte[] b = new byte[1 + textByte.Length];
            b[0] = (byte)Type;
            textByte.CopyTo(b, 1);
            return b.ToArray();
        }
    }

}
