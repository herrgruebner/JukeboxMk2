using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

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
                { "redirect_uri", redirectUri} // not actually used, but for validation
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

        public string CreateNewPlaylist(UserData data, bool secondAttempt = false)
        {
            var profile = GetUserProfile(data);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
            var content = new StringContent(JsonConvert.SerializeObject(new PlaylistRequestModel()), Encoding.UTF8, "application/json");
            var response = client.PostAsync($"https://api.spotify.com/v1/users/{profile.id}/playlists", null).Result;
            if (response.IsSuccessStatusCode)
            {
                var newPlaylistJson = response.Content.ReadAsStringAsync().Result;
                var playlist = JsonConvert.DeserializeObject<Playlist>(newPlaylistJson);
                return playlist.id;
            }
            else
            {
                RefreshTokenForJukebox(data.JukeBoxId);
                if (secondAttempt)
                    throw new Exception("Token expired, please try again");
                return CreateNewPlaylist(data, true);
            }
        }

        public void RefreshTokenForJukebox(string name)
        {
            var db = new Db();
            var data = db.GetData().FirstOrDefault(s => s.JukeBoxId == name);
            var tokens = RefreshTokens(data.RefreshToken);
            db.UpdateTokens(new UserData()
            {
                AccessToken = tokens.access_token,
                JukeBoxId = name,
                RefreshToken = tokens.refresh_token
            });
        }

        public void AddSong(string id, UserData data, bool secondAttempt = false)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
            var response = client.PostAsync($"https://api.spotify.com/v1/playlists/{data.PlaylistId}/tracks?uris=spotify%3Atrack%3A{id}", null).Result;
            if (!response.IsSuccessStatusCode)
            {                 
                RefreshTokenForJukebox("max");
                if (secondAttempt)
                    throw new Exception("Token expired, please try again");
                AddSong(id, data, true);

            }
        }
        public IEnumerable<Song> SearchSongs(string name, bool secondAttempt = false)
        {
            var data = new Db().GetData().FirstOrDefault(s => s.JukeBoxId == "max");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
            var response = client.GetAsync($"https://api.spotify.com/v1/search?q={WebUtility.UrlEncode(name)}&type=track").Result;
            var songs = new List<Song>();
            if (response.IsSuccessStatusCode)
            {
                var trackjson = response.Content.ReadAsStringAsync().Result;
                var tracks = JsonConvert.DeserializeObject<TrackList>(trackjson);
                foreach (var item in tracks.tracks.items)
                {
                    var song = new Song()
                    {
                        Title = item.name,
                        Artist = string.Join(" ", item.artists.Select(s => s.name)),
                        Id = item.id,
                        Length = item.duration_ms / 1000
                    };
                    songs.Add(song);
                }
                return songs;
            }
            else
            {
                RefreshTokenForJukebox("max");
                if (secondAttempt)
                    throw new Exception("Token expired, please try again");
                SearchSongs(name, true);
            }
            return null;
        }

        public Profile GetUserProfile(UserData data, bool secondAttempt = false)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
            var response = client.GetAsync($"https://api.spotify.com/v1/me").Result;
            if (response.IsSuccessStatusCode)
            {
                var profileJson = response.Content.ReadAsStringAsync().Result;
                var profile = JsonConvert.DeserializeObject<Profile>(profileJson);
                return profile;

            }
            else
            {
                RefreshTokenForJukebox(data.JukeBoxId);
                if (secondAttempt)
                    throw new Exception("Token expired, please try again");
                GetUserProfile(data, true);
            }
            return null;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    }

    public class PlaylistRequestModel
    {
        public string Name => $"Jukebox {DateTime.Now.ToString("yyyy-MM-dd")}";
    }
    public class ExternalUrls
    {
        public string spotify { get; set; }
    }

    public class Followers
    {
        public object href { get; set; }
        public int total { get; set; }
    }

    public class ExternalUrls2
    {
        public string spotify { get; set; }
    }

    public class Owner
    {
        public ExternalUrls2 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class Tracks
    {
        public string href { get; set; }
        public List<Item> items { get; set; }
        public int limit { get; set; }
        public object next { get; set; }
        public int offset { get; set; }
        public object previous { get; set; }
        public int total { get; set; }
    }

    public class Playlist
    {
        public bool collaborative { get; set; }
        public object description { get; set; }
        public ExternalUrls external_urls { get; set; }
        public Followers followers { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<object> images { get; set; }
        public string name { get; set; }
        public Owner owner { get; set; }
        public bool Public { get; set; }
        public string snapshot_id { get; set; }
        public Tracks tracks { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

public class Song
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Id { get; set; }
        public int Length { get; set; }
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

    public class Artist
    {
        public ExternalUrls external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
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

    public class ExternalIds
    {
        public string isrc { get; set; }
    }

    public class Item
    {
        public Album album { get; set; }
        public List<Artist> artists { get; set; }
        public List<string> available_markets { get; set; }
        public int disc_number { get; set; }
        public int duration_ms { get; set; }
        public bool @explicit { get; set; }
        public ExternalIds external_ids { get; set; }
        public ExternalUrls external_urls { get; set; }
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



    public class TrackList
    {
        public Tracks tracks { get; set; }
    }

    public class Profile
    {
        public string country { get; set; }
        public string display_name { get; set; }
        public string email { get; set; }
        public ExternalUrls external_urls { get; set; }
        public Followers followers { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<Image> images { get; set; }
        public string product { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }
}
