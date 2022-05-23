using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoBattler
{
    class Program
    {
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

        /// <summary>
        /// rectangle for window
        /// </summary>
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        /// <summary>
        /// tolerance of color RGB value
        /// </summary>
        private static int _toleranceColor = 15;

        /// <summary>
        /// wait time between repetative keystrokes (like {ENTER})
        /// </summary>
        private static int _pollWait = 10;

        /// <summary>
        /// battle menu screen location - x percent
        /// </summary>
        private static float _battleMenuPixelXPercent = 0.93f;

        /// <summary>
        /// battle menu screen location - y percent
        /// </summary>
        private static float _battleMenuPixelYPercent = 0.93f;

        /// <summary>
        /// game window handle
        /// </summary>
        private static IntPtr _gameHandle;

        /// <summary>
        /// use auto battle with 'Q' button - this uses last commands setup by the player
        /// when false, spams enter
        /// </summary>
        private static bool _useGameAutoBattle = true;

        /// <summary>
        /// quick save after every battle
        /// </summary>
        private static bool _quickSaveAfterEveryBattle = true;

        /// <summary>
        /// last quick save event
        /// </summary>
        private static DateTime _lastSave = DateTime.Now;

        /// <summary>
        /// if verbose, print out every key press and color
        /// </summary>
        private static bool _verbose = false;
            
        /// <summary>
        /// BLUE color of battle menu
        /// </summary>
        private static Color _battleMenu = Color.FromArgb(20, 20, 140);
            
        /// <summary>
        /// name window starts with
        /// </summary>
        private static string _windowName = "FINAL FANTASY";

        /// <summary>
        /// main - auto auto-battle
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Log("BEGIN AUTO AUTO-BATTLER");
            bool pause = false;

            // using auto-battle mode or enter spam mode
            if(_useGameAutoBattle)
            {
                Log("Using PRESS Q (game autobattle prior commands) mode");
            }
            else
            {
                Log("Using SPAM ENTER mode");
            }

            // using quick save or not
            if (_quickSaveAfterEveryBattle)
            {
                Log("Quick-saving after every battle");
            }
            else
            {
                Log("Quick-saving disabled");
            }

            // always detecting for death / main-menu to abort
            Log("Death/main-menu detection active");

            // find window
            if (FindWindow(_windowName, out _gameHandle))
            {
                // set focus
                SetForegroundWindow((int)_gameHandle);

                // check state
                bool state = CheckCombatState(_battleMenu, FindPixel());
                bool death = CheckDeath(FindPixel());

                // don't start at main menu
                if(death)
                {
                    Log("Don't start at main menu. Start on field!");
                    return; // abort
                }
                
                // begin
                Log(state);
                SpamEnter();

                // hasEngagedGameAutoBattle allows setting auto battle
                //   to occur only once at start of combat
                bool hasEngagedGameAutoBattle = false;

                // quick save (if applicable)
                QuickSave();

                // run
                bool running = true;
                while (running)
                {
                    // if console has focus and key press
                    if(Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;
                        Console.WriteLine(key.ToString());
                    
                        switch(key)
                        {
                            // try to pause (every key
                            default:
                            case ConsoleKey.Spacebar:
                                {
                                    pause = !pause;
                                    Log(pause ? "Pausing" : "Un-pausing");
                                    break;
                                }
                        }
                        
                        // set focus back to window (to avoid key presses into console
                        SetForegroundWindow((int)_gameHandle);
                    }

                    // paused
                    if (pause)
                    {

                    }
                    // not paused
                    else
                    {
                        // get pixel color
                        Color pixel = FindPixel();

                        // check if dead
                        if (CheckDeath(pixel))
                        {
                            Log("Uh oh, this looks like main menu. Aborting");
                            running = false;
                            return;
                        }

                        // not dead yet

                        // vebrose state log
                        if(_verbose)
                        {
                            string combatMessage = CheckCombatState(_battleMenu, pixel) ? "in combat" : "out of combat";
                            Log($"{pixel.ToString()} - {combatMessage}");
                        }

                        // check for state change
                        if (CheckCombatState(_battleMenu, pixel) != state)
                        {
                            state = StateTransitionAction(state);
                            Log(state);
                        }

                        // in combat
                        if (state)
                        {
                            // auto battle mode with 'q'
                            if (_useGameAutoBattle)
                            {
                                if(!hasEngagedGameAutoBattle)
                                {
                                    hasEngagedGameAutoBattle = true;
                                    Send("{q}");
                                }

                            }
                            // enter spam mode
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

                            // movement
                            Send("{DOWN}");
                            Send("{DOWN}");
                            Send("{UP}");
                            Send("{UP}");
                            Send("{ENTER}");
                        }

                        Thread.Sleep(_pollWait);
                    }
                }
            }
        }

        /// <summary>
        /// quick save
        /// </summary>
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

        /// <summary>
        /// state transition action
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool StateTransitionAction(bool state)
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

            // flip sate
            return !state;
        }

        /// <summary>
        /// spam enter
        /// </summary>
        /// <param name="amount"></param>
        private static void SpamEnter(int amount = 10)
        {
            Spam("{ENTER}", amount);
        }

        /// <summary>
        /// spam key press
        /// </summary>
        /// <param name="x"></param>
        /// <param name="amount"></param>
        private static void Spam(string x, int amount = 10)
        {
            int i = 0;
            while (i < amount)
            {
                Send(x);
                Thread.Sleep(_pollWait);
                i++;
            }
        }

        /// <summary>
        /// send key
        /// </summary>
        /// <param name="key"></param>
        private static void Send(string key)
        {
            if(_verbose)
            {
                Log($"  {key}");
            }
            SendKeys.SendWait(key);
        }

        /// <summary>
        /// log combat state
        /// </summary>
        /// <param name="inBattle"></param>
        private static void Log(bool inBattle)
        {
            Log(inBattle ? "In combat" : "Out of combat");
        }

        /// <summary>
        /// log generic
        /// </summary>
        /// <param name="value"></param>
        private static void Log(string value)
        {
            Console.WriteLine(value);
        }

        /// <summary>
        /// check combat state by pixel color
        /// </summary>
        /// <param name="battleMenu"></param>
        /// <param name="screenColor"></param>
        /// <returns></returns>
        private static bool CheckCombatState(Color battleMenu, Color screenColor)
        {
            return
                Math.Abs(battleMenu.R - screenColor.R) < _toleranceColor &&
                Math.Abs(battleMenu.G - screenColor.G) < _toleranceColor &&
                Math.Abs(battleMenu.B - screenColor.B) < _toleranceColor;
        }

        /// <summary>
        /// check death/main-menu state by color
        /// </summary>
        /// <param name="screencolor"></param>
        /// <returns></returns>
        private static bool CheckDeath(Color screencolor)
        {
            //  main menu?
            return
                Math.Abs(screencolor.R - 255) < _toleranceColor &&
                Math.Abs(screencolor.G - 255) < _toleranceColor &&
                Math.Abs(screencolor.B - 255) < _toleranceColor;
        }

        /// <summary>
        /// find window by name (starts with)
        /// </summary>
        /// <param name="windowName"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static bool FindWindow(string windowName, out IntPtr handle)
        {
            foreach (Process pList in Process.GetProcesses())
            {
                if (pList.MainWindowTitle.ToUpper().StartsWith(windowName))
                {
                    // ignore this come on..
                    if (pList.MainWindowTitle.ToLower().Contains("discord"))
                    {
                        continue;
                    }

                    handle = pList.MainWindowHandle;

                    Console.WriteLine($"Found: {pList.MainWindowTitle}");
                    Console.WriteLine($"Handle: {handle}");

                    return true;
                }
            }
            handle = IntPtr.Zero;
            return false;
        }

        /// <summary>
        /// get window rect
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static Rect GetWindowLocation(IntPtr  handle)
        {
            Rect result = new Rect();
            GetWindowRect(handle, ref result);


            return result;
        }

        /// <summary>
        /// get color of found window at pixel location _battleMenuPixelXPercent,_battleMenuPixelYPercent
        /// </summary>
        /// <returns></returns>
        private static Color FindPixel()
        {
            Rect rect = GetWindowLocation(_gameHandle);

            int width = rect.Bottom - rect.Top;
            int height = rect.Right - rect.Left;
            
            int xPosition = (int)Math.Floor(rect.Right * _battleMenuPixelXPercent);
            int yPosition = (int)Math.Floor(rect.Bottom * _battleMenuPixelYPercent);
            
            Color color = GetColorAt(xPosition, yPosition);

            return color;
        }

        /// <summary>
        /// get color atpixel location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);

            int a = (int)GetPixel(dc, x, y);
            ReleaseDC(dc, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }
    }
}
