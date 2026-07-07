using System;
using System.Data;
// 引入刚才添加的 Oracle 核心连接库
using Oracle.DataAccess.Client;

namespace OracleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. 配置连接字符串 (EZ Connect 格式)
            // Oracle 免费版默认的服务名通常是 FREE。我们先用管理员 system 身份来测试底层连通性
            string connString = "Data Source=<legacy-local-oracle>;User Id=<legacy-user>;Password=<redacted>;";

            // 2. 建立连接
            using (OracleConnection conn = new OracleConnection(connString))
            {
                try
                {
                    Console.WriteLine("正在尝试连接数据库，请稍候...");
                    conn.Open(); // 见证奇迹的时刻

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n恭喜！成功打通前后端，完美连接到 Oracle 数据库！\n");
                    Console.ResetColor();

                    // 3. 顺手执行一个简单的查询：获取数据库的真实版本信息
                    OracleCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT BANNER FROM v$version";
                    OracleDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        Console.WriteLine("当前数据库版本: " + reader.GetString(0));
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n连接失败，报错信息: \n" + ex.Message);
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\n按任意键退出测试...");
            Console.ReadLine();
        }
    }
}
