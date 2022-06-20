using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        private static string conString = @"Provider=SQLOLEDB;Server=206-13\MSSQLSERVER99;Database=practice;Trusted_Connection=yes;";
        static void Main(string[] args)
        {
            Task task = ExcuteSqlTransaction(@"Data Source=206-13\MSSQLSERVER99;Initial Catalog=practice;Trusted_Connection=yes;");
            task.Wait();
        }

        public static void Exmpl01()
        {
            #region Фабрика

            //Полчаем фабрику
            string factory = ConfigurationManager.AppSettings["factory"];
            DbProviderFactory provider = DbProviderFactories.GetFactory(factory);

            //испольщуем ФАБРИКУ для получения соедения
            DbConnection con = provider.CreateConnection();
            con.ConnectionString =
                ConfigurationManager.ConnectionStrings["DefaultConnection"]
                .ConnectionString;

            //Создаем команду
            DbCommand cmd = provider.CreateCommand();
            cmd.CommandText = ConfigurationManager.AppSettings["departments"];
            cmd.Connection = con;

            using (con)
            {
                con.Open();

                DbDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine("{0}. {1}", reader[0], reader[2]);
                }
            }
            #endregion
        }

        public static void Exmpl02()
        {
            using (SqlConnection conn = 
                new SqlConnection(ConfigurationManager
                .ConnectionStrings["DefaultConnection"]
                .ConnectionString))
            {
                SqlCommand command = 
                    new SqlCommand("SELECT Id, Building, DepartName FROM Departments", 
                    conn);

                var result = MethodAsync(conn, command);

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine("{0}. {1}", reader[0], reader[2]);
                }
            }
        }

        static async Task<bool> MethodAsync(SqlConnection conn, SqlCommand command)
        {
            await conn.OpenAsync();
            await command.ExecuteNonQueryAsync();

            Thread.Sleep(5000);
            return true;
        }

        public void Exmpl03()
        {
            SqlConnectionStringBuilder build = new SqlConnectionStringBuilder();
            build.DataSource = @"206-13\MSSQLSERVER99";
            build.InitialCatalog = "practice";
            build.IntegratedSecurity = true;

            string provider = "System.Data.SqlClient";
            Task task = Perf(build.ConnectionString, provider);
            task.Wait();
        }


        public static async Task Perf(string connectionString, string providerName)
        {
            var factory = DbProviderFactories.GetFactory(providerName);
            using (DbConnection con = factory.CreateConnection())
            {
                con.ConnectionString = connectionString;
                await con.OpenAsync();
            }
        }

        public static async Task ExcuteSqlTransaction(string connectionString)
        {
            using (SqlConnection conn =new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                SqlCommand cmd = conn.CreateCommand();
                SqlTransaction transaction = null;

                transaction = 
                    await Task.Run<SqlTransaction>(
                                        () => conn.BeginTransaction("Simple trn"));

                cmd.Connection = conn;
                cmd.Transaction = transaction;

                try
                {
                    cmd.CommandText = "INSERT INTO [dbo].[Doctors]([DocName],[Premium],[Salary],[Surname])VALUES ('Hugh',6666,5555,'Laurie')";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = "INSERT INTO [dbo].[Departments]([Building],[DepartName])VALUES(5,'DCT')";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = "INSERT INTO [dbo].[Examinations]([ExamName])VALUES(N'Джеймс Хью Кэ́лам Ло́ри — британский актёр, режиссёр, певец, музыкант, комик и писатель. Известность ему принесли роли в британских комедийных телесериалах «Чёрная Гадюка», «Шоу Фрая и Лори» и «Дживс и Вустер» со Стивеном Фраем, а также роль заглавного персонажа в американском телесериале «Доктор Хаус»')";
                    await cmd.ExecuteNonQueryAsync();

                    await Task.Run(() => transaction.Commit());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Message: "+ex.Message);
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception exR)
                    {
                        Console.WriteLine(exR.Message);
                    }                   
                }
            }
        }
    }
}
