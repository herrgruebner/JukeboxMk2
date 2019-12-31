using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace JukeboxMk2.Models
{
    public class Db
    {
        public Db()
        {
            CreateDatabase();
        }
        public void CreateDatabase()
        {
            
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            connection.Execute(@"IF NOT EXISTS 
                    (SELECT 'X'
                    FROM   INFORMATION_SCHEMA.TABLES
                    WHERE  TABLE_NAME = 'user_data')
                BEGIN
                    create table user_data (
                        Name varchar(max),
                        AccessToken varchar(max),
                        RefreshToken varchar(max)
                    )
                END");

        }
        public IEnumerable<UserData> GetData()
        {
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            var data = connection.Query<UserData>("select * from user_data");
            return data;
        }
        public void InsertData(UserData data)
        {
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            var res = connection.Execute("insert into user_data (Name, AccessToken, RefreshToken ) VALUES (@name, @accessToken, @refreshToken);", 
                new { name = data.Name, accessToken = data.AccessToken, refreshToken = data.RefreshToken });
            
        }
        public void UpdateTokens(UserData data)
        {
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            var res = connection.Execute("update user_data set AccessToken = @accessToken where name = @name",
                new { name = data.Name, accessToken = data.AccessToken, refreshToken = data.RefreshToken });
        }
    }
}
