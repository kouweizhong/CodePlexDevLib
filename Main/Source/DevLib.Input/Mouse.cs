﻿//-----------------------------------------------------------------------
// <copyright file="Mouse.cs" company="YuGuan Corporation">
//     Copyright (c) YuGuan Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace DevLib.Input
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using DevLib.Input.NativeAPI;

    /// <summary>
    /// Exposes a simple interface to common mouse operations, allowing the user to simulate mouse input.
    /// </summary>
    /// <example>The following code moves to screen coordinate 100,100 and left clicks.
    /// <code>
    /// Mouse.MoveTo(new Point(100, 100));
    /// Mouse.Click(MouseButton.Left);
    /// </code>
    /// </example>
    public static class Mouse
    {
        /// <summary>
        /// Clicks a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        public static void Click(MouseButton mouseButton)
        {
            Down(mouseButton);
            Up(mouseButton);
        }

        /// <summary>
        /// Double-clicks a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        public static void DoubleClick(MouseButton mouseButton)
        {
            Click(mouseButton);
            Click(mouseButton);
        }

        /// <summary>
        /// Performs a mouse-down operation for a specified mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        public static void Down(MouseButton mouseButton)
        {
            int additionalData;
            SendMouseInputFlags inputFlags = GetInputFlags(mouseButton, false, out additionalData);
            SendMouseInput(0, 0, additionalData, inputFlags);
        }

        /// <summary>
        /// Performs a mouse-up operation for a specified mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        public static void Up(MouseButton mouseButton)
        {
            int additionalData;
            SendMouseInputFlags inputFlags = GetInputFlags(mouseButton, true, out additionalData);
            SendMouseInput(0, 0, additionalData, inputFlags);
        }

        /// <summary>
        /// Simulates scrolling of the mouse wheel up or down.
        /// </summary>
        /// <param name="lines">The number of lines to scroll. Use positive numbers to scroll up and negative numbers to scroll down.</param>
        public static void Scroll(double lines)
        {
            int amount = (int)(NativeMethods.WheelDelta * lines);
            SendMouseInput(0, 0, amount, SendMouseInputFlags.Wheel);
        }

        /// <summary>
        /// Moves the mouse pointer to the specified screen coordinates.
        /// </summary>
        /// <param name="point">The screen coordinates to move to.</param>
        public static void MoveTo(Point point)
        {
            SendMouseInput(point.X, point.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Drags the mouse pointer to the specified screen coordinates, using the specified mouse button.
        /// Dragging involves pressing and holding the mouse button, moving to the specified location, and releasing the mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        /// <param name="point">The screen coordinates to move to.</param>
        public static void DragTo(MouseButton mouseButton, Point point)
        {
            Down(mouseButton);
            MoveTo(point);
            Up(mouseButton);
        }

        /// <summary>
        /// Resets the system mouse to a clean state.
        /// </summary>
        public static void Reset()
        {
            MoveTo(new System.Drawing.Point(0, 0));

            if (GetButtonState(MouseButton.Left) == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, 0, SendMouseInputFlags.LeftUp);
            }

            if (GetButtonState(MouseButton.Middle) == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, 0, SendMouseInputFlags.MiddleUp);
            }

            if (GetButtonState(MouseButton.Right) == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, 0, SendMouseInputFlags.RightUp);
            }

            if (GetButtonState(MouseButton.XButton1) == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, (int)NativeMethods.XBUTTON1, SendMouseInputFlags.XUp);
            }

            if (GetButtonState(MouseButton.XButton2) == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, (int)NativeMethods.XBUTTON2, SendMouseInputFlags.XUp);
            }
        }

        /// <summary>
        /// Sends mouse input.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="data">Scroll wheel amount.</param>
        /// <param name="flags">SendMouseInputFlags flags.</param>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        private static void SendMouseInput(int x, int y, int data, SendMouseInputFlags flags)
        {
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.Demand();

            uint intflags = (uint)flags;

            if ((intflags & (int)SendMouseInputFlags.Absolute) != 0)
            {
                // Absolute position requires normalized coordinates.
                NormalizeCoordinates(ref x, ref y);
                intflags |= NativeMethods.MouseeventfVirtualdesk;
            }

            INPUT mouseInput = new INPUT();
            mouseInput.Type = NativeMethods.INPUT_MOUSE;
            mouseInput.Data.Mouse.dx = x;
            mouseInput.Data.Mouse.dy = y;
            mouseInput.Data.Mouse.mouseData = data;
            mouseInput.Data.Mouse.dwFlags = intflags;
            mouseInput.Data.Mouse.time = 0;
            mouseInput.Data.Mouse.dwExtraInfo = new IntPtr(0);

            if (NativeMethods.SendInput(1, new INPUT[] { mouseInput }, Marshal.SizeOf(mouseInput)) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Normalize coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        private static void NormalizeCoordinates(ref int x, ref int y)
        {
            int vScreenWidth = NativeMethods.GetSystemMetrics(NativeMethods.SMCxvirtualscreen);
            int vScreenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SMCyvirtualscreen);
            int vScreenLeft = NativeMethods.GetSystemMetrics(NativeMethods.SMXvirtualscreen);
            int vScreenTop = NativeMethods.GetSystemMetrics(NativeMethods.SMYvirtualscreen);

            // Absolute input requires that input is in 'normalized' coords - with the entire
            // desktop being (0,0)...(65536,65536). Need to convert input x,y coords to this
            // first.
            //
            // In this normalized world, any pixel on the screen corresponds to a block of values
            // of normalized coords - eg. on a 1024x768 screen,
            // y pixel 0 corresponds to range 0 to 85.333,
            // y pixel 1 corresponds to range 85.333 to 170.666,
            // y pixel 2 correpsonds to range 170.666 to 256 - and so on.
            // Doing basic scaling math - (x-top)*65536/Width - gets us the start of the range.
            // However, because int math is used, this can end up being rounded into the wrong
            // pixel. For example, if we wanted pixel 1, we'd get 85.333, but that comes out as
            // 85 as an int, which falls into pixel 0's range - and that's where the pointer goes.
            // To avoid this, we add on half-a-"screen pixel"'s worth of normalized coords - to
            // push us into the middle of any given pixel's range - that's the 65536/(Width*2)
            // part of the formula. So now pixel 1 maps to 85+42 = 127 - which is comfortably
            // in the middle of that pixel's block.
            // The key ting here is that unlike points in coordinate geometry, pixels take up
            // space, so are often better treated like rectangles - and if you want to target
            // a particular pixel, target its rectangle's midpoint, not its edge.
            x = (((x - vScreenLeft) * 65536) / vScreenWidth) + (65536 / (vScreenWidth * 2));
            y = (((y - vScreenTop) * 65536) / vScreenHeight) + (65536 / (vScreenHeight * 2));
        }

        /// <summary>
        /// Gets input flags.
        /// </summary>
        /// <param name="mouseButton">The mouse button to check.</param>
        /// <param name="isUp">Whether is up.</param>
        /// <param name="additionalData">Additional data.</param>
        /// <returns>SendMouseInputFlags instance.</returns>
        private static SendMouseInputFlags GetInputFlags(MouseButton mouseButton, bool isUp, out int additionalData)
        {
            SendMouseInputFlags flags;
            additionalData = 0;

            if (mouseButton == MouseButton.Left && isUp)
            {
                flags = SendMouseInputFlags.LeftUp;
            }
            else if (mouseButton == MouseButton.Left && !isUp)
            {
                flags = SendMouseInputFlags.LeftDown;
            }
            else if (mouseButton == MouseButton.Right && isUp)
            {
                flags = SendMouseInputFlags.RightUp;
            }
            else if (mouseButton == MouseButton.Right && !isUp)
            {
                flags = SendMouseInputFlags.RightDown;
            }
            else if (mouseButton == MouseButton.Middle && isUp)
            {
                flags = SendMouseInputFlags.MiddleUp;
            }
            else if (mouseButton == MouseButton.Middle && !isUp)
            {
                flags = SendMouseInputFlags.MiddleDown;
            }
            else if (mouseButton == MouseButton.XButton1 && isUp)
            {
                flags = SendMouseInputFlags.XUp;
                additionalData = (int)NativeMethods.XBUTTON1;
            }
            else if (mouseButton == MouseButton.XButton1 && !isUp)
            {
                flags = SendMouseInputFlags.XDown;
                additionalData = (int)NativeMethods.XBUTTON1;
            }
            else if (mouseButton == MouseButton.XButton2 && isUp)
            {
                flags = SendMouseInputFlags.XUp;
                additionalData = (int)NativeMethods.XBUTTON2;
            }
            else if (mouseButton == MouseButton.XButton2 && !isUp)
            {
                flags = SendMouseInputFlags.XDown;
                additionalData = (int)NativeMethods.XBUTTON2;
            }
            else
            {
                throw new InvalidOperationException();
            }

            return flags;
        }

        /// <summary>
        /// Get button state.
        /// </summary>
        /// <param name="mouseButton">The mouse button to check.</param>
        /// <returns>MouseButtonState instance.</returns>
        private static MouseButtonState GetButtonState(MouseButton mouseButton)
        {
            MouseButtonState mouseButtonState = MouseButtonState.Released;

            int virtualKeyCode = 0;

            switch (mouseButton)
            {
                case MouseButton.Left:
                    virtualKeyCode = NativeMethods.VK_LBUTTON;
                    break;

                case MouseButton.Right:
                    virtualKeyCode = NativeMethods.VK_RBUTTON;
                    break;

                case MouseButton.Middle:
                    virtualKeyCode = NativeMethods.VK_MBUTTON;
                    break;

                case MouseButton.XButton1:
                    virtualKeyCode = NativeMethods.VK_XBUTTON1;
                    break;

                case MouseButton.XButton2:
                    virtualKeyCode = NativeMethods.VK_XBUTTON2;
                    break;
            }

            mouseButtonState = (NativeMethods.GetKeyState(virtualKeyCode) & 0x8000) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released;

            return mouseButtonState;
        }
    }
}
