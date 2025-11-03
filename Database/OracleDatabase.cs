using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper.Database
{
    public class OracleDatabase
    {
        public static async Task<DataTable> ExecuteQueryAsync(string connectionString, string userId, string password, string query)
        {
            var dataTable = new DataTable();

            try
            {
                await Task.Run(() =>
                {
                    string fullConnectionString = $"{connectionString}User Id={userId};Password={password};";
                        
                    using var connection = new OracleConnection(fullConnectionString);
                    connection.Open();

                    using var command = new OracleCommand(query, connection);
                    command.CommandTimeout = 300; // 5遺???꾩븘??

                    using var adapter = new OracleDataAdapter(command);
                    adapter.Fill(dataTable);
                    
                    // ?붾쾭洹? 諛섑솚 而щ읆紐??뺤씤
                    System.Diagnostics.Debug.WriteLine("=== Oracle?먯꽌 諛섑솚??而щ읆紐?===");
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {col.ColumnName}");
                    }
                });
            }
            catch (OracleException ex)
            {
                // Oracle ?뱀젙 ?먮윭 泥섎━
                if (ex.Number == 1017) // ORA-01017: invalid username/password; logon denied
                {
                    throw new Exception("Oracle ?곌껐 ?ㅽ뙣: ?ъ슜??ID ?먮뒗 鍮꾨?踰덊샇媛 ?щ컮瑜댁? ?딆뒿?덈떎.", ex);
                }
                else if (ex.Number == 12154) // ORA-12154: TNS:could not resolve the connect identifier specified
                {
                    throw new Exception("Oracle ?곌껐 ?ㅽ뙣: TNS ?대쫫??李얠쓣 ???놁뒿?덈떎.", ex);
                }
                else if (ex.Number == 12514) // ORA-12514: TNS:listener does not currently know of service requested
                {
                    throw new Exception("Oracle ?곌껐 ?ㅽ뙣: ?쒕퉬?ㅻ? 李얠쓣 ???놁뒿?덈떎.", ex);
                }
                else if (ex.Number == 12541) // ORA-12541: TNS:no listener
                {
                    throw new Exception("Oracle ?곌껐 ?ㅽ뙣: 由ъ뒪?덇? ?묐떟?섏? ?딆뒿?덈떎.", ex);
                }
                else
                {
                    throw new Exception($"Oracle ?ㅻ쪟 (ORA-{ex.Number:D5}): {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"?곗씠?곕쿋?댁뒪 ?곌껐 ?먮뒗 荑쇰━ ?ㅽ뻾 以??ㅻ쪟媛 諛쒖깮?덉뒿?덈떎: {ex.Message}", ex);
            }

            return dataTable;
        }

        public static async Task<bool> TestConnectionAsync(string connectionString, string userId, string password)
        {
            try
            {
                await Task.Run(() =>
                {
                    string fullConnectionString = $"{connectionString}User Id={userId};Password={password};";

                    using var connection = new OracleConnection(fullConnectionString);
                    connection.Open();
                    connection.Close();
                });

                return true;
            }
            catch (OracleException ex)
            {
                // 占쏙옙占쏙옙占쏙옙 占싸그울옙 占쏙옙占?(占쏙옙占시삼옙占쏙옙)
                System.Diagnostics.Debug.WriteLine($"Oracle 占쏙옙占쏙옙 占쌓쏙옙트 占쏙옙占쏙옙 (ORA-{ex.Number:D5}): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // 占싹뱄옙 占쏙옙占쏙옙 占싸깍옙
                System.Diagnostics.Debug.WriteLine($"占쏙옙占쏙옙 占쌓쏙옙트 占쏙옙占쏙옙: {ex.Message}");
                return false;
            }
        }
    }
}
