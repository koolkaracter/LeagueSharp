using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Menu = LeagueSharp.Common.Menu;
using MenuItem = LeagueSharp.Common.MenuItem;


namespace ChatTranslator
{
    internal class Program
    {
        public static Menu Config;
        public static String[] fromArray = new String[]
        {
            "auto", "en", "de", "es", "fr", "pl", "hu", "sq", "sv", "cs", "ro", "da", "pt", "sr", "fi", "lt", "lv",
            "sk", "sl", "ph", "tr", "el", "ms", "zh-CN", "zh-TW", "mk", "bg", "ru", "ko", "it", "be", "vi", "uk"
        };
        public static String[] toArray = new String[]
        {
            "en", "de", "es", "fr", "pl", "hu", "sq", "sv", "cs", "ro", "da", "pt", "sr", "fi", "lt", "lv", "sk", "sl",
            "ph", "tr", "el", "ms", "zh-CN", "zh-TW", "mk", "bg", "ru", "ko", "it", "be", "vi", "uk"
        };
        public static String[] SpecChars = new String[]
        {
            "bg", "zh-CN", "zh-TW", "ru", "ko", "uk"
        };
        public static ObservableCollection<Message> lastMessages = new ObservableCollection<Message>();

        public static bool ShowMessages, sent, copied, translate;
        public static string path, fileName, clipBoard;
        public static List<string> clipBoardLines;
        public static float gamestart;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("ChatTranslator", "ChatTranslator", true);
            Menu translator = new Menu("Translator", "Translator");
            Menu incomingText = new Menu("IncomingText", "IncomingText");
            incomingText.AddItem(new MenuItem("From", "From: ").SetValue(new StringList(fromArray)));
            incomingText.AddItem(new MenuItem("To", "To: ").SetValue(new StringList(toArray)));
            incomingText.AddItem(new MenuItem("Phonetical", "Use special characters").SetValue(false));
            incomingText.AddItem(new MenuItem("Enabled", "Enabled").SetValue(true));
            translator.AddSubMenu(incomingText);
            Menu outgoingText = new Menu("OutgoingText", "OutgoingText");
            outgoingText.AddItem(new MenuItem("OutFrom", "From: ").SetValue(new StringList(toArray)));
            outgoingText.AddItem(new MenuItem("OutTo", "To: ").SetValue(new StringList(toArray)));
            outgoingText.AddItem(new MenuItem("EnabledOut", "Enabled").SetValue(false));
            translator.AddSubMenu(outgoingText);
            Menu position = new Menu("Position", "Position");
            position.AddItem(new MenuItem("Horizontal", "Horizontal").SetValue(new Slider(15, 1, 2000)));
            position.AddItem(new MenuItem("Vertical", "Vertical").SetValue(new Slider(500, 1, 2000)));
            position.AddItem(new MenuItem("AutoShow", "Show on message").SetValue(true));
            position.AddItem(new MenuItem("Duration", "   Duration").SetValue(new Slider(3000, 1000, 8000)));
            translator.AddSubMenu(position);
            translator.AddItem(new MenuItem("Check", "Check").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.AddSubMenu(translator);
            Menu logger = new Menu("Logger", "Logger");
            logger.AddItem(new MenuItem("EnabledLog", "Enable").SetValue(true));
            Config.AddSubMenu(logger);
            Menu copyPaste = new Menu("Paste", "Paste");
            copyPaste.AddItem(new MenuItem("Paste", "Paste").SetValue(new KeyBind("P".ToCharArray()[0], KeyBindType.Press)));
            copyPaste.AddItem(new MenuItem("PasteForAll", "Paste for all").SetValue(new KeyBind("O".ToCharArray()[0], KeyBindType.Press)));
            copyPaste.AddItem(new MenuItem("Delay", "Spam delay").SetValue(new Slider(2000, 0, 2000)));
            copyPaste.AddItem(new MenuItem("DisablePaste", "Disable this section").SetValue(true));
            Config.AddSubMenu(copyPaste);
            Config.AddToMainMenu();

            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- ChatTranslator</font>");
            Game.OnUpdate += Game_OnUpdate;
            Game.OnInput += Game_GameInput;
            Game.OnChat += Game_OnChat;
            Drawing.OnDraw+=Drawing_OnDraw;
            lastMessages.CollectionChanged+=OnMessage;
            path = string.Format(@"{0}\ChatLogs\{1}\{2}\{3}\{4}\", LeagueSharp.Common.Config.AppDataDirectory, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MMMM"), DateTime.Now.ToString("dd"), Utility.Map.GetMap().ShortName);
            fileName = ObjectManager.Player.SkinName + "_" + Game.Id + ".txt";
            if (!System.IO.Directory.Exists(path))
            {
               System.IO.Directory.CreateDirectory(path); 
            }
            if (Config.Item("EnabledLog").GetValue<bool>())
            {
                InitText();
            }
            test("Hello");
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("Check").GetValue<KeyBind>().Active || ShowMessages)
            {
                var posX = Config.Item("Horizontal").GetValue<Slider>().Value;
                var posY = Config.Item("Vertical").GetValue<Slider>().Value;
                var line = 0;
                foreach (var message in lastMessages)
                {

                    Size textSize = TextRenderer.MeasureText(message.sender.Name + ":", new Font(FontFamily.GenericSansSerif, 10));

                    if (!message.sender.IsAlly)
                    {
                        Drawing.DrawText(posX, posY + line, Color.Red, message.sender.Name + ":");
                    }
                    else
                    {
                        if (message.sender.IsMe)
                        {
                            Drawing.DrawText(posX, posY + line, Color.Goldenrod, message.sender.Name + ":");
                        }
                        else
                        {
                            Drawing.DrawText(posX, posY + line, Color.DeepSkyBlue, message.sender.Name + ":");
                        }

                    }
                    Drawing.DrawText(posX + textSize.Width + message.sender.Name.Length, posY + line, Color.White, message.message);
                    line += 15;
                }
            }

        }

        static void Game_OnChat(GameChatEventArgs args)
        {
            if (args.Message.Contains("font color"))
            {
                return;
            }
            if (Config.Item("EnabledLog").GetValue<bool>())
            {
                AddToLog(args.Message, args.Sender);
            }
            addMessage(args.Message, args.Sender);
        }

        static void Game_OnUpdate(EventArgs args)
        {
            if (Config.Item("DisablePaste").GetValue<bool>())
            {
                return;
            }
            if (sent)
            {
                return;
            }
            var delay = Config.Item("Delay").GetValue<Slider>().Value;
            if (Config.Item("Paste").GetValue<KeyBind>().Active)
            {
                SetClipBoardData();
                if (!clipBoard.Contains("\n"))
                {
                    Game.Say(clipBoard);
                    sent = true;
                    Utility.DelayAction.Add(delay, () => sent = false);
                }
                else
                {
                    foreach (string s in clipBoardLines)
                    {
                        Game.Say(s);
                    }
                    sent = true;
                    Utility.DelayAction.Add(delay, () => sent = false);
                    clipBoardLines.Clear();
                }
            }
            if (Config.Item("PasteForAll").GetValue<KeyBind>().Active)
            {
                SetClipBoardData();
                if (!clipBoard.Contains("\n"))
                {
                    Game.Say("/all " + clipBoard);
                    sent = true;
                    Utility.DelayAction.Add(delay, () => sent = false);
                }
                else
                {
                    foreach (string s in clipBoardLines)
                    {
                        Game.Say("/all " + s);
                    }
                    sent = true;
                    Utility.DelayAction.Add(delay, () => sent = false);
                    clipBoardLines.Clear();
                }
            }
        }

        private static void SetClipBoardData()
        {
            if (Clipboard.ContainsText())
            {
                clipBoard = setEncodingDefault(Clipboard.GetText());
                if (clipBoard.Contains("\n"))
                {
                    clipBoardLines = clipBoard.Split('\n').ToList();
                }
            }
        }

        private static void OnMessage(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Config.Item("AutoShow").GetValue<bool>() && translate)
            {
                ShowMessages = true;
                Utility.DelayAction.Add(Config.Item("Duration").GetValue<Slider>().Value, () => ShowMessages = false);
            }
        }

        private static string setEncodingUTF8(string data)
        {
            byte[] bytes = Encoding.Default.GetBytes(data);
            return Encoding.UTF8.GetString(bytes);
        }
        private static string setEncodingDefault(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            return Encoding.Default.GetString(bytes);
        }
        private static void InitText()
        {
            if (!File.Exists(path + fileName))
            {
                string initText = "";
                var team = "";
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (team != hero.Team.ToString())
                    {
                        initText += "\t" + hero.Team.ToString() + "\n";
                        team = hero.Team.ToString();
                    }
                    initText += hero.Name + " (" + hero.ChampionName + ")\n";
                }
                initText += "------------------------\n";
                File.AppendAllText(path + fileName, initText, Encoding.Default);
            }
        }

        private static void AddToLog(string message, Obj_AI_Hero sender)
        {
            if (sender==null || message==null)
            {
                return;
            }
            InitText();
            string line = sender.Name+" ("+sender.ChampionName+")" + ": " + message+"\n";
            File.AppendAllText(path + fileName, line, Encoding.Default);
        }

        private static async void addMessage(string message, Obj_AI_Hero sender)
        {
            
            string from = Config.Item("From").GetValue<StringList>().SelectedValue;
            string to = Config.Item("To").GetValue<StringList>().SelectedValue;
            if (from != to && !sender.IsMe)
            {
                translate = true;
                Utility.DelayAction.Add(500, () => translate = false);
                string translated = await TranslateGoogle(message, from, to, true);
                lastMessages.Add(new Message(translated, sender));
            }
            else
            {
                lastMessages.Add(new Message(message, sender));
            }
            if (lastMessages.Count > 8)
            {
                lastMessages.RemoveAt(0);
            }
        }

        private static void Game_GameInput(GameInputEventArgs args)
        {
            if (Config.Item("EnabledOut").GetValue<bool>() &&
                Config.Item("OutFrom").GetValue<StringList>().SelectedValue !=
                Config.Item("OutTo").GetValue<StringList>().SelectedValue)
            {
                var message = "";
                message += args.Input;
                TranslateAndSend(message);
                args.Process = false;
            }
        }
        private static async void test(string text)
        {
            if (text.Length > 1)
            {
                string from = Config.Item("OutFrom").GetValue<StringList>().SelectedValue;
                string to = Config.Item("OutTo").GetValue<StringList>().SelectedValue;
                string x = "";
                x += await TranslateGoogle(text, from, to, false);
                Console.WriteLine(x);
            }
        }
        private static async void TranslateAndSend(string text)
        {
            if (text.Length > 1)
            {
                bool all = false;
                if (text.Contains("/all"))
                {
                    text = text.Replace("/all", "");
                    all = true;
                }
                string from = Config.Item("OutFrom").GetValue<StringList>().SelectedValue;
                string to = Config.Item("OutTo").GetValue<StringList>().SelectedValue;
                string x = "";
                x += await TranslateGoogle(text, from, to, false);
                if (all == true)
                {
                    Game.Say("/all " + x);
                }
                else
                {
                    Game.Say(x);
                }
            }
        }

        private static async Task<string> TranslateGoogle(string text, string fromCulture, string toCulture, bool langs)
        {
            string url;
            string strServerURL;

            strServerURL = "https://translate.google.com/translate_a/single?client=t&sl={0}&tl={1}&hl=en&dt=bd&dt=ex&dt=ld&dt=md&dt=qca&dt=rw&dt=rm&dt=ss&dt=t&dt=at&ie=UTF-8&oe=UTF-8&source=btn&ssel=3&tsel=3&kc=0&tk=520576|693806&q={2}";
            url = string.Format(strServerURL, fromCulture, toCulture, text.Replace(' ', '+'));

            byte[] bytessss = Encoding.Default.GetBytes(url);
            url = Encoding.UTF8.GetString(bytessss);
            string html="";

                System.Uri uri = new System.Uri(url);
                try
                {
                    html = await DownloadStringAsync(uri);
                }
                catch (Exception e)
                {
                    
                   Console.WriteLine(e.Message);
                } 

            string result = "";
            if (langs == true)
            {
                result += "(" + fromCulture + " => " + toCulture + ") ";
            }
            string trans ="";
            if (Config.Item("Phonetical").GetValue<bool>() && SpecChars.Contains(toCulture))
            {
                trans = Regex.Matches(html, "\\\".*?\\\"", RegexOptions.IgnoreCase)[2].ToString(); 
            }
            else
            {
                trans = Regex.Matches(html, "\\\".*?\\\"", RegexOptions.IgnoreCase)[0].ToString(); 
            }

            result += trans.Substring(1, trans.Length - 2);

            //result += trans.Substring(19, trans.Length - 20);

            return result;
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        public static Task<string> DownloadStringAsync(Uri url)
        {
            var tcs = new TaskCompletionSource<string>();
            var wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
            wc.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");
            wc.Encoding = Encoding.UTF8;
            wc.DownloadStringCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else if (e.Cancelled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(e.Result);
                }
            };
            wc.DownloadStringAsync(url);
  
            return tcs.Task;
            
        }

        private static double StringCompare(string a, string b)
        {
            if (a == b)
            {
                return 100;
            }
            if ((a.Length == 0) || (b.Length == 0))
            {
                return 0;
            }
            double maxLen = a.Length > b.Length ? a.Length : b.Length;
            int minLen = a.Length < b.Length ? a.Length : b.Length;
            int sameCharAtIndex = 0;
            for (int i = 0; i < minLen; i++)
            {
                if (a[i] == b[i])
                {
                    sameCharAtIndex++;
                }
            }
            return sameCharAtIndex / maxLen * 100;
        }
    }
}