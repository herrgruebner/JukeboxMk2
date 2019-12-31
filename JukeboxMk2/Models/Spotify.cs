using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace JukeboxMk2.Models
{
    public class Spotify
    {
        private static readonly HttpClient client = new HttpClient();

        public AuthorisationResponse GenerateAccessRefreshTokens(string authorisationCode, string redirectUri)
        {
            var values = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" }, //oauth spec
                { "code", authorisationCode},
                { "redirect_uri", redirectUri} // not actual used, but for validation
            };

            var content = new FormUrlEncodedContent(values);
            var authToken = Base64Encode(Environment.GetEnvironmentVariable("ClientId") + ":" + Environment.GetEnvironmentVariable("ClientSecret"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            var response = client.PostAsync("https://accounts.spotify.com/api/token", content).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception();
            }
            var auth = JsonConvert.DeserializeObject<AuthorisationResponse>(response.Content.ReadAsStringAsync().Result);
            return auth;
        }

        public AuthorisationResponse RefreshTokens(string refreshToken)
        {
            var values = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" }, //oauth spec
                { "refresh_token", refreshToken},
            };

            var content = new FormUrlEncodedContent(values);
            var authToken = Base64Encode(Environment.GetEnvironmentVariable("ClientId") + ":" + Environment.GetEnvironmentVariable("ClientSecret"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            var response = client.PostAsync("https://accounts.spotify.com/api/token", content).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception();
            }
            var auth = JsonConvert.DeserializeObject<AuthorisationResponse>(response.Content.ReadAsStringAsync().Result);
            return auth;
        }

        public void RefreshTokenForUser(string name)
        {
            var db = new Db();
            var data = db.GetData().FirstOrDefault(s => s.Name == name);
            var tokens = RefreshTokens(data.RefreshToken);
            db.UpdateTokens(new UserData()
            {
                AccessToken = tokens.access_token,
                Name = name,
                RefreshToken = tokens.refresh_token
            });
        }

        public void AddSong(string id)
        {
            var data = new Db().GetData().FirstOrDefault(s => s.Name == "max");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
            var playlistId = "2OmJbkmk1tuQggcFW97VFr";
            var response = client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks?uris=spotify%3Atrack%3A{id}", null).Result;
            if (response.IsSuccessStatusCode)
            {

            }
            else
            {
                RefreshTokenForUser("max");
                throw new Exception("Token expired, please try again");
            }
        }
        public IEnumerable<Song> SearchSongs(string name)
        {
            var data = new Db().GetData().FirstOrDefault(s => s.Name == "max");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
            var response = client.GetAsync($"https://api.spotify.com/v1/search?q={WebUtility.UrlEncode(name)}&type=track").Result;
            var songs = new List<Song>();
            if (response.IsSuccessStatusCode)
            {
                var trackjson = response.Content.ReadAsStringAsync().Result;
                var tracks = JsonConvert.DeserializeObject<RootObject>(trackjson);
                foreach (var item in tracks.tracks.items)
                {
                    var song = new Song()
                    {
                        Title = item.name,
                        Artist = string.Join(" ", item.artists.Select(s => s.name)),
                        Id = item.id,
                    };
                    songs.Add(song);
                }
                return songs;
            }
            else
            {
                RefreshTokenForUser("max");
                throw new Exception("Token expired, please try again");
            }
            return null;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    }

    public class Song
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Id { get; set; }
    }
    public class Tokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class AuthorisationResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }

    public class ExternalUrls
    {
        public string spotify { get; set; }
    }

    public class Artist
    {
        public ExternalUrls external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class ExternalUrls2
    {
        public string spotify { get; set; }
    }

    public class Image
    {
        public int height { get; set; }
        public string url { get; set; }
        public int width { get; set; }
    }

    public class Album
    {
        public string album_type { get; set; }
        public List<Artist> artists { get; set; }
        public List<string> available_markets { get; set; }
        public ExternalUrls2 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<Image> images { get; set; }
        public string name { get; set; }
        public string release_date { get; set; }
        public string release_date_precision { get; set; }
        public int total_tracks { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class ExternalUrls3
    {
        public string spotify { get; set; }
    }

    public class Artist2
    {
        public ExternalUrls3 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class ExternalIds
    {
        public string isrc { get; set; }
    }

    public class ExternalUrls4
    {
        public string spotify { get; set; }
    }

    public class Item
    {
        public Album album { get; set; }
        public List<Artist2> artists { get; set; }
        public List<string> available_markets { get; set; }
        public int disc_number { get; set; }
        public int duration_ms { get; set; }
        public bool @explicit { get; set; }
        public ExternalIds external_ids { get; set; }
        public ExternalUrls4 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public bool is_local { get; set; }
        public string name { get; set; }
        public int popularity { get; set; }
        public string preview_url { get; set; }
        public int track_number { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class Tracks
    {
        public string href { get; set; }
        public List<Item> items { get; set; }
        public int limit { get; set; }
        public string next { get; set; }
        public int offset { get; set; }
        public object previous { get; set; }
        public int total { get; set; }
    }

    public class RootObject
    {
        public Tracks tracks { get; set; }
    }
}
