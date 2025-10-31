using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// 업데이트 확인 테스트 프로그램
    /// </summary>
    class UpdateCheckerTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== GitHub API 테스트 ===");
            Console.WriteLine();
            
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FACTOVA_QueryHelper");
            
            try
            {
                Console.WriteLine("요청 URL: https://api.github.com/repos/jhs8581/FACTOVA_QueryHelper/releases/latest");
                Console.WriteLine("요청 중...");
                Console.WriteLine();
                
                var response = await client.GetAsync("https://api.github.com/repos/jhs8581/FACTOVA_QueryHelper/releases/latest");
                
                Console.WriteLine($"상태 코드: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"성공 여부: {response.IsSuccessStatusCode}");
                Console.WriteLine();
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("응답 내용 (첫 500자):");
                    Console.WriteLine(content.Substring(0, Math.Min(500, content.Length)));
                }
                else
                {
                    Console.WriteLine("오류 응답:");
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(content);
                }
                
                Console.WriteLine();
                Console.WriteLine("=== Rate Limit 확인 ===");
                var rateLimitResponse = await client.GetAsync("https://api.github.com/rate_limit");
                if (rateLimitResponse.IsSuccessStatusCode)
                {
                    var rateLimitContent = await rateLimitResponse.Content.ReadAsStringAsync();
                    Console.WriteLine(rateLimitContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.GetType().Name}");
                Console.WriteLine($"메시지: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"내부 예외: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("아무 키나 누르면 종료됩니다...");
            Console.ReadKey();
        }
    }
}
