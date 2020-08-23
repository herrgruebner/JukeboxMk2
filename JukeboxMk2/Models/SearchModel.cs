using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace JukeboxMk2.Models
{
    public class SearchModel
    {
        public IEnumerable<Song> Songs { get; set; }
        [DisplayName("Song Name")]
        public string SongName { get; set; }
        public string PlaylistId { get; set; }

    }
}
