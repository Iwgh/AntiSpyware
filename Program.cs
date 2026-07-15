using System;
using Microsoft.Win32;
using System.Security.Principal;
using System.Diagnostics;

namespace RegistryHelper
{
    class Program
    {
        // =========================================================================
        // [설정] 원하는 레지스트리 경로
        // =========================================================================
        private const string REG_PATH = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender";

        // 등록할 16진수 값 (0x0 = 10진수 0)
        private const uint HEX_VALUE = 0x0; 

        static void Main(string[] args)
        {
            // 프로그램 시작하자마자 전체 예외를 감시하여 무조건 꺼짐 방지
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n==================================================");
                Console.WriteLine("[치명적 오류] 예상치 못한 시스템 예외가 발생했습니다.");
                Console.WriteLine($"상세정보: {e.ExceptionObject}");
                Console.WriteLine("==================================================");
                Console.ResetColor();
                Console.WriteLine("\n아무 키나 누르면 종료합니다...");
                Console.ReadKey();
            };

            // 1. 관리자 권한 확인 및 자동 승인 요청 (UAC)
            if (!IsAdministrator())
            {
                ElevateToAdministrator();
                return; 
            }

            Console.WriteLine("==================================================");
            Console.WriteLine("      관리자 권한 레지스트리 자동 등록 프로그램");
            Console.WriteLine("==================================================");

            if (string.IsNullOrEmpty(REG_PATH) || REG_PATH.Contains("여기에"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[오류] REG_PATH에 올바른 경로를 먼저 입력해주세요.");
                Console.ResetColor();
                CloseProgram();
                return;
            }

            // 2. 레지스트리 키 생성 및 값 작성 자동 실행
            try
            {
                RegistryKey rootKey = Registry.LocalMachine; 
                string subKeyPath = REG_PATH;

                if (REG_PATH.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = Registry.LocalMachine;
                    subKeyPath = REG_PATH.Substring("HKEY_LOCAL_MACHINE\\".Length);
                }
                else if (REG_PATH.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = Registry.LocalMachine;
                    subKeyPath = REG_PATH.Substring("HKLM\\".Length);
                }
                else if (REG_PATH.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = Registry.CurrentUser;
                    subKeyPath = REG_PATH.Substring("HKEY_CURRENT_USER\\".Length);
                }
                else if (REG_PATH.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase))
                {
                    rootKey = Registry.CurrentUser;
                    subKeyPath = REG_PATH.Substring("HKCU\\".Length);
                }

                // 지정된 경로에 하위 키 생성 또는 열기
                using (RegistryKey key = rootKey.CreateSubKey(subKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key != null)
                    {
                        key.SetValue("DisablePROGRAM", HEX_VALUE, RegistryValueKind.DWord);
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[성공] 레지스트리 등록이 완료되었습니다!");
                        Console.WriteLine("==================================================");
                        Console.ResetColor();
                        Console.WriteLine($"경로: {rootKey}\\{subKeyPath}");
                        Console.WriteLine($"이름: DisablePROGRAM");
                        Console.WriteLine($"데이터: 0x{HEX_VALUE:X} ({HEX_VALUE})");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("==================================================");
                        Console.ResetColor();
                    }
                    else
                    {
                        throw new Exception("레지스트리 키를 생성하거나 열 수 없습니다. 권한이 부족할 수 있습니다.");
                    }
                }
            }
            catch (UnauthorizedAccessException uaeEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n==================================================");
                Console.WriteLine("[실패] 권한 오류 (UnauthorizedAccessException)");
                Console.WriteLine("윈도우 디펜더나 백신 실시간 감시가 접근을 차단했습니다.");
                Console.WriteLine($"상세 오류: {uaeEx.Message}");
                Console.WriteLine("==================================================");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n==================================================");
                Console.WriteLine($"[실패] 레지스트리 작성 중 에러가 발생했습니다.");
                Console.WriteLine($"에러 종류: {ex.GetType().Name}");
                Console.WriteLine($"에러 메시지: {ex.Message}");
                Console.WriteLine("==================================================");
                Console.ResetColor();
            }

            CloseProgram();
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void ElevateToAdministrator()
        {
            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas" 
            };

            try
            {
                Process.Start(proc);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"관리자 권한 실행 실패: {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        private static void CloseProgram()
        {
            Console.WriteLine("\n종료하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }
}
