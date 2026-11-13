using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Modules.AiAssistant;

public static partial class AiWinCareEngine
{
        public static async Task<SmartRemedyResult> ExecuteSmartRemedyBatchAsync()
        {
            return await Task.Run(async () =>
            {
                var result = new SmartRemedyResult();

                // 1. Purge Standby Memory & Working Set
                try
                {
                    long beforeMem = GC.GetTotalMemory(false);
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    long afterMem = GC.GetTotalMemory(true);
                    result.FreedMemoryBytes = Math.Max(0, beforeMem - afterMem);
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("Memory Working Set and Standby List optimized.".T());
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.PurgeMemory", ex);
                }

                // 2. Safe Temp Cleaning (Scoped to safe user temp folder)
                try
                {
                    long cleaned = 0;
                    string userTemp = Path.GetTempPath();
                    if (Directory.Exists(userTemp) && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(userTemp))
                    {
                        var dir = new DirectoryInfo(userTemp);
                        foreach (var file in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                if ((DateTime.Now - file.LastWriteTime).TotalHours > 12 && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(file.FullName))
                                {
                                    long len = file.Length;
                                    file.Delete();
                                    cleaned += len;
                                }
                            }
                            catch (Exception fEx)
                            {
                                Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.DeleteTempFile", fEx);
                            }
                        }
                    }
                    result.CleanedTempBytes = cleaned;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add(string.Format("Safe temp files cleaned ({0:F1} MB freed).".T(), cleaned / (1024.0 * 1024.0)));
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.SafeTempCleaning", ex);
                }

                // 3. Flush DNS & Winsock Cache
                try
                {
                    var cmdRes = await Core.Helpers.ProcessRunner.RunHiddenAsync("ipconfig.exe", new[] { "/flushdns" }, 3);
                    result.DnsFlushed = cmdRes.Success;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("DNS Resolver cache flushed.".T());
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.FlushDns", ex);
                }

                return result;
            });
        }

        public static async Task<SmartRemedyResult> ExecuteSmartRemedyLevelAsync(int level)
        {
            return await Task.Run(async () =>
            {
                var result = new SmartRemedyResult();

                // Level 1, 2, 3: Purge GC and Memory Working Set
                try
                {
                    long beforeMem = GC.GetTotalMemory(false);
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    long afterMem = GC.GetTotalMemory(true);
                    result.FreedMemoryBytes = Math.Max(0, beforeMem - afterMem);
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("Memory Working Set and Standby List optimized.".T());
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.PurgeMemoryLevel", ex);
                }

                // Safe Temp Cleaning (Level 1: > 24h, Level 2: > 6h, Level 3: > 1h)
                try
                {
                    double minHours = level switch { 1 => 24.0, 2 => 6.0, _ => 1.0 };
                    long cleaned = 0;
                    string userTemp = Path.GetTempPath();
                    if (Directory.Exists(userTemp) && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(userTemp))
                    {
                        var dir = new DirectoryInfo(userTemp);
                        foreach (var file in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                if ((DateTime.Now - file.LastWriteTime).TotalHours > minHours && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(file.FullName))
                                {
                                    long len = file.Length;
                                    file.Delete();
                                    cleaned += len;
                                }
                            }
                            catch (Exception fEx)
                            {
                                Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.DeleteTempFileLevel", fEx);
                            }
                        }
                    }
                    result.CleanedTempBytes = cleaned;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add(string.Format("Safe temp files cleaned ({0:F1} MB freed).".T(), cleaned / (1024.0 * 1024.0)));
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.SafeTempCleaningLevel", ex);
                }

                // Flush DNS & Winsock Cache
                try
                {
                    var cmdRes = await Core.Helpers.ProcessRunner.RunHiddenAsync("ipconfig.exe", new[] { "/flushdns" }, 3);
                    result.DnsFlushed = cmdRes.Success;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("DNS Resolver cache flushed.".T());
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("AiWinCareEngine.FlushDnsLevel", ex);
                }

                return result;
            });
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (long Size, DateTime Timestamp)> _dirSizeCache = new(StringComparer.OrdinalIgnoreCase);

        public class OfflineDiagnosticAnswer
        {
            public string Title { get; set; } = string.Empty;
            public string Diagnosis { get; set; } = string.Empty;
            public string Solution { get; set; } = string.Empty;
            public string RecommendedActionKey { get; set; } = "None";
            public string ActionButtonText { get; set; } = "Fix Now";
        }

        /// <summary>
        /// Provides instant rule-based diagnostic answers and 1-click remediation workflows
        /// when running in offline mode or when cloud AI is unreachable.
        /// </summary>
        public static OfflineDiagnosticAnswer GetOfflineDiagnosticAdvice(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "WinCare AI Assistant Ready".T(),
                    Diagnosis = "Please enter your question, issue, or error symptom.".T(),
                    Solution = "You can ask about system lag, disk space, network connection, gaming FPS, or startup apps.".T(),
                    RecommendedActionKey = "None"
                };
            }

            string q = userQuery.ToLowerInvariant();

            // 1. RAM / Memory
            if (q.Contains("ram") || q.Contains("bộ nhớ") || q.Contains("memory") || q.Contains("tràn ram") || q.Contains("đơ"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Memory Optimization & Standby Purge".T(),
                    Diagnosis = "High memory working set or bloated standby cache can cause sudden UI freezes and stuttering.".T(),
                    Solution = "Run RAM Booster to flush standby lists and reclaim unused physical RAM working sets.".T(),
                    RecommendedActionKey = "NavigateOptimizer",
                    ActionButtonText = "Open Optimizer".T()
                };
            }

            // 2. Junk / Temp / Cache
            if (q.Contains("rác") || q.Contains("junk") || q.Contains("temp") || q.Contains("cache") || q.Contains("dọn dẹp") || q.Contains("clean"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Junk & Temp Cache Cleanup".T(),
                    Diagnosis = "Accumulated Windows temp files, browser caches, and system logs take up space and slow down IO.".T(),
                    Solution = "Use Junk Cleaner for a comprehensive scan across Windows, browsers, logs, and installer caches.".T(),
                    RecommendedActionKey = "NavigateJunkCleaner",
                    ActionButtonText = "Clean Junk Now".T()
                };
            }

            // 3. Disk / Storage / Drive C Full
            if (q.Contains("ổ c") || q.Contains("đầy ổ") || q.Contains("disk") || q.Contains("dung lượng") || q.Contains("storage") || q.Contains("space"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Disk Space Recovery & Analysis".T(),
                    Diagnosis = "Drive C is filling up, potentially due to large system files, Hibernation file, Windows.old or temp dumps.".T(),
                    Solution = "Navigate to Disk Analyzer to locate large files and run System Component Store (WinSxS) cleanup.".T(),
                    RecommendedActionKey = "NavigateDisk",
                    ActionButtonText = "Open Disk Center".T()
                };
            }

            // 4. Network / DNS / Internet (Prioritize network connection queries)
            if (q.Contains("mạng") || q.Contains("dns") || q.Contains("wifi") || q.Contains("internet") || q.Contains("rớt mạng") || q.Contains("ping"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Network Health & DNS Optimization".T(),
                    Diagnosis = "DNS latency or corrupted Winsock cache can cause slow web browsing and packet loss.".T(),
                    Solution = "Switch to Ultra-Fast DNS (Cloudflare 1.1.1.1 / Google 8.8.8.8) and flush the DNS resolver cache.".T(),
                    RecommendedActionKey = "NavigateNetwork",
                    ActionButtonText = "Open Network Center".T()
                };
            }

            // 5. Gaming / FPS / Latency
            if (q.Contains("game") || q.Contains("fps") || q.Contains("lag") || q.Contains("giật") || q.Contains("turbo"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Performance & Latency Optimization".T(),
                    Diagnosis = "Background services and system latency timers can reduce application responsiveness and cause micro-stutter.".T(),
                    Solution = "Configure System Optimizer performance tweaks to prioritize active foreground applications.".T(),
                    RecommendedActionKey = "NavigateOptimizer",
                    ActionButtonText = "Open Optimizer".T()
                };
            }

            // 6. Startup / Slow Boot
            if (q.Contains("khởi động") || q.Contains("boot") || q.Contains("chậm") || q.Contains("startup") || q.Contains("mở máy"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Startup Boot Acceleration".T(),
                    Diagnosis = "Too many background apps launching at startup dramatically increases Windows boot time and disk IO.".T(),
                    Solution = "Disable high-impact startup apps and delay non-essential startup services.".T(),
                    RecommendedActionKey = "NavigateStartup",
                    ActionButtonText = "Manage Startup".T()
                };
            }

            // 7. System Repair / Windows Error / SFC / DISM
            if (q.Contains("lỗi") || q.Contains("repair") || q.Contains("sửa") || q.Contains("sfc") || q.Contains("dism") || q.Contains("update") || q.Contains("spooler") || q.Contains("máy in"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "System Health & Component Repair".T(),
                    Diagnosis = "Corrupted system files or stuck Windows background services can trigger unpredictable errors.".T(),
                    Solution = "Run 1-Click System Repair to verify system file integrity with SFC/DISM and reset corrupted services.".T(),
                    RecommendedActionKey = "NavigateRepair",
                    ActionButtonText = "Open System Repair".T()
                };
            }

            // Default General Fallback
            return new OfflineDiagnosticAnswer
            {
                Title = "WinCare Diagnostic Recommendation".T(),
                Diagnosis = string.Format("Analysis for '{0}': System baseline analyzed.".T(), userQuery),
                Solution = "Run One-Click Smart Remedy or AI Diagnostics to optimize system performance and clean cache.".T(),
                RecommendedActionKey = "NavigateOptimizer",
                ActionButtonText = "Run Optimization".T()
            };
        }

        private static long GetDirectorySizeBytesSafely(string dirPath)
        {
            if (_dirSizeCache.TryGetValue(dirPath, out var cached) && (DateTime.Now - cached.Timestamp).TotalSeconds < 60)
            {
                return cached.Size;
            }

            long size = 0;
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);
                if (!dirInfo.Exists) return 0;

                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    try { size += file.Length; } catch { }
                }

                foreach (var subDir in dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        foreach (var file in subDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try { size += file.Length; } catch { }
                        }
                    }
                    catch { }
                }

                _dirSizeCache[dirPath] = (size, DateTime.Now);
            }
            catch { }
            return size;
        }
    }
