using System;
using Microsoft.Win32;
using System.Security.Principal;
using System.Diagnostics;

namespace RegistryHelper
{
    class Program
    {
        // =========================================================================
        // [★여기에 레지스트리 경로를 넣어주세요★]
        // 예 1: @"HKEY_LOCAL_MACHINE\SOFTWARE\MyTestApp"
        // 예 2: @"HKEY_CURRENT_USER\Software\MyTestApp"
        // (앞에 @를 붙여두시면 백슬래시(\)를 그대로 편하게 입력하실 수 있습니다.)
        // =========================================================================
        private const string REG_PATH = @"컴퓨터\HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender";

        // 등록할 16진수 값 (0x1 = 10진수 1로 고정)
        private const uint HEX_VALUE = 0x0; 

        static void Main(string[] args)
        {
            // 1. 관리자 권한 확인 및 자동 승인 요청 (UAC)
            if (!IsAdministrator())
            {
                ElevateToAdministrator();
                return; // 권한 상승 프로세스를 띄웠으므로 현재 일반 권한 프로세스는 종료
            }

            Console.WriteLine("==================================================");
            Console.WriteLine("      관리자 권한 레지스트리 자동 등록 프로그램");
            Console.WriteLine("==================================================");

            // 경로를 입력하지 않았거나 초기 상태일 때 예외 처리
            if (string.IsNullOrEmpty(REG_PATH) || REG_PATH == "여기에_원하는_경로를_붙여넣으세요")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[오류] 소스코드 상단의 'REG_PATH'에 경로를 먼저 입력해주세요!");
                Console.ResetColor();
                CloseProgram();
                return;
            }

            // 2. 레지스트리 키 생성 및 값 작성 자동 실행
            try
            {
                RegistryKey rootKey = Registry.LocalMachine; // 기본값: HKLM
                string subKeyPath = REG_PATH;

                // 입력된 경로를 분석하여 HKLM과 HKCU를 구분합니다.
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
                using (RegistryKey key = rootKey.CreateSubKey(subKeyPath))
                {
                    if (key != null)
                    {
                        // "DisablePROGRAM" 이라는 이름으로 DWORD(32비트) 16진수 값 0x1 저장
                        key.SetValue("DisablePROGRAM", HEX_VALUE, RegistryValueKind.DWord);
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[성공] 레지스트리 등록이 완료되었습니다!");
                        Console.WriteLine("==================================================");
                        Console.ResetColor();
                        Console.WriteLine($"경로: {rootKey}\\{subKeyPath}");
                        Console.WriteLine($"이름: DisablePROGRAM");
                        Console.WriteLine($"형식: REG_DWORD (32비트)");
                        Console.WriteLine($"데이터: 0x{HEX_VALUE:X} ({HEX_VALUE})");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("==================================================");
                        Console.ResetColor();
                    }
                    else
                    {
                        throw new Exception("레지스트리 키를 생성하거나 열 수 없습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n==================================================");
                Console.WriteLine($"[실패] 레지스트리 작성 중 에러가 발생했습니다.");
                Console.WriteLine($"에러 메시지: {ex.Message}");
                Console.WriteLine("==================================================");
                Console.ResetColor();
            }

            CloseProgram();
        }

        /// <summary>
        /// 현재 프로세스가 관리자 권한으로 실행 중인지 체크
        /// </summary>
        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// 프로세스를 관리자 권한으로 재실행
        /// </summary>
        private static void ElevateToAdministrator()
        {
            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas" // 관리자 권한으로 실행을 요청하는 핵심 키워드
            };

            try
            {
                Process.Start(proc);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("관리자 권한 승인이 거부되어 프로그램을 실행할 수 없습니다.");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 프로그램 종료 전 대기
        /// </summary>
        private static void CloseProgram()
        {
            Console.WriteLine("\n종료하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }
}
