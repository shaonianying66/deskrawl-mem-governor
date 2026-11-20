using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DeskrawlMem
{
    // ------------------------------------------------------------------
    // Win32
    // ------------------------------------------------------------------
    internal static class Native
    {
        public const uint PROCESS_TERMINATE = 0x0001;
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint PROCESS_SET_QUOTA = 0x0100;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public const uint QUOTA_HARDWS_MIN_ENABLE = 0x1;
        public const uint QUOTA_HARDWS_MAX_ENABLE = 0x4;
        public const uint QUOTA_HARDWS_MAX_DISABLE = 0x8;

        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_IMAGE = 0x1000000;
        public const uint MEM_MAPPED = 0x40000;
        public const uint MEM_PRIVATE = 0x20000;

        public const int JobObjectExtendedLimitInformation = 9;
        public const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x100;
        public const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x200;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessWorkingSetSizeEx(IntPtr h, IntPtr min, IntPtr max, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessWorkingSetSize(IntPtr h, out IntPtr min, out IntPtr max);
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EmptyWorkingSet(IntPtr h);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessHandleCount(IntPtr h, out int count);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessTimes(IntPtr h, out long creation, out long exit, out long kernel, out long user);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageNameW(IntPtr h, int flags, StringBuilder sb, ref int size);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateJobObjectW(IntPtr attrs, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetInformationJobObject(IntPtr job, int cls, IntPtr info, int len);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(IntPtr job, IntPtr proc);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsProcessInJob(IntPtr proc, IntPtr job, out int result);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryDosDeviceW(string name, StringBuilder target, int max);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX st);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessInformation(IntPtr h, int cls, ref int info, int size);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, int pid);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Process32FirstW(IntPtr snap, ref PROCESSENTRY32W pe);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Process32NextW(IntPtr snap, ref PROCESSENTRY32W pe);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetProcessMemoryInfo(IntPtr h, ref PMC_EX p, int cb);
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModulesEx(IntPtr h, IntPtr[] mods, int cb, out int needed, uint filter);
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(IntPtr h, IntPtr mod, out MODULEINFO mi, int cb);
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetModuleFileNameExW(IntPtr h, IntPtr mod, StringBuilder sb, int size);
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetMappedFileNameW(IntPtr h, IntPtr addr, StringBuilder sb, int size);
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool QueryWorkingSetEx(IntPtr h, [In, Out] WSW_EX[] info, int cb);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr VirtualQueryEx(IntPtr h, IntPtr addr, out MEMORY_BASIC_INFORMATION mbi, IntPtr len);

        public const uint TH32CS_SNAPPROCESS = 0x2;
        public const uint LIST_MODULES_ALL = 0x3;

        [StructLayout(LayoutKind.Sequential)]
        public struct PMC_EX
        {
            public int cb;
            public int PageFaultCount;
            public IntPtr PeakWorkingSetSize;
            public IntPtr WorkingSetSize;
            public IntPtr QuotaPeakPagedPoolUsage;
            public IntPtr QuotaPagedPoolUsage;
            public IntPtr QuotaPeakNonPagedPoolUsage;
            public IntPtr QuotaNonPagedPoolUsage;
            public IntPtr PagefileUsage;
            public IntPtr PeakPagefileUsage;
            public IntPtr PrivateUsage;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public uint AllocationProtect;
            public uint PartitionId;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WSW_EX
        {
            public IntPtr VirtualAddress;
            public UIntPtr VirtualAttributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PROCESSENTRY32W
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJ_BASIC
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJ_IO
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJ_EXT
        {
            public JOBOBJ_BASIC Basic;
            public JOBOBJ_IO Io;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        public static long MB(long bytes) { return bytes >> 20; }
        public static string Fmt(long bytes)
        {
            double mb = bytes / 1048576.0;
            if (mb >= 1024) return (mb / 1024.0).ToString("0.00") + " GB";
            return mb.ToString("0") + " MB";
        }
        public static string Err()
        {
            int e = Marshal.GetLastWin32Error();
            try { return new System.ComponentModel.Win32Exception(e).Message + " (代码 " + e + ")"; }
            catch { return "错误代码 " + e; }
        }
    }

    // ------------------------------------------------------------------
    // 采样
    // ------------------------------------------------------------------
    public class ProcSample
    {
        public DateTime Time = DateTime.Now;
        public long WorkingSet;
        public long Commit;
        public long Private;
        public long PeakWorkingSet;
        public long PeakCommit;
        public int Handles;
        public int Threads;
        public double Cpu;
        public string Error;
    }

    public class ProcEntry
    {
        public int Pid;
        public string Name;
        public string Path;
        public long WorkingSet;
        public long Private;
        public string Kind;   // 游戏 / 助手 / 其他
        public bool InJob;
    }

    // ------------------------------------------------------------------
    // 进程工具
    // ------------------------------------------------------------------
    public static class Procs
    {
        public static List<ProcEntry> List(bool includeOthers)
        {
            List<ProcEntry> list = new List<ProcEntry>();
            IntPtr snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
            if (snap == (IntPtr)(-1)) return list;
            try
            {
                Native.PROCESSENTRY32W pe = new Native.PROCESSENTRY32W();
                pe.dwSize = (uint)Marshal.SizeOf(typeof(Native.PROCESSENTRY32W));
                if (!Native.Process32FirstW(snap, ref pe)) return list;
                do
                {
                    if (pe.th32ProcessID <= 4) continue;
                    ProcEntry e = ReadEntry((int)pe.th32ProcessID, pe.szExeFile);
                    if (e == null) continue;
                    bool hit = IsDeskrawlRelated(e);
                    if (!hit && !includeOthers) continue;
                    if (e.WorkingSet <= 0 && !hit) continue;
                    e.Kind = KindOf(e);
                    if (hit) list.Add(e);
                    else if (includeOthers) list.Add(e);
                }
                while (Native.Process32NextW(snap, ref pe));
            }
            finally { Native.CloseHandle(snap); }

            list.Sort(delegate(ProcEntry a, ProcEntry b)
            {
                int ka = a.Kind == "游戏" ? 0 : (a.Kind == "助手" ? 1 : 2);
                int kb = b.Kind == "游戏" ? 0 : (b.Kind == "助手" ? 1 : 2);
                if (ka != kb) return ka - kb;
                return b.WorkingSet.CompareTo(a.WorkingSet);
            });
            return list;
        }

        public static ProcEntry ReadEntry(int pid, string nameHint)
        {
            ProcEntry e = new ProcEntry();
            e.Pid = pid;
            e.Name = nameHint;
            IntPtr h = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return e;
            try
            {
                StringBuilder sb = new StringBuilder(1024);
                int sz = sb.Capacity;
                if (Native.QueryFullProcessImageNameW(h, 0, sb, ref sz)) e.Path = sb.ToString();
                Native.PMC_EX pm = new Native.PMC_EX();
                pm.cb = Marshal.SizeOf(typeof(Native.PMC_EX));
                if (Native.GetProcessMemoryInfo(h, ref pm, pm.cb))
                {
                    e.WorkingSet = pm.WorkingSetSize.ToInt64();
                    e.Private = pm.PrivateUsage.ToInt64();
                }
                int inj;
                e.InJob = Native.IsProcessInJob(h, IntPtr.Zero, out inj) && inj != 0;
            }
            finally { Native.CloseHandle(h); }
            return e;
        }

        public static bool IsDeskrawlRelated(ProcEntry e)
        {
            string n = (e.Name ?? "").ToLowerInvariant();
            string p = (e.Path ?? "").ToLowerInvariant();
            if (n == "deskrawl.exe" || n == "deskrawl") return true;
            if (p.Contains("deskrawl") && (n.EndsWith(".exe") || n.EndsWith(".dll"))) return true;
            if (p.Contains("deskrawl") && (n.StartsWith("python") || n.Contains("assistant") || n.Contains("helper"))) return true;
            if (n.Contains("x1x")) return true;
            if (p.Contains("steamapps") && n.StartsWith("deskrawl")) return true;
            return false;
        }

        public static string KindOf(ProcEntry e)
        {
            string n = (e.Name ?? "").ToLowerInvariant();
            string p = (e.Path ?? "").ToLowerInvariant();
            if (n.StartsWith("deskrawlmem")) return "工具";
            if (n == "deskrawl.exe" || n == "deskrawl" || p.EndsWith("\\deskrawl.exe")) return "游戏";
            if (n.Contains("装备助手")) return "助手";
            if (n.Contains("x1x")) return "助手";
            if (p.Contains("deskrawl"))
            {
                if (n.StartsWith("python")) return "助手";
                if (n.Contains("helper") || n.Contains("assistant") || n.Contains("deskrawl")) return "助手";
            }
            if (n == "steam.exe" || n == "steamwebhelper.exe") return "其他";
            return "其他";
        }

        public static int FindGamePid()
        {
            IntPtr snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
            if (snap == (IntPtr)(-1)) return 0;
            try
            {
                Native.PROCESSENTRY32W pe = new Native.PROCESSENTRY32W();
                pe.dwSize = (uint)Marshal.SizeOf(typeof(Native.PROCESSENTRY32W));
                if (!Native.Process32FirstW(snap, ref pe)) return 0;
                do
                {
                    if (pe.th32ProcessID <= 4) continue;
                    if (string.Equals(pe.szExeFile, "Deskrawl.exe", StringComparison.OrdinalIgnoreCase))
                        return (int)pe.th32ProcessID;
                }
                while (Native.Process32NextW(snap, ref pe));
            }
            finally { Native.CloseHandle(snap); }
            return 0;
        }
    }

    // ------------------------------------------------------------------
    // 管控
    // ------------------------------------------------------------------
    public class Governor : IDisposable
    {
        public int Pid;
        public string Name = "";
        public string Error = "";
        public bool ReadOnly;
        public bool CommitCapActive;
        public long CurrentLimit;      // 0 = 未设置
        public long LastLimit;

        private IntPtr _h = IntPtr.Zero;
        private IntPtr _job = IntPtr.Zero;
        private long _lastKernel, _lastUser;
        private DateTime _lastCpuTime = DateTime.MinValue;

        public bool Attach(int pid)
        {
            Detach();
            Pid = pid;
            _h = Native.OpenProcess(Native.PROCESS_TERMINATE | Native.PROCESS_SET_QUOTA |
                                    Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (_h == IntPtr.Zero)
            {
                _h = Native.OpenProcess(Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_VM_READ, false, pid);
                if (_h == IntPtr.Zero)
                {
                    Error = "无法打开进程：" + Native.Err();
                    return false;
                }
                ReadOnly = true;
                Error = "以只读方式连接（权限不足，无法修改内存配额）。以管理员身份运行可解锁全部功能。";
            }
            else ReadOnly = false;
            try { Name = Process.GetProcessById(pid).ProcessName; } catch { Name = "PID " + pid; }
            return true;
        }

        public void Detach()
        {
            ReleaseCommitCap();
            if (_h != IntPtr.Zero) { Native.CloseHandle(_h); _h = IntPtr.Zero; }
            CurrentLimit = 0;
        }

        public bool Alive
        {
            get
            {
                if (_h == IntPtr.Zero) return false;
                try { return !Process.GetProcessById(Pid).HasExited; }
                catch { return false; }
            }
        }

        public bool ApplyWorkingSet(long bytes, out string err)
        {
            err = null;
            if (_h == IntPtr.Zero) { err = "未连接进程"; return false; }
            if (ReadOnly) { err = "权限不足，请以管理员身份运行"; return false; }
            if (bytes < 64L << 20) bytes = 64L << 20;
            if (!Native.SetProcessWorkingSetSizeEx(_h, (IntPtr)1, (IntPtr)bytes, Native.QUOTA_HARDWS_MAX_ENABLE))
            {
                err = "设置工作集上限失败：" + Native.Err();
                return false;
            }
            CurrentLimit = bytes;
            return true;
        }

        public bool ReleaseWorkingSet(long fallbackBytes, out string err)
        {
            err = null;
            if (_h == IntPtr.Zero) { err = "未连接进程"; return false; }
            if (ReadOnly) { err = "权限不足"; return false; }
            long top = 1L << 40; // 1 TB，等于不设限
            if (!Native.SetProcessWorkingSetSizeEx(_h, (IntPtr)1, (IntPtr)top, Native.QUOTA_HARDWS_MAX_ENABLE))
            {
                if (!Native.SetProcessWorkingSetSizeEx(_h, (IntPtr)1, (IntPtr)top, Native.QUOTA_HARDWS_MAX_DISABLE))
                {
                    err = "解除工作集上限失败：" + Native.Err();
                    return false;
                }
            }
            CurrentLimit = 0;
            return true;
        }

        public bool Trim(out string err)
        {
            err = null;
            if (_h == IntPtr.Zero) { err = "未连接进程"; return false; }
            if (!Native.EmptyWorkingSet(_h)) { err = "回收失败：" + Native.Err(); return false; }
            return true;
        }

        public bool ApplyCommitCap(long bytes, out string err)
        {
            err = null;
            if (_h == IntPtr.Zero) { err = "未连接进程"; return false; }
            if (ReadOnly) { err = "权限不足，请以管理员身份运行"; return false; }
            ReleaseCommitCap();
            IntPtr job = Native.CreateJobObjectW(IntPtr.Zero, null);
            if (job == IntPtr.Zero) { err = "创建作业对象失败：" + Native.Err(); return false; }
            Native.JOBOBJ_EXT ext = new Native.JOBOBJ_EXT();
            ext.Basic.LimitFlags = Native.JOB_OBJECT_LIMIT_PROCESS_MEMORY;
            ext.ProcessMemoryLimit = (UIntPtr)(ulong)bytes;
            int size = Marshal.SizeOf(typeof(Native.JOBOBJ_EXT));
            IntPtr mem = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(ext, mem, false);
                if (!Native.SetInformationJobObject(job, Native.JobObjectExtendedLimitInformation, mem, size))
                {
                    err = "设置作业内存上限失败：" + Native.Err();
                    Native.CloseHandle(job);
                    return false;
                }
            }
            finally { Marshal.FreeHGlobal(mem); }

            if (!Native.AssignProcessToJobObject(job, _h))
            {
                err = "把进程加入作业失败：" + Native.Err() + "（部分受保护进程不允许）";
                Native.CloseHandle(job);
                return false;
            }
            _job = job;
            CommitCapActive = true;
            return true;
        }

        public void ReleaseCommitCap()
        {
            if (_job != IntPtr.Zero)
            {
                // 关掉作业句柄即销毁作业，进程自身的提交上限随之解除
                Native.CloseHandle(_job);
                _job = IntPtr.Zero;
            }
            CommitCapActive = false;
        }

        public bool SetMemoryPriority(uint prio, out string err)
        {
            err = null;
            if (_h == IntPtr.Zero) { err = "未连接进程"; return false; }
            int v = (int)prio;
            if (!Native.SetProcessInformation(_h, 0, ref v, 4)) { err = "设置内存优先级失败：" + Native.Err(); return false; }
            return true;
        }

        public ProcSample Sample()
        {
            ProcSample s = new ProcSample();
            if (_h == IntPtr.Zero) { s.Error = "未连接进程"; return s; }
            Native.PMC_EX pm = new Native.PMC_EX();
            pm.cb = Marshal.SizeOf(typeof(Native.PMC_EX));
            if (!Native.GetProcessMemoryInfo(_h, ref pm, pm.cb)) { s.Error = "读取失败：" + Native.Err(); return s; }
            s.WorkingSet = pm.WorkingSetSize.ToInt64();
            s.Commit = pm.PagefileUsage.ToInt64();
            s.Private = pm.PrivateUsage.ToInt64();
            s.PeakWorkingSet = pm.PeakWorkingSetSize.ToInt64();
            s.PeakCommit = pm.PeakPagefileUsage.ToInt64();
            int hc;
            if (Native.GetProcessHandleCount(_h, out hc)) s.Handles = hc;
            long c, e, k, u;
            if (Native.GetProcessTimes(_h, out c, out e, out k, out u))
            {
                DateTime now = DateTime.Now;
                if (_lastCpuTime != DateTime.MinValue)
                {
                    double ms = (now - _lastCpuTime).TotalMilliseconds;
                    if (ms > 50)
                    {
                        double used = ((k - _lastKernel) + (u - _lastUser)) / 10000.0;
                        s.Cpu = used / ms * 100.0;
                    }
                }
                _lastKernel = k; _lastUser = u; _lastCpuTime = now;
            }
            try { s.Threads = Process.GetProcessById(Pid).Threads.Count; } catch { }
            return s;
        }

        public void Dispose() { Detach(); }
    }

    // ------------------------------------------------------------------
    // 深度体检
    // ------------------------------------------------------------------
    public class MemClass
    {
        public string Kind;
        public string Name;
        public long Commit;      // 真实提交（文件映射计 0）
        public long Size;        // 地址空间大小
        public long WorkingSet;
        public int Regions;
    }

    public class ModuleInfo
    {
        public string Name;
        public string Path;
        public ulong Base;
        public ulong Size;
        public long Commit;
        public long WorkingSet;
    }

    public class Snapshot
    {
        public int Pid;
        public string Name;
        public string Path;
        public long WorkingSet, Commit, Private, PeakWorkingSet, PeakCommit;
        public bool Partial;
        public string Error;
        public List<MemClass> Classes = new List<MemClass>();
        public List<ModuleInfo> Modules = new List<ModuleInfo>();
        public List<string> Notes = new List<string>();
        public DateTime Time = DateTime.Now;

        public long ClassCommit(string kind)
        {
            long t = 0;
            foreach (MemClass c in Classes) if (c.Kind == kind) t += c.Commit;
            return t;
        }
        public long ClassWs(string kind)
        {
            long t = 0;
            foreach (MemClass c in Classes) if (c.Kind == kind) t += c.WorkingSet;
            return t;
        }
        public MemClass Top(string kind)
        {
            MemClass best = null;
            foreach (MemClass c in Classes) if (c.Kind == kind && (best == null || c.WorkingSet > best.WorkingSet)) best = c;
            return best;
        }

        public string Headline()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("进程：" + Name + "（PID " + Pid + "）");
            if (!string.IsNullOrEmpty(Path)) sb.AppendLine("路径：" + Path);
            sb.AppendLine("工作集：" + Native.Fmt(WorkingSet) + "　　提交：" + Native.Fmt(Commit) + "　　私有提交：" + Native.Fmt(Private));
            sb.AppendLine("峰值：" + Native.Fmt(PeakWorkingSet) + " 工作集 / " + Native.Fmt(PeakCommit) + " 提交");
            return sb.ToString();
        }

        public string Analysis()
        {
            StringBuilder sb = new StringBuilder();
            long imgWs = ClassWs("映像");
            long mapWs = ClassWs("映射文件");
            long privWs = ClassWs("私有");
            long totalWs = Math.Max(1, imgWs + mapWs + privWs);
            long imgCom = ClassCommit("映像");
            long mapCom = ClassCommit("映射文件");
            long privCom = ClassCommit("私有");

            sb.AppendLine("【内存构成】按常驻内存（任务管理器里的“内存”）拆开看：");
            sb.AppendLine("  映射资源文件　" + Native.Fmt(mapWs) + "　" + Pct(mapWs, totalWs));
            sb.AppendLine("  私有堆与栈　　" + Native.Fmt(privWs) + "　" + Pct(privWs, totalWs));
            sb.AppendLine("  模块代码映像　" + Native.Fmt(imgWs) + "　" + Pct(imgWs, totalWs));

            MemClass topMap = Top("映射文件");
            if (topMap != null && topMap.WorkingSet > 64L << 20)
                sb.AppendLine("  最大映射：" + ShortName(topMap.Name) + "（" + topMap.Regions + " 个映射区，常驻 " +
                              Native.Fmt(topMap.WorkingSet) + "，映射大小 " + Native.Fmt(topMap.Size) + "）");
            MemClass topMod = Top("映像");
            if (topMod != null && topMod.WorkingSet > 32L << 20)
                sb.AppendLine("  最大模块：" + topMod.Name + "（常驻 " + Native.Fmt(topMod.WorkingSet) + "，提交 " + Native.Fmt(topMod.Commit) + "）");

            sb.AppendLine();
            sb.AppendLine("【提交 vs 常驻】");
            sb.AppendLine("  提交 " + Native.Fmt(Commit) + "，常驻 " + Native.Fmt(WorkingSet));
            if (WorkingSet > Commit + (64L << 20))
                sb.AppendLine("  常驻比提交还大：多出来的这部分是文件映射页（.resS/.assets 之类），它们只占物理内存、");
            else if (Commit - WorkingSet > (256L << 20))
                sb.AppendLine("  有 " + Native.Fmt(Commit - WorkingSet) + " 已提交但不在物理内存里——系统回收过，进程却还占着地址空间。");
            else
                sb.AppendLine("  提交与常驻基本持平，页面几乎全在物理内存里，没有回收余地。");
            if (WorkingSet > Commit + (64L << 20))
                sb.AppendLine("  不进页面文件，用完就能被系统直接丢掉——这也是压上限最安全、最有效的一块。");

            sb.AppendLine();
            sb.AppendLine("【判断】");
            bool unity = IsUnity;
            if (mapWs > totalWs * 0.35)
            {
                sb.AppendLine("· 资源映射是最大头。Unity 把 .resS/.assets 直接映射进地址空间，被读过的页面就计入工作集；");
                sb.AppendLine("  资源卸载只在内存告急时才发生，于是“玩得越久、去过的场景越多，内存越高”。");
                sb.AppendLine("  " + Pct(mapWs, totalWs) + " 的常驻来自这里，它是可以被系统直接丢弃的干净页，压缩代价最低。");
            }
            if (privWs > totalWs * 0.30)
            {
                if (unity)
                {
                    sb.AppendLine("· 私有堆占了 " + Pct(privWs, totalWs) + "。Unity 6 的 IL2CPP 用 Boehm 保守式 GC：只标记不压缩、");
                    sb.AppendLine("  堆只涨不缩，峰值过去后内存也不还给系统；资源解压缓冲、渲染中间数据同样留在这里。");
                }
                else
                {
                    sb.AppendLine("· 私有堆占了 " + Pct(privWs, totalWs) + "，这是进程自己 new/alloc 出来、");
                    sb.AppendLine("  释放后通常也不立刻归还系统的部分。");
                }
            }
            if (imgWs > totalWs * 0.40)
            {
                sb.AppendLine("· 代码与驱动映像占了 " + Pct(imgWs, totalWs) + "，主要是显卡驱动、CEF/浏览器内核这类大 DLL；");
                sb.AppendLine("  这些页大多是只读共享页，压缩它意义不大，还会拖慢渲染。");
            }
            if (unity && privWs > 1L << 30)
                sb.AppendLine("· 进程内还注入了一整套 .NET 运行时（mod 宿主），托管堆和 JIT 代码也计在私有堆里。");

            sb.AppendLine();
            sb.AppendLine("【建议】");
            long rec = SuggestLimit();
            sb.AppendLine("  现在常驻 " + Native.Fmt(WorkingSet) + "。把上限设在 " + Native.Fmt(rec) + " 左右比较稳：");
            sb.AppendLine("  既能装下当前的热数据，又能在超出的瞬间逼系统把冷页面丢掉。");
            sb.AppendLine("  压到 " + Native.Fmt((long)(rec * 0.55)) + " 以下就会开始频繁缺页：进场景卡顿、贴图变糊、切图变慢。");
            if (WorkingSet * 1.3 > rec)
                sb.AppendLine("  设到 " + Native.Fmt((long)(WorkingSet * 1.3)) + " 以上，基本等于没限制。");

            if (Notes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("【线索】");
                foreach (string n in Notes) sb.AppendLine("· " + n);
            }
            if (Partial)
            {
                sb.AppendLine();
                sb.AppendLine("（部分区域无法读取，统计为下限；以管理员身份运行结果更完整。）");
            }
            return sb.ToString();
        }

        public bool IsUnity
        {
            get
            {
                foreach (ModuleInfo m in Modules)
                {
                    string n = (m.Name ?? "").ToLowerInvariant();
                    if (n == "unityplayer.dll" || n == "gameassembly.dll") return true;
                }
                return (Name ?? "").ToLowerInvariant().Contains("deskrawl");
            }
        }

        public long SuggestLimit()
        {
            double v = WorkingSet * 0.82;
            if (v < 768.0 * 1048576.0) v = 768.0 * 1048576.0;
            long step = 128L << 20;
            long q = (long)Math.Round(v / step) * step;
            return q;
        }

        private static string ShortName(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int i = s.LastIndexOf('\\');
            return i >= 0 ? s.Substring(i + 1) : s;
        }

        private static string Pct(long part, long total)
        {
            return (part * 100.0 / total).ToString("0.0") + "%";
        }

        public string ToReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Deskrawl 内存体检报告");
            sb.AppendLine("生成时间：" + Time.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine(new string('-', 68));
            sb.Append(Headline());
            sb.AppendLine(new string('-', 68));
            sb.Append(Analysis());
            sb.AppendLine(new string('-', 68));
            sb.AppendLine("【明细：按常驻内存排序】");
            sb.AppendLine(Pad("类别", 10) + Pad("名称", 46) + PadLeft("常驻", 12) + PadLeft("提交", 12) + PadLeft("大小", 12));
            List<MemClass> copy = new List<MemClass>(Classes);
            copy.Sort(delegate(MemClass a, MemClass b) { return b.WorkingSet.CompareTo(a.WorkingSet); });
            int shown = 0;
            foreach (MemClass c in copy)
            {
                if (shown++ >= 40) break;
                sb.AppendLine(Pad(c.Kind, 10) + Pad(Shorten(c.Name, 44), 46) + PadLeft(Native.Fmt(c.WorkingSet), 12) +
                              PadLeft(Native.Fmt(c.Commit), 12) + PadLeft(Native.Fmt(c.Size), 12));
            }
            sb.AppendLine();
            sb.AppendLine("【模块明细】");
            sb.AppendLine(Pad("模块", 46) + PadLeft("常驻", 12) + PadLeft("提交", 12));
            List<ModuleInfo> mods = new List<ModuleInfo>(Modules);
            mods.Sort(delegate(ModuleInfo a, ModuleInfo b) { return b.WorkingSet.CompareTo(a.WorkingSet); });
            shown = 0;
            foreach (ModuleInfo m in mods)
            {
                if (shown++ >= 25) break;
                sb.AppendLine(Pad(Shorten(m.Name, 44), 46) + PadLeft(Native.Fmt(m.WorkingSet), 12) + PadLeft(Native.Fmt(m.Commit), 12));
            }
            return sb.ToString();
        }

        private static string Pad(string s, int n)
        {
            if (s == null) s = "";
            int w = 0;
            foreach (char c in s) w += c < 128 ? 1 : 2;
            StringBuilder b = new StringBuilder(s);
            for (int i = w; i < n; i++) b.Append(' ');
            return b.ToString();
        }
        private static string PadLeft(string s, int n)
        {
            if (s == null) s = "";
            StringBuilder b = new StringBuilder();
            int w = s.Length;
            for (int i = w; i < n; i++) b.Append(' ');
            b.Append(s);
            return b.ToString();
        }
        private static string Shorten(string s, int n)
        {
            if (s == null) return "";
            if (s.Length <= n) return s;
            return "…" + s.Substring(s.Length - n + 1);
        }
    }

    public static class Inspector
    {
        private class Region
        {
            public ulong Base;
            public ulong Size;
            public uint Type;
            public string Kind;
            public string Name;
            public long Ws;
        }

        public static Snapshot Run(int pid)
        {
            Snapshot s = new Snapshot();
            s.Pid = pid;
            IntPtr h = Native.OpenProcess(Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_VM_READ, false, pid);
            if (h == IntPtr.Zero)
            {
                s.Error = "无法打开进程进行体检：" + Native.Err();
                return s;
            }
            try
            {
                try
                {
                    Process p = Process.GetProcessById(pid);
                    s.Name = p.ProcessName + ".exe";
                    try { s.Path = p.MainModule.FileName; } catch { }
                }
                catch { s.Name = "PID " + pid; }

                Native.PMC_EX pm = new Native.PMC_EX();
                pm.cb = Marshal.SizeOf(typeof(Native.PMC_EX));
                if (Native.GetProcessMemoryInfo(h, ref pm, pm.cb))
                {
                    s.WorkingSet = pm.WorkingSetSize.ToInt64();
                    s.Commit = pm.PagefileUsage.ToInt64();
                    s.Private = pm.PrivateUsage.ToInt64();
                    s.PeakWorkingSet = pm.PeakWorkingSetSize.ToInt64();
                    s.PeakCommit = pm.PeakPagefileUsage.ToInt64();
                }

                List<ModuleInfo> mods = ReadModules(h);
                s.Modules = mods;

                List<Region> regions = Walk(h, mods, s);
                if (regions == null) { s.Partial = true; return s; }

                bool wsOk = ReadWorkingSet(h, regions, s);
                if (!wsOk) s.Partial = true;

                Aggregate(s, regions);
                BuildNotes(s);
                return s;
            }
            finally { Native.CloseHandle(h); }
        }

        private static List<ModuleInfo> ReadModules(IntPtr h)
        {
            List<ModuleInfo> list = new List<ModuleInfo>();
            IntPtr[] buf = new IntPtr[2048];
            int needed;
            if (!Native.EnumProcessModulesEx(h, buf, buf.Length * IntPtr.Size, out needed, Native.LIST_MODULES_ALL))
                return list;
            int count = needed / IntPtr.Size;
            if (count > buf.Length) count = buf.Length;
            for (int i = 0; i < count; i++)
            {
                Native.MODULEINFO mi;
                if (!Native.GetModuleInformation(h, buf[i], out mi, Marshal.SizeOf(typeof(Native.MODULEINFO)))) continue;
                StringBuilder sb = new StringBuilder(1024);
                string path = null;
                if (Native.GetModuleFileNameExW(h, buf[i], sb, sb.Capacity) > 0) path = sb.ToString();
                ModuleInfo m = new ModuleInfo();
                m.Base = (ulong)mi.lpBaseOfDll.ToInt64();
                m.Size = mi.SizeOfImage;
                m.Path = path;
                m.Name = string.IsNullOrEmpty(path) ? ("模块@" + m.Base.ToString("X")) : System.IO.Path.GetFileName(path);
                list.Add(m);
            }
            list.Sort(delegate(ModuleInfo a, ModuleInfo b) { return a.Base.CompareTo(b.Base); });
            return list;
        }

        private static List<Region> Walk(IntPtr h, List<ModuleInfo> mods, Snapshot s)
        {
            List<Region> list = new List<Region>();
            ulong addr = 0;
            ulong limit = 0x7FFFFFFF0000UL;
            int guard = 0;
            Native.MEMORY_BASIC_INFORMATION mbi;
            int mbiSize = Marshal.SizeOf(typeof(Native.MEMORY_BASIC_INFORMATION));
            while (addr < limit && guard++ < 4000000)
            {
                UIntPtr got = Native.VirtualQueryEx(h, (IntPtr)(long)addr, out mbi, (IntPtr)mbiSize);
                if (got == UIntPtr.Zero) break;
                ulong size = mbi.RegionSize;
                if (size == 0) break;
                if (mbi.State == Native.MEM_COMMIT && (mbi.Protect & 0x100) == 0) // 排除 PAGE_GUARD
                {
                    Region r = new Region();
                    r.Base = mbi.BaseAddress;
                    r.Size = size;
                    r.Type = mbi.Type;
                    if (mbi.Type == Native.MEM_IMAGE)
                    {
                        r.Kind = "映像";
                        ModuleInfo m = FindModule(mods, mbi.BaseAddress);
                        r.Name = m != null ? m.Name : ("映像@" + mbi.BaseAddress.ToString("X"));
                    }
                    else if (mbi.Type == Native.MEM_MAPPED)
                    {
                        r.Kind = "映射文件";
                        string fn = MappedName(h, mbi.BaseAddress);
                        r.Name = string.IsNullOrEmpty(fn) ? "匿名映射" : fn;
                    }
                    else
                    {
                        r.Kind = "私有";
                        r.Name = "私有堆 / 栈 / JIT";
                    }
                    list.Add(r);
                }
                addr = mbi.BaseAddress + size;
                if (addr <= mbi.BaseAddress) break;
            }
            return list;
        }

        private static ModuleInfo FindModule(List<ModuleInfo> mods, ulong addr)
        {
            int lo = 0, hi = mods.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                ModuleInfo m = mods[mid];
                if (addr < m.Base) hi = mid - 1;
                else if (addr >= m.Base + m.Size) lo = mid + 1;
                else return m;
            }
            return null;
        }

        private static string MappedName(IntPtr h, ulong addr)
        {
            StringBuilder sb = new StringBuilder(2048);
            int n = Native.GetMappedFileNameW(h, (IntPtr)(long)addr, sb, sb.Capacity);
            if (n <= 0) return null;
            return DosToWin32(sb.ToString());
        }

        private static Dictionary<string, string> _devices;

        public static string DosToWin32(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length > 1 && path[1] == ':') return path;
            if (_devices == null)
            {
                _devices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                StringBuilder target = new StringBuilder(1024);
                for (char c = 'A'; c <= 'Z'; c++)
                {
                    string dev = c + ":";
                    if (Native.QueryDosDeviceW(dev, target, target.Capacity) != false && target.Length > 0)
                    {
                        string t = target.ToString();
                        int idx = t.IndexOf('\0');
                        if (idx >= 0) t = t.Substring(0, idx);
                        if (!_devices.ContainsKey(t)) _devices[t] = dev + "\\";
                    }
                }
            }
            foreach (KeyValuePair<string, string> kv in _devices)
            {
                if (path.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value + path.Substring(kv.Key.Length).TrimStart('\\');
            }
            return path;
        }

        private static bool ReadWorkingSet(IntPtr h, List<Region> regions, Snapshot s)
        {
            long total = 0;
            foreach (Region r in regions) total += (long)r.Size;
            if (total > 48L << 30) return false;

            const int Chunk = 131072;
            Native.WSW_EX[] arr = new Native.WSW_EX[Chunk];
            List<ulong> pages = new List<ulong>(Chunk);
            foreach (Region r in regions)
            {
                ulong end = r.Base + r.Size;
                for (ulong a = r.Base; a < end; a += 4096)
                {
                    pages.Add(a);
                    if (pages.Count == Chunk) { Flush(h, arr, pages, regions); pages.Clear(); }
                }
            }
            if (pages.Count > 0) Flush(h, arr, pages, regions);
            return true;
        }

        private static void Flush(IntPtr h, Native.WSW_EX[] arr, List<ulong> pages, List<Region> regions)
        {
            int n = pages.Count;
            for (int i = 0; i < n; i++)
            {
                arr[i].VirtualAddress = (IntPtr)(long)pages[i];
                arr[i].VirtualAttributes = UIntPtr.Zero;
            }
            int cb = n * Marshal.SizeOf(typeof(Native.WSW_EX));
            if (!Native.QueryWorkingSetEx(h, arr, cb)) return;
            for (int i = 0; i < n; i++)
            {
                ulong attr = (ulong)arr[i].VirtualAttributes;
                if ((attr & 1) == 0) continue;   // Valid 位
                Region r = FindRegion(regions, pages[i]);
                if (r != null) r.Ws += 4096;
            }
        }

        private static Region FindRegion(List<Region> regions, ulong addr)
        {
            int lo = 0, hi = regions.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                Region r = regions[mid];
                if (addr < r.Base) hi = mid - 1;
                else if (addr >= r.Base + r.Size) lo = mid + 1;
                else return r;
            }
            return null;
        }

        private static void Aggregate(Snapshot s, List<Region> regions)
        {
            Dictionary<string, MemClass> map = new Dictionary<string, MemClass>();
            foreach (Region r in regions)
            {
                string key = r.Kind + "|" + r.Name;
                MemClass c;
                if (!map.TryGetValue(key, out c))
                {
                    c = new MemClass();
                    c.Kind = r.Kind;
                    c.Name = r.Name;
                    map[key] = c;
                }
                c.Commit += (r.Kind == "映射文件" && r.Name != "匿名映射") ? 0 : (long)r.Size;
                c.Size += (long)r.Size;
                c.WorkingSet += r.Ws;
                c.Regions++;
            }
            foreach (MemClass c in map.Values)
            {
                if (c.Kind == "私有") continue;
                s.Classes.Add(c);
            }
            // 私有区域合并成一条，避免几千个碎片
            MemClass priv = new MemClass();
            priv.Kind = "私有";
            priv.Name = "私有堆 / 栈 / JIT";
            foreach (MemClass c in map.Values)
            {
                if (c.Kind != "私有") continue;
                priv.Commit += c.Commit;
                priv.Size += c.Size;
                priv.WorkingSet += c.WorkingSet;
                priv.Regions += c.Regions;
            }
            s.Classes.Add(priv);

            foreach (ModuleInfo m in s.Modules)
            {
                MemClass c;
                if (map.TryGetValue("映像|" + m.Name, out c))
                {
                    m.Commit = c.Commit;
                    m.WorkingSet = c.WorkingSet;
                }
            }
        }

        private static void BuildNotes(Snapshot s)
        {
            foreach (ModuleInfo m in s.Modules)
            {
                string n = (m.Name ?? "").ToLowerInvariant();
                if (n == "coreclr.dll" || n == "clrjit.dll")
                    s.Notes.Add("进程内加载了 .NET 运行时（" + m.Name + "，约 " + Native.Fmt(m.Commit) + "），来自 mod 注入宿主，会额外占用托管堆与 JIT 内存。");
                if (n == "gameassembly.dll")
                    s.Notes.Add("GameAssembly.dll 是 IL2CPP 编译出的原生代码，占 " + Native.Fmt(m.Commit) + "，属于只读映像，压不动。");
                if (n == "unityplayer.dll")
                    s.Notes.Add("UnityPlayer.dll 占 " + Native.Fmt(m.Commit) + "。");
            }
            foreach (MemClass c in s.Classes)
            {
                if (c.Kind == "映射文件" && c.Commit > 512L << 20)
                    s.Notes.Add("映射资源 " + System.IO.Path.GetFileName(c.Name) + " 达 " + Native.Fmt(c.Commit) + "。");
            }
        }
    }
}
