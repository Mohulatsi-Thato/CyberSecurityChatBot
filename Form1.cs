// MainForm.cs  ────────────────────────────────────────────────────────────────
// Put this in your CyberChatBotGUI project and remove any old Designer files.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WinTimer = System.Windows.Forms.Timer;

namespace CyberChatBotGUI
{
    public class MainForm : Form
    {
        // ────────────────────────────  user context  ─────────────────────────
        private string userName = "User";
        private string userInterest = "cybersecurity";
        private string userSentiment = "neutral";

        // ────────────────────────────  knowledge pool  ───────────────────────
        private readonly Random rnd = new();
        private readonly List<string> phishingTips = new()
        {
            "Be cautious of emails asking for personal info. Scammers often pose as trusted companies.",
            "Always check the sender's address — fake ones often look similar to real domains.",
            "Hover over links before you click; make sure the URL looks right.",
            "Never download unexpected attachments — they may contain malware.",
            "Enable two-factor authentication so stolen passwords alone aren’t enough."
        };

        // ────────────────────────────  task + reminder  ──────────────────────
        private class TaskItem
        {
            public string Title = "";
            public DateTime? Reminder = null;
            public bool Completed = false;
        }
        private readonly List<TaskItem> tasks = new();

        // ────────────────────────────  activity log  ─────────────────────────
        private readonly List<string> activityLog = new();
        private void Log(string msg)
        {
            activityLog.Add($"{DateTime.Now:yyyy-MM-dd HH:mm} – {msg}");
            if (activityLog.Count > 250) activityLog.RemoveAt(0);
        }

        // ────────────────────────────  quiz data  ────────────────────────────
        private readonly List<(string Q, string[] Opt, int Ans, string Exp)> quiz =
            new()
            {
                ("What should you do if you get an email asking for your password?",
                 new[]{"Reply with password","Delete it","Report as phishing","Ignore it"}, 2,
                 "Reporting phishing emails helps prevent scams."),
                ("True / False: use the same password on every site.",
                 new[]{"True","False"},1,
                 "Re-using passwords means one breach affects every account."),
                ("Which is a strong password?",
                 new[]{"password123","qwerty","P@ssw0rd!2024","johnsmith"},2,
                 "Strong passwords mix upper/lower, numbers & symbols."),
                ("What is 2-factor authentication?",
                 new[]{"Backup password","Password reset","Second layer of verification","Antivirus software"},2,
                 "2FA adds an extra verification step."),
                ("Sign of phishing?",
                 new[]{"Personal greeting","Poor grammar + urgency","From your boss","Correct full name"},1,
                 "Urgent tone and errors are common phishing clues."),
                ("True / False: Public Wi-Fi is always safe.",new[]{"True","False"},1,
                 "Public Wi-Fi can be risky — use a VPN."),
                ("Good browsing habit?",
                 new[]{"Click every link","Skip updates","Check site URLs","Share passwords"},2,
                 "Checking URLs helps avoid fake sites."),
                ("Firewall purpose?",
                 new[]{"Burn viruses","Block unauthorised access","Manage cookies","Clean PC"},1,
                 "Firewalls keep intruders out of a network."),
                ("Protects data on public networks?",new[]{"Incognito","VPN","Cookies","Firewall"},1,
                 "A VPN encrypts traffic on open networks."),
                ("True / False: open attachments from unknown senders.",new[]{"True","False"},1,
                 "Unknown attachments might contain malware.")
            };
        private int quizIdx = -1, quizScore = 0;

        // ────────────────────────────  UI controls  ──────────────────────────
        private RichTextBox chat;
        private TextBox input;

        // ────────────────────────────  constructor / UI  ─────────────────────
        public MainForm()
        {
            BuildUi();
            StartReminderTimer();
            AppendBot("Hello! I’m your Cyber-Security Assistant. Type 'help' for commands.");
        }

        private void BuildUi()
        {
            Text = "CyberBot";
            MinimumSize = new Size(640, 560);
            StartPosition = FormStartPosition.CenterScreen;

            chat = new RichTextBox
            {
                Location = new Point(20, 20),
                Size = new Size(580, 400),
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            input = new TextBox
            {
                Location = new Point(20, 440),
                Size = new Size(480, 28),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            var send = new Button
            {
                Text = "Send",
                Location = new Point(520, 438),
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            send.Click += (s, e) => HandleInput();
            input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; send.PerformClick(); } };

            Controls.AddRange(new Control[] { chat, input, send });
        }

        // ────────────────────────────  helper output  ────────────────────────
        private void AppendUser(string text) => chat.AppendText($"You: {text}\n");
        private void AppendBot(string text) => chat.AppendText($"Bot: {text}\n\n");

        // ────────────────────────────  main input path  ──────────────────────
        private void HandleInput()
        {
            string original = input.Text.Trim();
            if (original == "") return;
            AppendUser(original);
            string lower = original.ToLowerInvariant();

            // If currently inside quiz flow
            if (quizIdx >= 0) { HandleQuizAnswer(lower); input.Clear(); return; }

            DetectSentiment(lower);
            string reply = ProcessIntent(lower, original);
            AppendBot(reply);
            input.Clear();
        }

        // ───── sentiment
        private void DetectSentiment(string txt)
        {
            userSentiment = txt.Contains("worried") || txt.Contains("scared") ? "worried" :
                            txt.Contains("curious") || txt.Contains("interested") ? "curious" :
                            txt.Contains("frustrated") || txt.Contains("annoyed") ? "frustrated" : "neutral";
        }
        private string Mood() => userSentiment switch
        {
            "worried" => "It’s normal to feel worried. ",
            "curious" => "Curiosity is great! ",
            "frustrated" => "I know this can feel overwhelming. ",
            _ => ""
        };

        // ───── intent / NLP simulation
        private string ProcessIntent(string lower, string original)
        {
            // help
            if (Regex.IsMatch(lower, @"^(help|commands)$"))
                return "Commands: add task <desc>, remind me in X days, show tasks, start quiz, show activity log.";

            // show activity log
            if (lower.Contains("activity log") || lower.StartsWith("what have you done"))
                return activityLog.Any()
                        ? "Recent actions:\n" + string.Join("\n", activityLog.TakeLast(10).Select((l, i) => $"{i + 1}. {l}"))
                        : "No activities recorded yet.";

            // ---- tasks ------------------------------------------------------------------
            if (Regex.IsMatch(lower, @"(add .*task)|(remind .*to)|(set .*reminder)"))
            {
                string desc = Regex.Replace(original, @"(?i)(add .*task|remind .*to|set .*reminder)", "").Trim();
                if (desc == "") return "Please tell me what the task is.";
                tasks.Add(new TaskItem { Title = desc });
                Log($"Task added: '{desc}' (no reminder)");
                return $"Task added: '{desc}'. Want to set a reminder?";
            }

            if (lower.StartsWith("remind me in"))
            {
                var m = Regex.Match(lower, @"remind me in (\d+) (day|days|week|weeks)");
                if (!m.Success || !int.TryParse(m.Groups[1].Value, out int n))
                    return "Try 'remind me in 3 days'.";
                var unit = m.Groups[2].Value.StartsWith("week") ? 7 : 1;
                if (!tasks.Any()) return "Add a task first so I know what to remind you about.";
                var t = tasks.Last(); t.Reminder = DateTime.Now.AddDays(n * unit); Log($"Reminder set for '{t.Title}' on {t.Reminder:yyyy-MM-dd}");
                return $"Got it! I'll remind you on {t.Reminder:yyyy-MM-dd}.";
            }

            if (lower.Contains("show tasks") || lower.Contains("my tasks"))
                return tasks.Any()
                        ? string.Join("\n", tasks.Select((t, i) => $"{i + 1}. {t.Title} {(t.Completed ? "✓" : "")} {(t.Reminder is null ? "" : $"⏰ {t.Reminder:yyyy-MM-dd}")}"))
                        : "You have no tasks yet.";

            // ---- quiz -------------------------------------------------------------------
            if (lower.Contains("start quiz") || lower == "quiz")
            {
                quizIdx = 0; quizScore = 0; Log("Quiz started");
                return AskQuiz();
            }

            // ---- knowledge lookup -------------------------------------------------------
            if (lower.Contains("phishing"))
            { Log("Gave phishing tip"); return Mood() + phishingTips[rnd.Next(phishingTips.Count)]; }
            if (lower.Contains("password"))
            { Log("Gave password tip"); return Mood() + "Use strong, unique passwords and consider a password manager."; }
            if (lower.Contains("vpn"))
            { Log("Gave VPN tip"); return Mood() + "A VPN encrypts your connection, especially on public Wi-Fi."; }

            return Mood() + "I'm not sure how to help with that. Type 'help' to see what I can do.";
        }

        // ───── quiz helpers
        private string AskQuiz()
        {
            if (quizIdx >= quiz.Count)
            {
                string end = $"Quiz done! You scored {quizScore}/{quiz.Count}.\n"
                           + (quizScore >= 8 ? "Great job — cyber-security pro!" : "Keep learning to stay safe online.");
                Log($"Quiz finished – score {quizScore}/{quiz.Count}");
                quizIdx = -1; return end;
            }
            var (q, opt, _, _) = quiz[quizIdx];
            return $"Question {quizIdx + 1}: {q}\n"
                 + string.Join("\n", opt.Select((o, i) => $"{i + 1}. {o}"));
        }
        private void HandleQuizAnswer(string ans)
        {
            if (!int.TryParse(ans, out int sel))
            { AppendBot("Enter the answer number (1-4)."); return; }

            var (q, opt, correct, exp) = quiz[quizIdx];
            if (sel - 1 == correct) { quizScore++; AppendBot($"Correct! {exp}"); }
            else { AppendBot($"Not quite. {exp}"); }
            quizIdx++;
            AppendBot(AskQuiz());
        }

        // ───── reminders timer
        private void StartReminderTimer()
        {
            var timer = new WinTimer { Interval = 60_000 };
            timer.Tick += (s, e) => {
                foreach (var t in tasks.Where(t => !t.Completed && t.Reminder <= DateTime.Now).ToList())
                {
                    AppendBot($"🔔 Reminder: {t.Title} is due!");
                    Log($"Reminder triggered for '{t.Title}'");
                    t.Reminder = null; // alert once
                }
            };
            timer.Start();
        }
    }
}
