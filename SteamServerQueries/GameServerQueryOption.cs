using System.Text;
using JetBrains.Annotations;

namespace Magic.Steam
{
    public sealed class GameServerQueryOption
    {
        public GameServerQueryOption()
        {
            MasterServerName = "hl2master.steampowered.com";
            Filter = new QueryFilter();
        }

        [PublicAPI]
        public string MasterServerName { get; set; }

        [PublicAPI]
        public QueryFilter Filter { get; }
    }

    public sealed class QueryFilter
    {
        public string GameDir { get; set; }

        public string Appid { get; set; }

        public bool? PasswordProtected { get; set; }

        
        [Pure]
        public override string ToString()
        {
            var result = new StringBuilder();
            if (!string.IsNullOrEmpty(this.GameDir))
            {
                result.Append(@"\gamedir\");
                result.Append(this.GameDir);
            }
            if (!string.IsNullOrEmpty(this.Appid))
            {
                result.Append(@"\appid\");
                result.Append(this.Appid);
            }
            if (this.PasswordProtected.HasValue)
            {
                result.Append(@"\password\");
                result.Append(this.PasswordProtected.Value ? "1" : "0");
            }
            return result.ToString();
        }
    }
}