using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFT_Augment_Data_Classes
{
    public class Match
    {
        public string id { get; set; }
        public string region { get; set; }
        public string patch { get; set; }
        public string[] augments { get; set; }
        public string placement { get; set; }

        public Match(string id, string region, string patch, string[] augments, string placement)
        {
            this.id = id;
            this.region = region;
            this.patch = patch;
            this.augments = augments;
            this.placement = placement;
        }
    }
}