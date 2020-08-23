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
            // todo setup DI and put this in at server startup
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            connection.Execute(@"IF NOT EXISTS 
                    (SELECT 'X'
                    FROM   INFORMATION_SCHEMA.TABLES
                    WHERE  TABLE_NAME = 'user_data')
                BEGIN
                    create table user_data (
                        JukeboxId varchar(max),
                        AccessToken varchar(max),
                        RefreshToken varchar(max),
                        PlaylistId varchar(max)
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
            var res = connection.Execute("insert into user_data (JukeboxId, AccessToken, RefreshToken, PlaylistId ) VALUES (@jukeboxId, @accessToken, @refreshToken, @playlistId);", 
                new { jukeboxId = data.JukeBoxId, accessToken = data.AccessToken, refreshToken = data.RefreshToken, playlistId = data.PlaylistId });
            
        }
        public void UpdateTokens(UserData data)
        {
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            var res = connection.Execute("update user_data set AccessToken = @accessToken where JukeboxId = @jukeboxId",
                new { jukeboxId = data.JukeBoxId, accessToken = data.AccessToken, refreshToken = data.RefreshToken });
        }

        internal void UpdatePlaylistId(UserData data)
        {
            using var connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionString"));
            var res = connection.Execute("update user_data set playlistId = @playlistId where JukeboxId = @jukeboxId",
                new { jukeboxId = data.JukeBoxId, playlistId = data.PlaylistId });
        }
    }
}
