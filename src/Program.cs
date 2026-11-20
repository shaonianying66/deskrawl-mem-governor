using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DeskrawlMem
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--cli" || args[0] == "-c"))
                return Cli(args);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
            {
                LogCrash(e.Exception);
                MessageBox.Show("这一步操作失败了：\n\n" + e.Exception.Message +
                                "\n\n程序没有退出，可以继续用其他功能。",
                                "Deskrawl 内存管家", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            try
            {
                Application.Run(new MainForm(args));
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                MessageBox.Show(ex.Message, "Deskrawl 内存管家", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            return 0;
        }

        private static void LogCrash(Exception ex)
        {
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Application.StartupPath, "crash.log"),
                    DateTime.Now + "\r\n" + ex, Encoding.UTF8);
            }
            catch { }
        }

        // --------------------------------------------------------------
        private static Dictionary<string, string> Parse(string[] args, int start)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = start; i < args.Length; i++)
            {
                string a = args[i];
                if (a.StartsWith("--"))
                {
                    string key = a.Substring(2);
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) { d[key] = args[i + 1]; i++; }
                    else d[key] = "1";
                }
            }
            return d;
        }

        private static string Get(Dictionary<string, string> d, string key, string def)
        {
            string v;
            return d.TryGetValue(key, out v) ? v : def;
        }

        private static int Int(Dictionary<string, string> d, string key, int def)
        {
            int v;
            return int.TryParse(Get(d, key, ""), out v) ? v : def;
        }

        private static long MB(Dictionary<string, string> d, string key, long def)
        {
            long v;
            return long.TryParse(Get(d, key, ""), out v) ? v << 20 : def;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private static void PrepareConsole()
        {
            try
            {
                if (GetConsoleWindow() != IntPtr.Zero)
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;
                }
                else
                {
                    // 被重定向（管道 / 文件）时强制 UTF-8，避免中文乱码
                    UTF8Encoding utf8 = new UTF8Encoding(false);
                    StreamWriter so = new StreamWriter(Console.OpenStandardOutput(), utf8);
                    so.AutoFlush = true;
                    Console.SetOut(so);
                    StreamWriter se = new StreamWriter(Console.OpenStandardError(), utf8);
                    se.AutoFlush = true;
                    Console.SetError(se);
                }
            }
            catch { }
        }

        private static int Cli(string[] args)
        {
            PrepareConsole();
            string cmd = args.Length > 1 ? args[1] : "help";
            Dictionary<string, string> o = Parse(args, 2);
            try
            {
                switch (cmd)
                {
                    case "list": return CmdList();
                    case "stat": return CmdStat(o);
                    case "apply": return CmdApply(o);
                    case "release": return CmdRelease(o);
                    case "snapshot": return CmdSnapshot(o);
                    case "watch": return CmdWatch(o);
                    case "help":
                    default: return Help();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("错误：" + ex.Message);
                return 1;
            }
        }

        private static int Help()
        {
            Console.WriteLine("Deskrawl 内存管家 - 命令行");
            Console.WriteLine();
            Console.WriteLine("  --cli list                         列出 Deskrawl 相关进程");
            Console.WriteLine("  --cli stat     --pid N              查看实时内存");
            Console.WriteLine("  --cli apply    --pid N --limit 4096 [--mode cap|trim|commit] [--priority 1..5]");
            Console.WriteLine("  --cli release  --pid N              解除限制");
            Console.WriteLine("  --cli snapshot --pid N [--out 文件]  深度体检（分析内存构成）");
            Console.WriteLine("  --cli watch    --name Deskrawl.exe --limit 4096 [--mode cap] [--interval 3]");
            Console.WriteLine();
            Console.WriteLine("  --limit 单位 MB；cap=工作集上限（推荐），trim=上限+定时回收，commit=提交硬上限（可能崩溃）");
            return 0;
        }

        private static int CmdList()
        {
            List<ProcEntry> list = Procs.List(true);
            Console.WriteLine("PID      工作集      私有       类型   名称");
            Console.WriteLine(new string('-', 78));
            foreach (ProcEntry e in list)
            {
                if (e.Kind == "其他" && e.WorkingSet < (200L << 20)) continue;
                Console.WriteLine(
                    e.Pid.ToString().PadRight(8) +
                    Native.Fmt(e.WorkingSet).PadRight(12) +
                    Native.Fmt(e.Private).PadRight(11) +
                    (e.Kind ?? "").PadRight(7) +
                    e.Name + (string.IsNullOrEmpty(e.Path) ? "" : "  " + e.Path));
            }
            int g = Procs.FindGamePid();
            Console.WriteLine();
            Console.WriteLine(g > 0 ? "检测到游戏进程：PID " + g : "未检测到 Deskrawl.exe");
            return 0;
        }

        private static int CmdStat(Dictionary<string, string> o)
        {
            int pid = Int(o, "pid", 0);
            if (pid <= 0) pid = Procs.FindGamePid();
            if (pid <= 0) { Console.Error.WriteLine("未找到进程，请用 --pid 指定"); return 2; }
            int seconds = Int(o, "seconds", 1);
            using (Governor g = new Governor())
            {
                if (!g.Attach(pid)) { Console.Error.WriteLine(g.Error); return 3; }
                for (int i = 0; i < seconds; i++)
                {
                    ProcSample s = g.Sample();
                    Console.WriteLine(s.Time.ToString("HH:mm:ss") + "  ws=" + Native.Fmt(s.WorkingSet) +
                                      "  commit=" + Native.Fmt(s.Commit) + "  private=" + Native.Fmt(s.Private) +
                                      "  cpu=" + s.Cpu.ToString("0.0") + "%  threads=" + s.Threads + "  handles=" + s.Handles +
                                      (g.CurrentLimit > 0 ? "  limit=" + Native.Fmt(g.CurrentLimit) : ""));
                    if (seconds > 1) Thread.Sleep(1000);
                }
            }
            return 0;
        }

        private static int CmdApply(Dictionary<string, string> o)
        {
            int pid = Int(o, "pid", 0);
            if (pid <= 0) pid = Procs.FindGamePid();
            if (pid <= 0) { Console.Error.WriteLine("未找到进程，请用 --pid 指定"); return 2; }
            long limit = MB(o, "limit", 0);
            if (limit <= 0) { Console.Error.WriteLine("请用 --limit 指定上限（MB）"); return 2; }
            string mode = Get(o, "mode", "cap");
            string err;
            using (Governor g = new Governor())
            {
                if (!g.Attach(pid)) { Console.Error.WriteLine(g.Error); return 3; }
                if (mode == "commit")
                {
                    if (!g.ApplyCommitCap(limit, out err)) { Console.Error.WriteLine(err); return 4; }
                }
                else
                {
                    if (!g.ApplyWorkingSet(limit, out err)) { Console.Error.WriteLine(err); return 4; }
                    if (mode == "trim") g.Trim(out err);
                }
                string prio = Get(o, "priority", "");
                if (prio.Length > 0) g.SetMemoryPriority((uint)int.Parse(prio), out err);

                ProcSample before = g.Sample();
                Console.WriteLine("已应用：" + g.Name + " PID " + pid + " 上限 " + Native.Fmt(limit) + " 方式 " + mode);
                for (int i = 0; i < 4; i++)
                {
                    Thread.Sleep(700);
                    ProcSample s = g.Sample();
                    Console.WriteLine("  " + (i + 1) + "s: ws=" + Native.Fmt(s.WorkingSet) + " commit=" + Native.Fmt(s.Commit));
                }
                Console.WriteLine("初始 ws=" + Native.Fmt(before.WorkingSet));
            }
            return 0;
        }

        private static int CmdRelease(Dictionary<string, string> o)
        {
            int pid = Int(o, "pid", 0);
            if (pid <= 0) pid = Procs.FindGamePid();
            if (pid <= 0) { Console.Error.WriteLine("未找到进程"); return 2; }
            string err;
            using (Governor g = new Governor())
            {
                if (!g.Attach(pid)) { Console.Error.WriteLine(g.Error); return 3; }
                g.ReleaseCommitCap();
                if (!g.ReleaseWorkingSet(0, out err)) { Console.Error.WriteLine(err); return 4; }
                Console.WriteLine("已解除限制：" + g.Name + " PID " + pid);
            }
            return 0;
        }

        private static int CmdSnapshot(Dictionary<string, string> o)
        {
            int pid = Int(o, "pid", 0);
            if (pid <= 0) pid = Procs.FindGamePid();
            if (pid <= 0) { Console.Error.WriteLine("未找到进程，请用 --pid 指定"); return 2; }
            Snapshot s = Inspector.Run(pid);
            if (s.Error != null) { Console.Error.WriteLine(s.Error); return 3; }
            string text = s.ToReport();
            Console.WriteLine(text);
            string outPath = Get(o, "out", "");
            if (outPath.Length > 0)
            {
                File.WriteAllText(outPath, text, Encoding.UTF8);
                Console.WriteLine("已写入 " + outPath);
            }
            return 0;
        }

        private static int CmdWatch(Dictionary<string, string> o)
        {
            string name = Get(o, "name", "Deskrawl.exe");
            long limit = MB(o, "limit", 4096);
            string mode = Get(o, "mode", "cap");
            int interval = Int(o, "interval", 3);
            Console.WriteLine("守护中：等待 " + name + "，上限 " + Native.Fmt(limit) + "，每 " + interval + " 秒检查。Ctrl+C 退出。");
            Governor g = new Governor();
            long lastTrim = 0;
            DateTime lastTrimAt = DateTime.MinValue;
            while (true)
            {
                try
                {
                    int pid = 0;
                    foreach (ProcEntry e in Procs.List(false))
                        if (string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)) { pid = e.Pid; break; }
                    if (pid == 0) pid = Procs.FindGamePid();

                    if (pid == 0)
                    {
                        if (g.Pid != 0) { g.Dispose(); Console.WriteLine("进程已退出。"); }
                    }
                    else if (g.Pid != pid || !g.Alive)
                    {
                        g.Dispose();
                        if (g.Attach(pid))
                        {
                            string err;
                            if (mode == "commit") g.ApplyCommitCap(limit, out err);
                            else g.ApplyWorkingSet(limit, out err);
                            Console.WriteLine("已接管 " + g.Name + " PID " + pid + "：" + Native.Fmt(limit));
                        }
                    }
                    else if (g.CurrentLimit != limit && mode != "commit")
                    {
                        string err;
                        g.ApplyWorkingSet(limit, out err);
                        Console.WriteLine("上限更新为 " + Native.Fmt(limit));
                    }

                    if (mode == "trim" && g.Pid != 0 && g.Alive && (DateTime.Now - lastTrimAt).TotalSeconds >= 15)
                    {
                        string err;
                        g.Trim(out err);
                        lastTrimAt = DateTime.Now;
                        lastTrim = g.Sample().WorkingSet;
                        Console.WriteLine("回收工作集 -> " + Native.Fmt(lastTrim));
                    }
                }
                catch (Exception ex) { Console.WriteLine("守护异常：" + ex.Message); }
                Thread.Sleep(Math.Max(1, interval) * 1000);
            }
        }
    }
}
