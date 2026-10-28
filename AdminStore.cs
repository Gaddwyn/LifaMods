using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CoopServer
{
    /// <summary>
    /// The server's admins: pseudo and secret token, in "Coop-Admins.txt". A player becomes admin with the server's password
    /// ("/admin password" in the chat, ADMIN_PASSWORD / --admin-password); their game keeps the token and sends it back at
    /// each connection, so using the same pseudo is not enough to take the role.
    /// </summary>
    internal sealed class AdminStore
    {
        private readonly String _path;
        private readonly Action<String> _log;
        private readonly Object _lock = new Object();
        private readonly Dictionary<String, String> _tokens = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        public AdminStore(String path, Action<String> log)
        {
            _path = path;
            _log = log;
            Load();
        }

        /// <summary>The player's token (a new one the first time)</summary>
        public String Add(String name)
        {
            lock (_lock)
            {
                if (!_tokens.TryGetValue(name, out String token))
                {
                    Byte[] bytes = new Byte[16];
                    using (RandomNumberGenerator random = RandomNumberGenerator.Create())
                        random.GetBytes(bytes);
                    token = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                    _tokens[name] = token;
                    Save();
                }
                return token;
            }
        }

        public Boolean Remove(String name)
        {
            lock (_lock)
            {
                if (!_tokens.Remove(name))
                    return false;
                Save();
                return true;
            }
        }

        public Boolean Check(String name, String token)
        {
            lock (_lock)
                return token.Length > 0 && _tokens.TryGetValue(name, out String known) && FixedEquals(known, token);
        }

        public List<String> Names()
        {
            lock (_lock)
                return new List<String>(_tokens.Keys);
        }

        private static Boolean FixedEquals(String a, String b)
        {
            Byte[] x = Encoding.UTF8.GetBytes(a);
            Byte[] y = Encoding.UTF8.GetBytes(b);
            return x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
        }

        // Admin = name|token
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
                    if (line.StartsWith(";") || equal < 0 || !String.Equals(line.Substring(0, equal).Trim(), "admin", StringComparison.OrdinalIgnoreCase))
                        continue;
                    String[] parts = line.Substring(equal + 1).Trim().Split('|');
                    if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                        _tokens[parts[0]] = parts[1];
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
                sb.Append("; Admins of the server (pseudo|secret token kept by their game), written by CoopServer: use 'admin remove <pseudo>' in the console\r\n");
                foreach (KeyValuePair<String, String> pair in _tokens)
                    sb.Append("Admin = ").Append(pair.Key).Append('|').Append(pair.Value).Append("\r\n");
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
