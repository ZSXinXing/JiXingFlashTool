using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BuildRelease
{
    /// <summary>
    /// 发布打包工具入口，负责构建主程序、整理发布目录并调用 Inno Setup 生成安装包。
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 仓库根目录。
        /// </summary>
        private static readonly string RepoRoot = ResolveRepoRoot();

        /// <summary>
        /// 解决方案路径。
        /// </summary>
        private static readonly string SolutionPath = Path.Combine(RepoRoot, "JiXingFlashTool.sln");

        /// <summary>
        /// 主程序版本文件路径。
        /// </summary>
        private static readonly string AssemblyInfoPath = Path.Combine(RepoRoot, "JiXingFlashTool", "Properties", "AssemblyInfo.cs");

        /// <summary>
        /// 安装包脚本路径。
        /// </summary>
        private static readonly string InstallerScriptPath = Path.Combine(RepoRoot, "BuildRelease", "JiXingFlashTool.iss");

        /// <summary>
        /// 主程序图标路径。
        /// </summary>
        private static readonly string IconPath = Path.Combine(RepoRoot, "JiXingFlashTool", "logo.ico");

        /// <summary>
        /// 发布输出根目录。
        /// </summary>
        private static readonly string ReleaseOutputDirectory = Path.Combine(RepoRoot, "release_out");

        /// <summary>
        /// 发布暂存目录。
        /// </summary>
        private static readonly string StageDirectory = Path.Combine(ReleaseOutputDirectory, "stage");

        /// <summary>
        /// 当前构建配置。
        /// </summary>
        private static readonly string BuildConfiguration = ResolveBuildConfiguration();

        /// <summary>
        /// 程序入口。
        /// </summary>
        /// <returns>进程退出码。</returns>
        private static int Main()
        {
            try
            {
                string version = ResolveApplicationVersion();
                string outputBaseFileName = BuildOutputBaseFileName(version);
                string msbuildPath = ResolveMsBuildPath();
                string isccPath = ResolveIsccPath();

                PrepareDirectories();
                BuildSolution(msbuildPath);
                CopyBuildOutput();
                BuildInstaller(isccPath, version, outputBaseFileName);

                Console.WriteLine("BuildRelease completed successfully.");
                Console.WriteLine("Configuration : " + BuildConfiguration);
                Console.WriteLine("Version       : " + version);
                Console.WriteLine("Installer     : " + outputBaseFileName + ".exe");
                Console.WriteLine("OutputDir     : " + ReleaseOutputDirectory);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// 准备发布输出目录和暂存目录。
        /// </summary>
        private static void PrepareDirectories()
        {
            Directory.CreateDirectory(ReleaseOutputDirectory);
            if (Directory.Exists(StageDirectory))
            {
                Directory.Delete(StageDirectory, true);
            }

            Directory.CreateDirectory(StageDirectory);
        }

        /// <summary>
        /// 解析主程序版本号。
        /// </summary>
        /// <returns>版本号文本。</returns>
        private static string ResolveApplicationVersion()
        {
            string assemblyInfo = File.ReadAllText(AssemblyInfoPath);
            Match match = Regex.Match(assemblyInfo, "AssemblyFileVersion\\(\"([^\"]+)\"\\)");
            if (!match.Success)
            {
                match = Regex.Match(assemblyInfo, "AssemblyVersion\\(\"([^\"]+)\"\\)");
            }

            if (!match.Success)
            {
                throw new InvalidOperationException("Unable to resolve JiXingFlashTool version.");
            }

            string[] parts = match.Groups[1].Value
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(3)
                .ToArray();

            if (parts.Length < 3)
            {
                throw new InvalidOperationException("Application version must contain at least three numeric parts.");
            }

            return string.Join(".", parts);
        }

        /// <summary>
        /// 构建安装包基础文件名。
        /// </summary>
        /// <param name="version">版本号。</param>
        /// <returns>基础文件名。</returns>
        private static string BuildOutputBaseFileName(string version)
        {
            string buildDate = DateTime.Now.ToString("yyyyMMdd");
            return "JiXingFlashTool-" + version + "-" + buildDate;
        }

        /// <summary>
        /// 解析当前构建配置。
        /// </summary>
        /// <returns>构建配置名称。</returns>
        private static string ResolveBuildConfiguration()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        /// <summary>
        /// 解析 MSBuild 路径。
        /// </summary>
        /// <returns>MSBuild 可执行文件路径。</returns>
        private static string ResolveMsBuildPath()
        {
            string envPath = Environment.GetEnvironmentVariable("MSBUILD_EXE");
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            {
                return envPath;
            }

            List<string> candidates = new List<string>
            {
                @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
            };

            string vsWherePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio",
                "Installer",
                "vswhere.exe");

            if (File.Exists(vsWherePath))
            {
                string vsWhereResult = RunProcessForSingleLineOutput(
                    vsWherePath,
                    "-latest -products * -requires Microsoft.Component.MSBuild -find \"MSBuild\\**\\Bin\\MSBuild.exe\"");
                if (!string.IsNullOrWhiteSpace(vsWhereResult) && File.Exists(vsWhereResult))
                {
                    return vsWhereResult;
                }
            }

            string[] pathCandidates = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim().Trim('"'))
                .Where(Directory.Exists)
                .Select(item => Path.Combine(item, "MSBuild.exe"))
                .ToArray();
            candidates.AddRange(pathCandidates);

            string match = candidates.FirstOrDefault(File.Exists);
            if (match == null)
            {
                throw new FileNotFoundException("MSBuild.exe not found. Set MSBUILD_EXE or install Visual Studio Build Tools.");
            }

            return match;
        }

        /// <summary>
        /// 根据当前可执行文件位置向上查找仓库根目录。
        /// </summary>
        /// <returns>仓库根目录。</returns>
        private static string ResolveRepoRoot()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int depth = 0; depth < 10; depth++)
            {
                if (File.Exists(Path.Combine(current, "JiXingFlashTool.sln")))
                {
                    return current;
                }

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            throw new DirectoryNotFoundException("Unable to locate repository root (JiXingFlashTool.sln).");
        }

        /// <summary>
        /// 解析 Inno Setup 编译器路径。
        /// </summary>
        /// <returns>ISCC 可执行文件路径。</returns>
        private static string ResolveIsccPath()
        {
            string envPath = Environment.GetEnvironmentVariable("ISCC_EXE");
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            {
                return envPath;
            }

            string[] candidates =
            {
                @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
                @"C:\Program Files\Inno Setup 6\ISCC.exe",
                @"D:\Program Files (x86)\Inno Setup 6\ISCC.exe",
                @"D:\Program Files\Inno Setup 6\ISCC.exe"
            };

            string direct = candidates.FirstOrDefault(File.Exists);
            if (direct != null)
            {
                return direct;
            }

            foreach (string registryPath in new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            })
            {
                using (RegistryKey root = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (root == null)
                    {
                        continue;
                    }

                    foreach (string subKeyName in root.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = root.OpenSubKey(subKeyName))
                        {
                            string displayName = subKey?.GetValue("DisplayName") as string;
                            string installLocation = subKey?.GetValue("InstallLocation") as string;
                            if (string.IsNullOrWhiteSpace(displayName) ||
                                displayName.IndexOf("Inno Setup", StringComparison.OrdinalIgnoreCase) < 0 ||
                                string.IsNullOrWhiteSpace(installLocation))
                            {
                                continue;
                            }

                            string isccPath = Path.Combine(installLocation, "ISCC.exe");
                            if (File.Exists(isccPath))
                            {
                                return isccPath;
                            }
                        }
                    }
                }
            }

            throw new FileNotFoundException("ISCC.exe not found.");
        }

        /// <summary>
        /// 构建解决方案中的主程序项目。
        /// </summary>
        /// <param name="msbuildPath">MSBuild 路径。</param>
        private static void BuildSolution(string msbuildPath)
        {
            RunProcess(
                msbuildPath,
                "\"" + SolutionPath + "\" /t:JiXingFlashTool:Rebuild /p:Configuration=" + BuildConfiguration + " /p:Platform=\"Any CPU\" /m /nologo");
        }

        /// <summary>
        /// 复制主程序输出到暂存目录。
        /// </summary>
        private static void CopyBuildOutput()
        {
            string buildOutputDirectory = Path.Combine(RepoRoot, "JiXingFlashTool", "bin", BuildConfiguration);
            if (!Directory.Exists(buildOutputDirectory))
            {
                throw new DirectoryNotFoundException("Build output not found: " + buildOutputDirectory);
            }

            bool includeSymbols = string.Equals(BuildConfiguration, "Debug", StringComparison.OrdinalIgnoreCase);
            CopyDirectory(buildOutputDirectory, StageDirectory, includeSymbols);
        }

        /// <summary>
        /// 生成安装包。
        /// </summary>
        /// <param name="isccPath">ISCC 路径。</param>
        /// <param name="version">版本号。</param>
        /// <param name="outputBaseFileName">输出文件基础名。</param>
        private static void BuildInstaller(string isccPath, string version, string outputBaseFileName)
        {
            RunProcess(
                isccPath,
                "\"" + InstallerScriptPath + "\" " +
                "\"/DMyAppVersion=" + version + "\" " +
                "\"/DMyOutputBaseFilename=" + outputBaseFileName + "\" " +
                "\"/DSourceDir=" + StageDirectory + "\" " +
                "\"/DIconFile=" + IconPath + "\" " +
                "\"/DOutputDir=" + ReleaseOutputDirectory + "\"");
        }

        /// <summary>
        /// 递归复制目录。
        /// </summary>
        /// <param name="sourceDirectory">源目录。</param>
        /// <param name="destinationDirectory">目标目录。</param>
        /// <param name="includeSymbols">是否保留符号文件。</param>
        private static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool includeSymbols)
        {
            string[] excludedExtensions =
            {
                ".xml",
                ".log",
                ".bak"
            };

            Directory.CreateDirectory(destinationDirectory);

            foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
            }

            foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file);
                if (!includeSymbols && string.Equals(extension, ".pdb", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (excludedExtensions.Any(item => string.Equals(item, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string relative = file.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(destinationDirectory, relative);
                string targetDirectory = Path.GetDirectoryName(targetFile) ?? destinationDirectory;
                Directory.CreateDirectory(targetDirectory);
                File.Copy(file, targetFile, true);
            }
        }

        /// <summary>
        /// 运行外部进程并输出标准输出与错误输出。
        /// </summary>
        /// <param name="fileName">程序路径。</param>
        /// <param name="arguments">参数。</param>
        private static void RunProcess(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,
                WorkingDirectory = RepoRoot
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.Error.WriteLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(Path.GetFileName(fileName) + " failed with exit code " + process.ExitCode + ".");
                }
            }
        }

        /// <summary>
        /// 运行外部进程并获取第一行有效输出。
        /// </summary>
        /// <param name="fileName">程序路径。</param>
        /// <param name="arguments">参数。</param>
        /// <returns>第一行有效输出。</returns>
        private static string RunProcessForSingleLineOutput(string fileName, string arguments)
        {
            string[] lines = RunProcessForMultiLineOutput(fileName, arguments);
            return lines.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();
        }

        /// <summary>
        /// 运行外部进程并获取多行输出。
        /// </summary>
        /// <param name="fileName">程序路径。</param>
        /// <param name="arguments">参数。</param>
        /// <returns>输出行集合。</returns>
        private static string[] RunProcessForMultiLineOutput(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = RepoRoot
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
                {
                    return Array.Empty<string>();
                }

                return stdout
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToArray();
            }
        }
    }
}
