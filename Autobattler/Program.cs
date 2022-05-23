using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoBattler
{
    class Program
    {
        public object Graphics { get; private set; }

        [DllImport("user32")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr window);
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr dc, int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr window, IntPtr dc);

        [DllImportAttribute("User32.dll")]
        private static extern IntPtr SetForegroundWindow(int hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private static int _tolerance = 15;
        private static int _pollWait = 10;
        private static float _pixelXPercent = 0.93f;
        private static float _pixelYPercent = 0.93f;

        private static IntPtr _handle;

        private static bool _runAway = false;

        private static bool _useGameAutoBattle = true;

        private static bool _useQuickSave = true;
        private static int _quickSaveTimer = 60000; // 1 minute

        private static DateTime _lastSave = DateTime.Now;

        private static bool _verbose = false;

        static void Main(string[] args)
        {
            Log("BEGIN AUTO BATTLER");
            bool pause = false;

            if(_useGameAutoBattle)
            {
                Log("Using PRESS Q (game autobattle prior commands) mode");
            }
            else
            {
                Log("Using SPAM ENTER mode");
            }

            if (_useQuickSave)
            {
                //Log($"Quick-saving every {_quickSaveTimer / 60000} minute(s)");
                Log("Quick-saving after every battle");
            }
            else
            {
                Log("Quick-saving disabled");
            }

            Log("Death/main-menu detection active");

            Color battleMenu = Color.FromArgb(20, 20, 140);

            string windowName = "FINAL FANTASY";

            if (FindWindow(windowName, out _handle))
            {
                SetForegroundWindow((int)_handle);

                bool state = CheckCombatState(battleMenu, FindPixel());
                bool death = CheckDeath(FindPixel());

                if(death)
                {
                    Log("Don't start at main menu. Start on field!");
                    return; // abort
                }
                
                Log(state);
                SpamEnter();

                bool hasEngagedGameAutoBattle = false;

                QuickSave();

                bool running = true;

                while (running)
                {
                    if(Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;
                        Console.WriteLine(key.ToString());
                    
                        SetForegroundWindow((int)_handle);

                        switch(key)
                        {
                            default:
                            case ConsoleKey.Spacebar:
                                {
                                    pause = !pause;
                                    Log(pause ? "Pausing" : "Un-pausing");
                                    break;
                                }
                            //case ConsoleKey.Delete:
                            //case ConsoleKey.Enter:
                            //case ConsoleKey.R:
                            //    {
                            //        _runAway = true;
                            //        Log("Running away");
                            //        break;
                            //    }
                        }
                    }

                    if (pause)
                    {

                    }
                    else
                    {
                        Color pixel = FindPixel();

                        if (CheckDeath(pixel))
                        {
                            Log("Uh oh, this looks like main menu. Aborting");
                            running = false;
                            return;
                        }

                        if(_verbose)
                        {
                            string combatMessage = CheckCombatState(battleMenu, pixel) ? "in combat" : "out of combat";
                            Log($"{pixel.ToString()} - {combatMessage}");
                        }

                        if (CheckCombatState(battleMenu, pixel) != state)
                        {
                            state = StateTransition(state);
                            Log(state);
                        }

                        // in combat
                        if (state)
                        {
                            if (_useGameAutoBattle)
                            {
                                if(!hasEngagedGameAutoBattle)
                                {
                                    hasEngagedGameAutoBattle = true;
                                    Send("{q}");
                                }

                            }
                            else
                            {
                                Send("{ENTER}");
                                Thread.Sleep(10);
                            }
                        }
                        // out of combat
                        else
                        {
                            hasEngagedGameAutoBattle = false;

                            //CheckQuickSaveTimer();

                            Send("{DOWN}");
                            Send("{DOWN}");
                            Send("{UP}");
                            Send("{UP}");
                            Send("{ENTER}");
                        }

                        Thread.Sleep(_pollWait);
                        //Log(DateTime.Now.ToString());
                    }
                }
            }
        }

        //private static void CheckQuickSaveTimer()
        //{
        //    if(!_useQuickSave)
        //    {
        //        return;
        //    }

        //    if((DateTime.Now - _lastSave).TotalMilliseconds > _quickSaveTimer )
        //    {
        //        QuickSave();
        //    }
        //}

        private static void QuickSave()
        {
            Send("{TAB}");
            Thread.Sleep(500);
            Send("{UP}");
            Send("{UP}");
            Send("{UP}");
            Send("{ENTER}");
            Thread.Sleep(500);
            Send("{LEFT}");
            Send("{ENTER}");
            Thread.Sleep(500);
            Send("{BACKSPACE}");
            Send("{BACKSPACE}");
                
            _lastSave = DateTime.Now;
            Log($"Quicksave at: {_lastSave}");
        }

        private static bool StateTransition(bool state)
        {
            // if in combat transitioning to out of combat
            if (state)
            {
                // do some extra enter keys
                Console.WriteLine("Finishing combat");
                Thread.Sleep(4000);
                SpamEnter();
                Thread.Sleep(1000);
                QuickSave();
            }
            else
            {
                Console.WriteLine("Entering combat");
                // do nothing
            }


            return !state;
        }

        private static void SpamEnter(int amount = 10)
        {
            Spam("{ENTER}");
        }

        private static void Spam(string x, int amount = 10)
        {
            int i = 0;
            while (i < amount)
            {
                Send(x);
                Thread.Sleep(10);
                i++;
            }
        }

        private static void Send(string key)
        {
            if(_verbose)
            {
                Log($"  {key}");
            }
            SendKeys.SendWait(key);
        }

        private static void Log(bool inBattle)
        {
            Log(inBattle ? "In combat" : "Out of combat");
        }
        private static void Log(string value)
        {
            Console.WriteLine(value);
        }

        private static bool CheckCombatState(Color battleMenu, Color screenColor)
        {
            return
                Math.Abs(battleMenu.R - screenColor.R) < _tolerance &&
                Math.Abs(battleMenu.G - screenColor.G) < _tolerance &&
                Math.Abs(battleMenu.B - screenColor.B) < _tolerance;
        }

        private static bool CheckDeath(Color screencolor)
        {
            //  main menu?
            return
                Math.Abs(screencolor.R - 255) < _tolerance &&
                Math.Abs(screencolor.G - 255) < _tolerance &&
                Math.Abs(screencolor.B - 255) < _tolerance;
        }

        private static bool FindWindow(string windowName, out IntPtr handle)
        {
            foreach (Process pList in Process.GetProcesses())
            {
                if (pList.MainWindowTitle.ToUpper().StartsWith(windowName))
                {
                    handle = pList.MainWindowHandle;

                    Console.WriteLine($"Found: {pList.MainWindowTitle}");
                    Console.WriteLine($"Handle: {handle}");

                    return true;
                }
            }
            handle = IntPtr.Zero;
            return false;
        }

        public class Location
        {
            public int x { get; set; }
            public int y { get; set; } 
        }

        private static Rect GetWindowLocation(IntPtr  handle)
        {
            Rect result = new Rect();
            GetWindowRect(handle, ref result);


            return result;
        }

        private static Color FindPixel()
        {
            Rect rect = GetWindowLocation(_handle);

            int width = rect.Bottom - rect.Top;
            int height = rect.Right - rect.Left;
            
            int xPosition = (int)Math.Floor(rect.Right * _pixelXPercent);
            int yPosition = (int)Math.Floor(rect.Bottom * _pixelYPercent);
            
            Color color = GetColorAt(xPosition, yPosition);

            //Console.WriteLine($"BottomRight: ({rect.Right},{rect.Bottom}) | Check: ({xPosition},{yPosition}) | Color {color}");
            //Size: ({height},{width}) | 

            return color;
        }

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            //IntPtr dc = _handle;

            //Console.WriteLine(desk);
            //Console.WriteLine(dc);

            int a = (int)GetPixel(dc, x, y);
            //ReleaseDC(desk, dc);
            ReleaseDC(dc, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }
    }
}
