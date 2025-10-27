using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper
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
                    command.CommandTimeout = 300; // 5�� Ÿ�Ӿƿ�

                    using var adapter = new OracleDataAdapter(command);
                    adapter.Fill(dataTable);
                    
                    // �����: ���� �÷��� Ȯ��
                    System.Diagnostics.Debug.WriteLine("=== Oracle���� ��ȯ�� �÷��� ===");
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {col.ColumnName}");
                    }
                });
            }
            catch (OracleException ex)
            {
                // Oracle Ư�� ���� ó��
                if (ex.Number == 1017) // ORA-01017: invalid username/password; logon denied
                {
                    throw new Exception("Oracle ���� ����: ����� ID �Ǵ� ��й�ȣ�� �ùٸ��� �ʽ��ϴ�.", ex);
                }
                else if (ex.Number == 12154) // ORA-12154: TNS:could not resolve the connect identifier specified
                {
                    throw new Exception("Oracle ���� ����: TNS �̸��� ã�� �� �����ϴ�.", ex);
                }
                else if (ex.Number == 12514) // ORA-12514: TNS:listener does not currently know of service requested
                {
                    throw new Exception("Oracle ���� ����: ���񽺸� ã�� �� �����ϴ�.", ex);
                }
                else if (ex.Number == 12541) // ORA-12541: TNS:no listener
                {
                    throw new Exception("Oracle ���� ����: �����ʰ� �������� �ʽ��ϴ�.", ex);
                }
                else
                {
                    throw new Exception($"Oracle ���� (ORA-{ex.Number:D5}): {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"�����ͺ��̽� ���� �Ǵ� ���� ���� �� ������ �߻��߽��ϴ�: {ex.Message}", ex);
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
                // ������ �α׿� ��� (���û���)
                System.Diagnostics.Debug.WriteLine($"Oracle ���� �׽�Ʈ ���� (ORA-{ex.Number:D5}): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // �Ϲ� ���� �α�
                System.Diagnostics.Debug.WriteLine($"���� �׽�Ʈ ����: {ex.Message}");
                return false;
            }
        }
    }
}
