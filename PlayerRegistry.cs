using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CoopServer
{
    /// <summary>
    /// Every player who came to the server, by a secret code the server gives their game at the first connection (the
    /// game keeps it and sends it back each time, the player never sees it). The code is the player's real identity: they
    /// may change their pseudo, the server still knows who they are. Kept in "Coop-Players.txt" (easy to read, to find
    /// someone and ban them).
    /// A pseudo belongs to the first code that used it: another player can't take it (the admin frees it with "release").
    /// A ban covers the code and the last address of the player (a new install gives a new code, not a new address).
    /// </summary>
    internal sealed class PlayerRegistry
    {
        public sealed class Player
        {
            public String Code;
            public DateTime FirstSeen;
            public DateTime LastSeen;
            public String LastAddress = "";
            public List<String> Names = new List<String>(); // the last one used first
            public Boolean Banned;
            public String BanReason = "";

            public String ShortCode => Code.Length > 8 ? Code.Substring(0, 8) : Code;
            public String LastName => Names.Count > 0 ? Names[0] : "?";
        }

        private readonly String _path;
        private readonly Action<String> _log;
        private readonly Object _lock = new Object();
        private readonly List<Player> _players = new List<Player>();
        private readonly Dictionary<String, String> _bannedAddresses = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase); // address -> code

        public PlayerRegistry(String path, Action<String> log)
        {
            _path = path;
            _log = log;
            Load();
        }

        public Int32 Count
        {
            get { lock (_lock) return _players.Count; }
        }

        public static String NewCode()
        {
            Byte[] bytes = new Byte[16];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
                random.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public static Boolean IsValidCode(String code)
        {
            if (code == null || code.Length != 32)
                return false;
            foreach (Char c in code)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    return false;
            return true;
        }

        /// <summary>
        /// A player connects: "code" is the one their game sent (empty the first time: a new one is made). Returns the
        /// refusal, or null; "code" is then the player's code to send back.
        /// </summary>
        public String Arrive(ref String code, String name, String address)
        {
            if (code == null)
                return Memoria.Coop.Lang.B("Ton mod LifaMods est trop ancien pour ce serveur : mets-le à jour", "Your LifaMods mod is too old for this server: please update it");
            name = CleanName(name);
            lock (_lock)
            {
                if (!IsValidCode(code))
                    code = NewCode();
                String me = code;
                Player player = _players.Find(p => p.Code == me);
                if (player != null && player.Banned)
                    return Memoria.Coop.Lang.B("Tu es banni de ce serveur", "You are banned from this server") + Reason(player.BanReason);
                if (_bannedAddresses.TryGetValue(address, out String bannedCode))
                {
                    Player banned = _players.Find(p => p.Code == bannedCode);
                    return Memoria.Coop.Lang.B("Tu es banni de ce serveur", "You are banned from this server") + Reason(banned?.BanReason);
                }
                Player owner = Owner(name);
                if (owner != null && owner.Code != me)
                    return Memoria.Coop.Lang.B($"Le pseudo {name} appartient à un autre joueur : choisis un autre pseudo dans le menu titre",
                        $"The name {name} belongs to another player: choose another name on the title screen");
                if (player == null)
                {
                    player = new Player { Code = me, FirstSeen = DateTime.Now };
                    _players.Add(player);
                    _log?.Invoke($"Nouveau joueur : {name} (code {player.ShortCode})");
                }
                else if (!player.Names.Exists(n => String.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                {
                    _log?.Invoke($"{player.LastName} (code {player.ShortCode}) se connecte sous un nouveau pseudo : {name}");
                }
                player.LastSeen = DateTime.Now;
                player.LastAddress = address;
                player.Names.RemoveAll(n => String.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                player.Names.Insert(0, name);
                Save();
                return null;
            }
        }

        private static String Reason(String reason)
        {
            return String.IsNullOrEmpty(reason) ? "" : " (" + reason + ")";
        }

        private Player Owner(String name)
        {
            return _players.Find(p => p.Names.Exists(n => String.Equals(n, name, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>A player by one of their pseudos, or by the start of their code (8 characters or more)</summary>
        private Player Find(String who)
        {
            who = (who ?? "").Trim();
            if (who.Length == 0)
                return null;
            Player player = Owner(who);
            if (player == null && who.Length >= 8)
                player = _players.Find(p => p.Code.StartsWith(who.ToLowerInvariant(), StringComparison.Ordinal));
            return player;
        }

        /// <summary>Who is it: pseudos, dates, ban; the address only for the console ("withAddress")</summary>
        public String Whois(String who, Boolean withAddress)
        {
            lock (_lock)
            {
                Player player = Find(who);
                if (player == null)
                    return $"Joueur inconnu : {who}";
                return $"Code {player.ShortCode} : pseudos {String.Join(", ", player.Names)} ; première connexion {player.FirstSeen:dd/MM/yyyy HH:mm}, dernière {player.LastSeen:dd/MM/yyyy HH:mm}"
                    + (withAddress ? $" ; adresse {player.LastAddress}" : "")
                    + (player.Banned ? " ; BANNI" + Reason(player.BanReason) : "");
            }
        }

        /// <summary>The last players seen (most recent first)</summary>
        public List<String> Recent(Int32 count, Boolean withAddress)
        {
            lock (_lock)
            {
                List<Player> sorted = new List<Player>(_players);
                sorted.Sort((a, b) => b.LastSeen.CompareTo(a.LastSeen));
                List<String> lines = new List<String>();
                for (Int32 i = 0; i < sorted.Count && i < count; i++)
                {
                    Player p = sorted[i];
                    lines.Add($"  {p.ShortCode} {String.Join(", ", p.Names)} (vu le {p.LastSeen:dd/MM HH:mm}{(withAddress ? ", " + p.LastAddress : "")}){(p.Banned ? " BANNI" : "")}");
                }
                return lines;
            }
        }

        /// <summary>Bans the player's code and last address. Returns the pseudos the player used (to kick them), or null.</summary>
        public List<String> Ban(String who, String reason, out String result)
        {
            lock (_lock)
            {
                Player player = Find(who);
                if (player == null)
                {
                    result = $"Joueur inconnu : {who}";
                    return null;
                }
                player.Banned = true;
                player.BanReason = (reason ?? "").Replace("|", "/").Trim();
                if (player.LastAddress.Length > 0)
                    _bannedAddresses[player.LastAddress] = player.Code;
                Save();
                result = $"{player.LastName} (code {player.ShortCode}) est banni" + Reason(player.BanReason);
                return new List<String>(player.Names);
            }
        }

        public String Unban(String who)
        {
            lock (_lock)
            {
                Player player = Find(who);
                if (player == null)
                    return $"Joueur inconnu : {who}";
                if (!player.Banned)
                    return $"{player.LastName} n'est pas banni";
                player.Banned = false;
                player.BanReason = "";
                List<String> addresses = new List<String>();
                foreach (KeyValuePair<String, String> pair in _bannedAddresses)
                    if (pair.Value == player.Code)
                        addresses.Add(pair.Key);
                foreach (String address in addresses)
                    _bannedAddresses.Remove(address);
                Save();
                return $"{player.LastName} (code {player.ShortCode}) n'est plus banni";
            }
        }

        /// <summary>Frees a pseudo (a player who lost their code, from a new install, can take it again)</summary>
        public String Release(String name)
        {
            lock (_lock)
            {
                Player owner = Owner(name);
                if (owner == null)
                    return $"Le pseudo {name} n'appartient à personne";
                owner.Names.RemoveAll(n => String.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                Save();
                return $"Le pseudo {name} est libéré (il était au code {owner.ShortCode})";
            }
        }

        // ---------------- File ----------------
        // Player = code|first seen|last seen|last address|pseudo1,pseudo2...|banned 0/1|ban reason
        // BannedAddress = address|code

        private const String DateFormat = "yyyy-MM-dd HH:mm:ss";

        private static String CleanName(String name)
        {
            return name.Replace("|", "").Replace(",", "").Trim();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return;
                foreach (String raw in File.ReadAllLines(_path, Encoding.UTF8))
                {
                    String line = raw.Trim();
                    Int32 equal = line.IndexOf('=');
                    if (line.StartsWith(";") || equal < 0)
                        continue;
                    String key = line.Substring(0, equal).Trim().ToLowerInvariant();
                    String[] parts = line.Substring(equal + 1).Trim().Split('|');
                    if (key == "player" && parts.Length >= 7 && IsValidCode(parts[0]))
                    {
                        Player player = new Player { Code = parts[0], LastAddress = parts[3], Banned = parts[5] == "1", BanReason = parts[6] };
                        DateTime.TryParse(parts[1], out player.FirstSeen);
                        DateTime.TryParse(parts[2], out player.LastSeen);
                        foreach (String name in parts[4].Split(','))
                            if (name.Trim().Length > 0)
                                player.Names.Add(name.Trim());
                        _players.Add(player);
                    }
                    else if (key == "bannedaddress" && parts.Length >= 2 && parts[0].Length > 0)
                    {
                        _bannedAddresses[parts[0]] = parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Can't read {_path}: {ex.Message}");
            }
        }

        private void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("; Players of the server, written by the server (stop it before editing)\r\n");
                sb.Append("; Player = code|first seen|last seen|last address|pseudos (last used first)|banned (1)|ban reason\r\n");
                foreach (Player p in _players)
                {
                    List<String> names = p.Names.ConvertAll(CleanName);
                    sb.Append("Player = ").Append(p.Code).Append('|').Append(p.FirstSeen.ToString(DateFormat)).Append('|').Append(p.LastSeen.ToString(DateFormat))
                        .Append('|').Append(p.LastAddress).Append('|').Append(String.Join(",", names)).Append('|').Append(p.Banned ? "1" : "0").Append('|').Append(p.BanReason).Append("\r\n");
                }
                foreach (KeyValuePair<String, String> pair in _bannedAddresses)
                    sb.Append("BannedAddress = ").Append(pair.Key).Append('|').Append(pair.Value).Append("\r\n");
                String temp = _path + ".tmp";
                File.WriteAllText(temp, sb.ToString(), Encoding.UTF8);
                if (File.Exists(_path))
                    File.Delete(_path);
                File.Move(temp, _path);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Can't write {_path}: {ex.Message}");
            }
        }
    }
}
