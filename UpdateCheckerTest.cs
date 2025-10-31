using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// ������Ʈ Ȯ�� �׽�Ʈ ���α׷�
    /// </summary>
    class UpdateCheckerTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== GitHub API �׽�Ʈ ===");
            Console.WriteLine();
            
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FACTOVA_QueryHelper");
            
            try
            {
                Console.WriteLine("��û URL: https://api.github.com/repos/jhs8581/FACTOVA_QueryHelper/releases/latest");
                Console.WriteLine("��û ��...");
                Console.WriteLine();
                
                var response = await client.GetAsync("https://api.github.com/repos/jhs8581/FACTOVA_QueryHelper/releases/latest");
                
                Console.WriteLine($"���� �ڵ�: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"���� ����: {response.IsSuccessStatusCode}");
                Console.WriteLine();
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("���� ���� (ù 500��):");
                    Console.WriteLine(content.Substring(0, Math.Min(500, content.Length)));
                }
                else
                {
                    Console.WriteLine("���� ����:");
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(content);
                }
                
                Console.WriteLine();
                Console.WriteLine("=== Rate Limit Ȯ�� ===");
                var rateLimitResponse = await client.GetAsync("https://api.github.com/rate_limit");
                if (rateLimitResponse.IsSuccessStatusCode)
                {
                    var rateLimitContent = await rateLimitResponse.Content.ReadAsStringAsync();
                    Console.WriteLine(rateLimitContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"���� �߻�: {ex.GetType().Name}");
                Console.WriteLine($"�޽���: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"���� ����: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("�ƹ� Ű�� ������ ����˴ϴ�...");
            Console.ReadKey();
        }
    }
}
