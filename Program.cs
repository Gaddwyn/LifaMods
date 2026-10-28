using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoopServer
{
    /// <summary>
    /// Dedicated server of the Coop mod for FF9 (Memoria). It does what the playing host does in the game, without being a
    /// player: every client gets an id, and the server relays positions, chat and Tetra Master messages. It never decodes
    /// positions, so it doesn't need Unity: only the player id at the start of each state is rewritten.
    /// Must speak the same protocol as the mod (Protocol.cs).
    /// </summary>
    public static class Program
    {
        private const Int32 ProtocolVersion = 8;
        private const Byte ServerId = 0; // the "host" id: nobody plays with it here
        private const Int32 MaxFrameSize = 4096;
        private const Int32 MaxChatLength = 120;

        private enum MessageType : byte { Hello = 1, Welcome = 2, PlayerJoin = 3, PlayerLeave = 4, PlayerState = 5, Reject = 6, Chat = 7, Duel = 8 }

        private static readonly Object Lock = new Object();
        private static readonly List<Peer> Peers = new List<Peer>();
        private static Int32 s_port = 7777;
        private static Boolean s_noCheats;
        private static Int32 s_maxPlayers = 16;
        private static String s_serverName = ""; // shown to the players instead of "LifaMods"
        private static String s_allowedModsText = ""; // "MoguriMain, ExtraEquipmentMenu, Moguri*": the mods players may use (empty = any)
        private static List<String> s_allowedMods = new List<String>();
        private static Boolean s_strictMods = true; // refuse what changes the game outside Memoria's mod list (loaders, files added to the game folder)
        private static Int32 s_antiCheat = 100; // anti-cheat tolerance in percent, 0 = off
        private static volatile Boolean s_running = true;
        private static TcpListener s_listener;
        private static Memoria.Coop.ClanBankStore s_banks; // clan banks, kept here when players use a dedicated server
        private static Memoria.Coop.RankingStore s_ranking; // duel ranking, same
        private static Memoria.Coop.HuntStore s_hunt; // the Fête de la chasse, same
        private static Memoria.Coop.GiftStore s_gifts; // admin gifts waiting for their player, same
        private static PlayerRegistry s_players; // every player by the secret code their game keeps (Coop-Players.txt)
        private static AdminStore s_admins; // players made admin with the password
        private static String s_adminPassword = ""; // empty = nobody can become admin from the game
        private const Int32 MaxLoginFailures = 5; // wrong passwords before a connection is ignored for a while
        private static readonly Dictionary<String, KeyValuePair<Int32, DateTime>> s_loginFailures = new Dictionary<String, KeyValuePair<Int32, DateTime>>(); // by IP: failures, locked until
        [ThreadStatic] private static StringBuilder s_capture; // what a command run by an admin prints, sent back to them
        private static Int32 s_huntEvery = 6;   // hours between two festivals
        private static Int32 s_huntLength = 60; // minutes
        private static Int32 s_huntOffset = 0;  // hours after midnight of the first festival of the day
        private static String s_huntSchedule = ""; // "sam 20:00, dim 15:30": days and hours instead of every s_huntEvery hours

        public static Int32 Main(String[] args)
        {
            ReadSettings(args);
            try
            {
                s_listener = new TcpListener(IPAddress.Any, s_port);
                s_listener.Start();
            }
            catch (Exception ex)
            {
                Log($"Impossible d'ouvrir le port {s_port} : {ex.Message}");
                return 1;
            }
            Log($"CoopServer {typeof(Program).Assembly.GetName().Version} (protocole {ProtocolVersion}), NoCheats = {(s_noCheats ? 1 : 0)}, {s_maxPlayers} joueurs max");
            s_banks = new Memoria.Coop.ClanBankStore(Path.Combine(Directory.GetCurrentDirectory(), "Coop-ClanBank.txt"), Log) { CheatTolerance = s_antiCheat / 100.0 };
            Log(s_antiCheat > 0 ? $"Anti-triche des sauvegardes : tolérance {s_antiCheat} %" : "Anti-triche des sauvegardes : désactivé");
            s_ranking = new Memoria.Coop.RankingStore(Path.Combine(Directory.GetCurrentDirectory(), "Coop-Ranking.txt"), Log);
            s_hunt = new Memoria.Coop.HuntStore(Path.Combine(Directory.GetCurrentDirectory(), "Coop-Hunt.txt"), Log) { IntervalHours = s_huntEvery, DurationMinutes = s_huntLength, OffsetHours = s_huntOffset };
            s_hunt.Schedule = s_huntSchedule;
            Log(s_hunt.Describe());
            s_gifts = new Memoria.Coop.GiftStore(Path.Combine(Directory.GetCurrentDirectory(), "Coop-Gifts.txt"), Log);
            s_players = new PlayerRegistry(Path.Combine(Directory.GetCurrentDirectory(), "Coop-Players.txt"), Log);
            Log($"Joueurs connus : {s_players.Count} (whois, players, ban, unban, release)");
            s_admins = new AdminStore(Path.Combine(Directory.GetCurrentDirectory(), "Coop-Admins.txt"), Log);
            Log(s_adminPassword.Length > 0 ? $"Admins : {s_admins.Names().Count} (mot de passe admin réglé, /admin <mot de passe> dans le tchat)" : $"Admins : {s_admins.Names().Count} (pas de mot de passe admin : --admin-password ou ADMIN_PASSWORD)");
            if (s_serverName.Length > 0)
                Log($"Nom du serveur : {s_serverName}");
            Log(s_strictMods ? "Mods hors de Memoria (BepInEx, fichiers ajoutés au dossier du jeu...) : refusés (--strict-mods 0 ou STRICT_MODS=0 pour les accepter)" : "Mods hors de Memoria : acceptés");
            Log(s_allowedMods.Count > 0 ? $"Mods autorisés : LifaMods, {String.Join(", ", s_allowedMods)}" : "Mods autorisés : tous (--mods ou ALLOWED_MODS pour choisir)");
            Log($"Server listening on port {s_port}"); // Pterodactyl waits for this line
            Log("Commandes : list, say <texte>, kick <pseudo ou id>, unblock <pseudo>, ranking reset, hunt [start|end], give <pseudo> gil|item|card ..., admin [remove <pseudo>], whois <pseudo>, players, ban <pseudo> [raison], unban <pseudo>, release <pseudo>, stop");
            new Thread(AcceptLoop) { IsBackground = true, Name = "accept" }.Start();

            String line;
            while (s_running && (line = Console.ReadLine()) != null)
                RunCommand(line.Trim());
            // stdin closed (service without console): keep serving until killed
            while (s_running)
                Thread.Sleep(1000);
            return 0;
        }

        // ---------------- Settings ----------------

        /// <summary>Command line (--port 7777 --nocheats 1 --maxplayers 16 --name "My server") first, then environment variables (Docker / Pterodactyl).</summary>
        private static void ReadSettings(String[] args)
        {
            s_port = ReadInt(Environment.GetEnvironmentVariable("SERVER_PORT"), s_port);
            s_noCheats = Environment.GetEnvironmentVariable("NO_CHEATS") == "1";
            s_maxPlayers = ReadInt(Environment.GetEnvironmentVariable("MAX_PLAYERS"), s_maxPlayers);
            s_serverName = Environment.GetEnvironmentVariable("SERVER_NAME") ?? "";
            s_allowedModsText = Environment.GetEnvironmentVariable("ALLOWED_MODS") ?? "";
            if (Environment.GetEnvironmentVariable("STRICT_MODS") == "0")
                s_strictMods = false;
            s_adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "";
            if (Int32.TryParse(Environment.GetEnvironmentVariable("ANTI_CHEAT"), out Int32 antiCheat))
                s_antiCheat = antiCheat;
            s_huntEvery = ReadInt(Environment.GetEnvironmentVariable("HUNT_EVERY"), s_huntEvery);
            s_huntLength = ReadInt(Environment.GetEnvironmentVariable("HUNT_LENGTH"), s_huntLength);
            if (Int32.TryParse(Environment.GetEnvironmentVariable("HUNT_OFFSET"), out Int32 huntOffset))
                s_huntOffset = huntOffset;
            s_huntSchedule = Environment.GetEnvironmentVariable("HUNT_SCHEDULE") ?? "";
            for (Int32 i = 0; i + 1 < args.Length; i += 2)
            {
                switch (args[i].TrimStart('-').ToLowerInvariant())
                {
                    case "port": s_port = ReadInt(args[i + 1], s_port); break;
                    case "nocheats": s_noCheats = args[i + 1] == "1"; break;
                    case "maxplayers": s_maxPlayers = ReadInt(args[i + 1], s_maxPlayers); break;
                    case "name": s_serverName = args[i + 1]; break;
                    case "mods": s_allowedModsText = args[i + 1]; break;
                    case "strict-mods": s_strictMods = args[i + 1] != "0"; break;
                    case "admin-password": s_adminPassword = args[i + 1]; break;
                    case "anticheat": if (Int32.TryParse(args[i + 1], out Int32 value)) s_antiCheat = value; break;
                    case "hunt-every": s_huntEvery = ReadInt(args[i + 1], s_huntEvery); break;
                    case "hunt-length": s_huntLength = ReadInt(args[i + 1], s_huntLength); break;
                    case "hunt-offset": if (Int32.TryParse(args[i + 1], out Int32 offset)) s_huntOffset = offset; break;
                    case "hunt-schedule": s_huntSchedule = args[i + 1]; break;
                }
            }
            s_maxPlayers = Math.Max(2, Math.Min(250, s_maxPlayers));
            s_antiCheat = Math.Max(0, Math.Min(1000, s_antiCheat));
            s_huntEvery = Math.Max(1, Math.Min(168, s_huntEvery));
            s_huntLength = Math.Max(1, Math.Min(s_huntSchedule.Trim().Length > 0 ? 24 * 60 : s_huntEvery * 60 - 1, s_huntLength));
            s_huntOffset = ((s_huntOffset % 24) + 24) % 24;
            s_serverName = s_serverName.Trim();
            s_adminPassword = s_adminPassword.Trim();
            if (s_serverName.Length > 40)
                s_serverName = s_serverName.Substring(0, 40);
            s_allowedMods = ParseModList(s_allowedModsText);
        }

        // ---------------- Mod pack ----------------

        private static List<String> ParseModList(String text)
        {
            List<String> mods = new List<String>();
            foreach (String part in (text ?? "").Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                String mod = part.Trim().Trim('"').Trim().Replace('\\', '/').TrimEnd('/');
                if (mod.Length > 0 && !mods.Exists(m => String.Equals(m, mod, StringComparison.OrdinalIgnoreCase)))
                    mods.Add(mod);
            }
            return mods;
        }

        /// <summary>
        /// A mod folder of the player's Memoria.ini (FolderNames) is allowed if it is LifaMods, one of the allowed mods,
        /// a sub-folder of one ("ExtraEquipmentMenu/WeaponTransmog/Equipable" for "ExtraEquipmentMenu"), or matches a
        /// name ending with * ("Moguri*" for MoguriMain, MoguriFiles...).
        /// </summary>
        private static Boolean IsModAllowed(String folder)
        {
            if (folder.Length == 0 || String.Equals(folder, "LifaMods", StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (String allowed in s_allowedMods)
            {
                if (allowed.EndsWith("*"))
                {
                    if (folder.StartsWith(allowed.Substring(0, allowed.Length - 1), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (String.Equals(folder, allowed, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(allowed + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The refusal for a game changed outside Memoria's mod list ("foreign" null: a mod without this check)</summary>
        private static String CheckForeign(String foreign)
        {
            if (!s_strictMods)
                return null;
            if (foreign == null)
                return Memoria.Coop.Lang.B("Ton mod LifaMods est trop ancien pour ce serveur : mets-le à jour", "Your LifaMods mod is too old for this server: please update it");
            List<String> added = new List<String>(), changed = new List<String>();
            foreach (String item in foreign.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                String clean = item.Replace("\u001E", "").Replace("\u001F", "").Replace("\u001D", "").Trim();
                if (clean.StartsWith("~"))
                    changed.Add(clean.Substring(1));
                else if (clean.Length > 0)
                    added.Add(clean);
            }
            if (added.Count == 0 && changed.Count == 0)
                return null;
            String fr = "", en = "";
            if (added.Count > 0)
            {
                fr += $"Trouvé hors de la liste des mods de Memoria : {Shorten(added)}. ";
                en += $"Found outside Memoria's mod list: {Shorten(added)}. ";
            }
            if (changed.Count > 0)
            {
                fr += $"Fichiers de Memoria modifiés : {Shorten(changed)}. ";
                en += $"Memoria files changed: {Shorten(changed)}. ";
            }
            fr += "Retire-les (ou réinstalle Memoria) pour te connecter : les mods passent par FolderNames dans Memoria.ini.";
            en += "Remove them (or reinstall Memoria) to connect: mods go through FolderNames in Memoria.ini.";
            return Memoria.Coop.Lang.B(fr, en);
        }

        private static String Shorten(List<String> items)
        {
            const Int32 Shown = 5;
            String text = String.Join(", ", items.GetRange(0, Math.Min(Shown, items.Count)));
            return items.Count > Shown ? text + $" (+{items.Count - Shown})" : text;
        }

        /// <summary>The refusal for a player whose mods aren't all allowed, or null ("mods" null: a mod without the mod list)</summary>
        private static String CheckMods(List<String> mods)
        {
            if (s_allowedMods.Count == 0)
                return null;
            if (mods == null)
                return Memoria.Coop.Lang.B("Ton mod LifaMods est trop ancien pour ce serveur : mets-le à jour", "Your LifaMods mod is too old for this server: please update it");
            List<String> refused = mods.FindAll(m => !IsModAllowed(m));
            if (refused.Count == 0)
                return null;
            String allowed = s_allowedMods.Count > 0 ? String.Join(", ", s_allowedMods) : "-";
            return Memoria.Coop.Lang.B($"Mods non autorisés sur ce serveur : {String.Join(", ", refused)}. Retire-les de Memoria.ini (FolderNames) pour te connecter. Mods autorisés : LifaMods, {allowed}",
                $"Mods not allowed on this server: {String.Join(", ", refused)}. Remove them from Memoria.ini (FolderNames) to connect. Allowed mods: LifaMods, {allowed}");
        }

        private static Int32 ReadInt(String value, Int32 fallback)
        {
            return Int32.TryParse(value, out Int32 result) && result > 0 ? result : fallback;
        }

        private static void RunCommand(String line)
        {
            line = line.TrimStart('/');
            if (line.Length == 0)
                return;
            String command = line.Split(' ')[0].ToLowerInvariant();
            String rest = line.Length > command.Length ? line.Substring(command.Length).Trim() : "";
            switch (command)
            {
                case "stop":
                case "quit":
                case "exit":
                    Log("Arrêt du serveur");
                    s_running = false;
                    try { s_listener.Stop(); } catch { }
                    foreach (Peer peer in Snapshot())
                        peer.Close();
                    Environment.Exit(0);
                    break;
                case "list":
                {
                    List<Peer> peers = Snapshot();
                    Log($"{peers.Count} joueur(s) connecté(s)");
                    foreach (Peer peer in peers)
                        Log(s_capture != null ? $"  [{peer.Id}] {peer.Name}" : $"  [{peer.Id}] {peer.Name} ({peer.Address})"); // admins in the game don't see addresses
                    break;
                }
                case "say":
                {
                    String text = CleanChat(rest);
                    if (text.Length == 0)
                        return;
                    Broadcast(Frame(MessageType.Chat, w => { w.Write(ServerId); WriteString(w, text); }), null);
                    Log($"[Serveur] {text}");
                    break;
                }
                case "kick":
                {
                    Peer target = Snapshot().Find(p => p.Id.ToString() == rest || String.Equals(p.Name, rest, StringComparison.OrdinalIgnoreCase));
                    if (target == null)
                    {
                        Log($"Joueur introuvable : {rest}");
                        return;
                    }
                    target.Send(Frame(MessageType.Reject, w => WriteString(w, Memoria.Coop.Lang.B("Exclu par le serveur", "Kicked by the server"))));
                    Thread.Sleep(300);
                    RemovePeer(target);
                    break;
                }
                case "unblock":
                {
                    // A player blocked for playing an older save (or a new game): their current game is accepted
                    if (rest.Length == 0 || !s_banks.Unblock(rest))
                    {
                        Log($"Aucun blocage pour : {rest}");
                        return;
                    }
                    Log($"{rest} est débloqué (sa partie actuelle devient sa référence)");
                    Byte[] status = Memoria.Coop.ClanBankStore.WriteSaveStatus(new Memoria.Coop.ClanBankStore.SaveRecord());
                    Peer target = Snapshot().Find(p => p.Welcomed && String.Equals(p.Name, rest, StringComparison.OrdinalIgnoreCase));
                    target?.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)status.Length); w.Write(status); }));
                    break;
                }
                case "hunt":
                {
                    // hunt = state, hunt start [minutes], hunt end
                    String[] words = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    String action = words.Length > 0 ? words[0].ToLowerInvariant() : "";
                    if (action == "start")
                        Log(s_hunt.Start(words.Length > 1 && Int32.TryParse(words[1], out Int32 minutes) ? minutes : 0));
                    else if (action == "end" || action == "stop")
                        Log(s_hunt.Stop());
                    else
                        Log(s_hunt.Describe() + " (hunt start [minutes], hunt end)");
                    break;
                }
                case "give":
                {
                    // give <pseudo> gil <amount> | item <id> [count] | card <id> [count] | list
                    Log(s_gifts.Command(rest, "", out String giftTarget));
                    if (giftTarget == null)
                        return;
                    Peer target = Snapshot().Find(p => p.Welcomed && String.Equals(p.Name, giftTarget, StringComparison.OrdinalIgnoreCase));
                    if (target != null)
                    {
                        Byte[] state = s_gifts.BuildState(target.Name);
                        target.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)state.Length); w.Write(state); }));
                    }
                    break;
                }
                case "admin":
                {
                    // admin = list, admin remove <pseudo>
                    if (rest.StartsWith("remove ", StringComparison.OrdinalIgnoreCase))
                    {
                        String name = rest.Substring(7).Trim();
                        if (!s_admins.Remove(name))
                        {
                            Log($"{name} n'est pas admin");
                            return;
                        }
                        Log($"{name} n'est plus admin");
                        Peer target = Snapshot().Find(p => p.Welcomed && String.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                        if (target != null && target.IsAdmin)
                        {
                            target.IsAdmin = false;
                            SendAuth(target, false, "", Memoria.Coop.Lang.B("Tu n'es plus admin du serveur", "You are no longer an admin of the server"));
                            BroadcastAdmins();
                        }
                        return;
                    }
                    List<String> names = s_admins.Names();
                    Log(names.Count == 0 ? "Aucun admin (un joueur le devient avec /admin <mot de passe> dans le tchat)" : $"Admins : {String.Join(", ", names)} (admin remove <pseudo> pour retirer)");
                    break;
                }
                case "whois":
                case "qui":
                    Log(rest.Length == 0 ? "whois <pseudo ou début du code>" : s_players.Whois(rest, s_capture == null)); // admins in the game don't see addresses
                    break;
                case "players":
                case "joueurs":
                {
                    List<String> lines = s_players.Recent(20, s_capture == null);
                    Log($"{s_players.Count} joueur(s) connu(s), les {lines.Count} dernier(s) vus :");
                    foreach (String line2 in lines)
                        Log(line2);
                    break;
                }
                case "ban":
                case "bannir":
                {
                    // ban <pseudo or code> [reason]: the pseudo may hold spaces, so the longest known start wins
                    String[] words = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 0)
                    {
                        Log("ban <pseudo ou début du code> [raison]");
                        return;
                    }
                    List<String> names = null;
                    String result = null;
                    for (Int32 n = words.Length; n >= 1 && names == null; n--)
                    {
                        String who = String.Join(" ", words, 0, n);
                        String reason = String.Join(" ", words, n, words.Length - n);
                        names = s_players.Ban(who, reason, out result);
                    }
                    Log(result);
                    if (names == null)
                        return;
                    foreach (Peer target in Snapshot())
                        if (target.Welcomed && names.Exists(n => String.Equals(n, target.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            target.Send(Frame(MessageType.Reject, w => WriteString(w, Memoria.Coop.Lang.B("Tu es banni de ce serveur", "You are banned from this server"))));
                            Thread.Sleep(300);
                            RemovePeer(target);
                        }
                    break;
                }
                case "unban":
                case "debannir":
                    Log(rest.Length == 0 ? "unban <pseudo ou début du code>" : s_players.Unban(rest));
                    break;
                case "release":
                case "liberer":
                    Log(rest.Length == 0 ? "release <pseudo> : libère un pseudo (joueur qui a perdu son code en réinstallant)" : s_players.Release(rest));
                    break;
                case "ranking":
                {
                    if (rest != "reset")
                    {
                        Log("ranking reset : remet le classement des duels à zéro");
                        return;
                    }
                    s_ranking.Reset();
                    Log("Classement des duels remis à zéro");
                    break;
                }
                default:
                    Log("Commandes : list, say <texte>, kick <pseudo ou id>, unblock <pseudo>, ranking reset, hunt [start|end], give <pseudo> gil|item|card ..., admin [remove <pseudo>], stop");
                    break;
            }
        }

        // ---------------- Connections ----------------

        private static void AcceptLoop()
        {
            while (s_running)
            {
                TcpClient tcp;
                try
                {
                    tcp = s_listener.AcceptTcpClient();
                }
                catch (Exception ex)
                {
                    if (s_running)
                        Log($"Erreur d'acceptation : {ex.Message}");
                    return;
                }
                Peer peer = new Peer(tcp);
                new Thread(() => PeerLoop(peer)) { IsBackground = true, Name = "peer" }.Start();
            }
        }

        private static void PeerLoop(Peer peer)
        {
            try
            {
                while (s_running)
                {
                    Byte[] frame = ReadFrame(peer.Stream);
                    if (frame == null)
                        break;
                    MessageType type = (MessageType)frame[0];
                    using (BinaryReader r = new BinaryReader(new MemoryStream(frame, 1, frame.Length - 1)))
                    {
                        if (type == MessageType.Hello && !peer.Welcomed)
                        {
                            Int32 version = r.ReadInt32();
                            String name = CleanChat(ReadString(r));
                            // The player's secret code comes after the name (empty the first time; mods before it send nothing)
                            String code = r.BaseStream.Position < r.BaseStream.Length ? ReadString(r).Trim().ToLowerInvariant() : null;
                            // ...then the mod folders of their Memoria.ini (mods before it send nothing)
                            List<String> mods = r.BaseStream.Position < r.BaseStream.Length ? ParseModList(ReadString(r)) : null;
                            // ...then what changes the game outside that list (mods before it send nothing)
                            String foreign = r.BaseStream.Position < r.BaseStream.Length ? ReadString(r) : null;
                            if (!OnHello(peer, version, name, code, mods, foreign))
                                break;
                        }
                        else if (!peer.Welcomed)
                        {
                            break;
                        }
                        else if (type == MessageType.PlayerState && frame.Length > 1)
                        {
                            // [type][player id][state...]: never trust the id sent by a client
                            Byte[] copy = (Byte[])frame.Clone();
                            copy[1] = peer.Id;
                            Broadcast(WithHeader(copy), peer);
                        }
                        else if (type == MessageType.Chat)
                        {
                            String text = CleanChat(ReadString(r));
                            if (text.Length == 0)
                                continue;
                            Log($"[{peer.Name}] {text}");
                            Broadcast(Frame(MessageType.Chat, w => { w.Write(peer.Id); WriteString(w, text); }), peer);
                        }
                        else if (type == MessageType.Duel)
                        {
                            Byte target = r.ReadByte();
                            Byte[] data = r.ReadBytes(r.ReadUInt16());
                            if (target == ServerId)
                            {
                                // A clan bank request: the server keeps the banks
                                if (data.Length > 0 && Memoria.Coop.ClanBankStore.IsRequest(data[0]))
                                    foreach (Byte[] reply in s_banks.Handle(peer.Name, data))
                                        peer.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)reply.Length); w.Write(reply); }));
                                // The duel ranking: the answers may also go to the opponent
                                // (a player blocked for an older save or a suspect game doesn't add results)
                                else if (data.Length > 0 && Memoria.Coop.RankingStore.IsRequest(data[0]) && !(data[0] == Memoria.Coop.RankingStore.MsgReport && s_banks.IsBlocked(peer.Name)))
                                    foreach (KeyValuePair<String, Byte[]> reply in s_ranking.Handle(peer.Name, data))
                                    {
                                        Peer receiver = Snapshot().Find(p => p.Welcomed && String.Equals(p.Name, reply.Key, StringComparison.OrdinalIgnoreCase));
                                        receiver?.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)reply.Value.Length); w.Write(reply.Value); }));
                                    }
                                // The number 1 of each ranking, for the ornaments on their names
                                else if (data.Length > 0 && data[0] == Memoria.Coop.RankingStore.MsgChampionsGet)
                                {
                                    Byte[] reply = Memoria.Coop.RankingStore.BuildChampions(s_ranking.Champion(Memoria.Coop.RankingStore.KindBattle), s_ranking.Champion(Memoria.Coop.RankingStore.KindTetra), s_hunt.Champion());
                                    peer.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)reply.Length); w.Write(reply); }));
                                }
                                // The Fête de la chasse (a blocked player's points don't count)
                                else if (data.Length > 0 && Memoria.Coop.HuntStore.IsRequest(data[0]) && !(data[0] == Memoria.Coop.HuntStore.MsgScore && s_banks.IsBlocked(peer.Name)))
                                {
                                    Byte[] reply = s_hunt.Handle(peer.Name, data);
                                    if (reply != null)
                                        peer.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)reply.Length); w.Write(reply); }));
                                }
                                // Admin role, commands and channel
                                else if (data.Length > 0 && Memoria.Coop.AdminProtocol.IsRequest(data[0]))
                                    HandleAdmin(peer, data);
                                // Admin gifts waiting for this player
                                else if (data.Length > 0 && Memoria.Coop.GiftStore.IsRequest(data[0]))
                                {
                                    Byte[] reply = s_gifts.Handle(peer.Name, data);
                                    if (reply != null)
                                        peer.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)reply.Length); w.Write(reply); }));
                                }
                                continue;
                            }
                            Peer to = Snapshot().Find(p => p.Id == target && p.Welcomed);
                            to?.Send(Frame(MessageType.Duel, w => { w.Write(peer.Id); w.Write((UInt16)data.Length); w.Write(data); }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (s_running && peer.Welcomed)
                    Log($"{peer.Name} déconnecté : {ex.Message}");
            }
            RemovePeer(peer);
        }

        private static String AddressOnly(String address)
        {
            return address.Contains(':') ? address.Substring(0, address.LastIndexOf(':')) : address;
        }

        private static Boolean OnHello(Peer peer, Int32 version, String name, String code, List<String> mods, String foreign)
        {
            String refusal = null;
            List<Peer> others;
            lock (Lock)
            {
                others = new List<Peer>(Peers);
                if (version != ProtocolVersion)
                    refusal = Memoria.Coop.Lang.B($"Version du mod différente (serveur {ProtocolVersion}, toi {version})", $"Different mod version (server {ProtocolVersion}, you {version})");
                else if (String.IsNullOrWhiteSpace(name))
                    refusal = Memoria.Coop.Lang.B("Choisis un pseudo dans le menu titre", "Choose a name on the title screen");
                else if ((refusal = CheckMods(mods)) != null || (refusal = CheckForeign(foreign)) != null)
                {
                }
                else if (Peers.Count >= s_maxPlayers)
                    refusal = Memoria.Coop.Lang.B("Serveur plein", "Server full");
                else if (Peers.Exists(p => String.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                    refusal = Memoria.Coop.Lang.B($"Le pseudo {name} est déjà connecté : choisis un autre pseudo dans le menu titre", $"The name {name} is already connected: choose another name on the title screen");
                else if ((refusal = s_players.Arrive(ref code, name, AddressOnly(peer.Address))) != null)
                {
                }
                else
                {
                    peer.Code = code;
                    peer.Id = FreeId();
                    peer.Name = name;
                    Peers.Add(peer);
                }
            }
            if (refusal != null)
            {
                Log($"Connexion refusée pour {name} ({peer.Address}) : {refusal}");
                peer.Send(Frame(MessageType.Reject, w => WriteString(w, refusal)));
                Thread.Sleep(500); // let the sending thread deliver the reason
                return false;
            }
            // The server's name comes after the original fields: older mods simply don't read it
            // ...and the player's code, which their game keeps (older mods don't read it)
            peer.Send(Frame(MessageType.Welcome, w => { w.Write(peer.Id); w.Write(s_noCheats); w.Write(true); WriteString(w, s_serverName); WriteString(w, peer.Code); }));
            peer.Welcomed = true;
            foreach (Peer other in others)
                peer.Send(Frame(MessageType.PlayerJoin, w => { w.Write(other.Id); WriteString(w, other.Name); }));
            Broadcast(Frame(MessageType.PlayerJoin, w => { w.Write(peer.Id); WriteString(w, name); }), peer);
            Log($"{name} connecté (id {peer.Id}, {peer.Address}), {others.Count + 1} joueur(s)");
            SendDuel(peer, AdminList());
            return true;
        }

        // ---------------- Admins ----------------

        private static void SendDuel(Peer peer, Byte[] data)
        {
            peer.Send(Frame(MessageType.Duel, w => { w.Write(ServerId); w.Write((UInt16)data.Length); w.Write(data); }));
        }

        private static void SendAuth(Peer peer, Boolean admin, String token, String text)
        {
            SendDuel(peer, Memoria.Coop.AdminProtocol.Build(Memoria.Coop.AdminProtocol.MsgAuth, w =>
            {
                w.Write(admin);
                Memoria.Coop.ClanBankStore.WriteString(w, token);
                Memoria.Coop.AdminProtocol.WriteText(w, text);
            }));
        }

        /// <summary>The admins online, for the star in front of their chat lines</summary>
        private static Byte[] AdminList()
        {
            List<Peer> admins = Snapshot().FindAll(p => p.Welcomed && p.IsAdmin);
            return Memoria.Coop.AdminProtocol.Build(Memoria.Coop.AdminProtocol.MsgAdmins, w =>
            {
                w.Write((Byte)Math.Min(admins.Count, 255));
                for (Int32 i = 0; i < admins.Count && i < 255; i++)
                    Memoria.Coop.ClanBankStore.WriteString(w, admins[i].Name);
            });
        }

        private static void BroadcastAdmins()
        {
            Byte[] list = AdminList();
            foreach (Peer peer in Snapshot())
                if (peer.Welcomed)
                    SendDuel(peer, list);
        }

        private static void HandleAdmin(Peer peer, Byte[] data)
        {
            try
            {
                using (BinaryReader r = new BinaryReader(new MemoryStream(data)))
                {
                    Byte type = r.ReadByte();
                    if (type == Memoria.Coop.AdminProtocol.MsgLogin)
                    {
                        String password = Memoria.Coop.ClanBankStore.ReadString(r);
                        if (password.Length == 0)
                        {
                            // "/admin off": the role is given up for good
                            Boolean was = peer.IsAdmin;
                            s_admins.Remove(peer.Name);
                            peer.IsAdmin = false;
                            SendAuth(peer, false, "", Memoria.Coop.Lang.B("Tu n'es plus admin du serveur", "You are no longer an admin of the server"));
                            if (was)
                            {
                                Log($"[Admin] {peer.Name} a quitté le rôle d'admin");
                                BroadcastAdmins();
                            }
                            return;
                        }
                        String ip = peer.Address.Contains(':') ? peer.Address.Substring(0, peer.Address.LastIndexOf(':')) : peer.Address;
                        KeyValuePair<Int32, DateTime> failures;
                        lock (s_loginFailures)
                            s_loginFailures.TryGetValue(ip, out failures);
                        if (DateTime.UtcNow < failures.Value)
                        {
                            SendAuth(peer, peer.IsAdmin, "", Memoria.Coop.Lang.B("Trop d'essais : réessaie dans quelques minutes", "Too many tries: try again in a few minutes"));
                            return;
                        }
                        if (s_adminPassword.Length == 0 || !FixedEquals(password, s_adminPassword))
                        {
                            Int32 count = failures.Key + 1;
                            lock (s_loginFailures)
                                s_loginFailures[ip] = count >= MaxLoginFailures ? new KeyValuePair<Int32, DateTime>(0, DateTime.UtcNow.AddMinutes(10)) : new KeyValuePair<Int32, DateTime>(count, DateTime.MinValue);
                            Log($"[Admin] Mauvais mot de passe admin de {peer.Name} ({peer.Address})");
                            SendAuth(peer, peer.IsAdmin, "", s_adminPassword.Length == 0
                                ? Memoria.Coop.Lang.B("Ce serveur n'a pas de mot de passe admin", "This server has no admin password")
                                : Memoria.Coop.Lang.B("Mot de passe admin incorrect", "Wrong admin password"));
                            return;
                        }
                        lock (s_loginFailures)
                            s_loginFailures.Remove(ip);
                        String token = s_admins.Add(peer.Name);
                        peer.IsAdmin = true;
                        Log($"[Admin] {peer.Name} est maintenant admin");
                        SendAuth(peer, true, token, Memoria.Coop.Lang.B("Tu es admin du serveur ! /a <texte> : canal des admins, /admin : aide", "You are an admin of the server! /a <text>: admins' channel, /admin: help"));
                        BroadcastAdmins();
                        return;
                    }
                    if (type == Memoria.Coop.AdminProtocol.MsgResume)
                    {
                        String token = Memoria.Coop.ClanBankStore.ReadString(r);
                        if (s_admins.Check(peer.Name, token))
                        {
                            peer.IsAdmin = true;
                            SendAuth(peer, true, token, Memoria.Coop.Lang.B("Connecté en admin", "Connected as an admin"));
                            BroadcastAdmins();
                        }
                        else
                        {
                            SendAuth(peer, false, "", "");
                        }
                        return;
                    }
                    if (!peer.IsAdmin)
                        return;
                    if (type == Memoria.Coop.AdminProtocol.MsgChat)
                    {
                        String text = CleanChat(Memoria.Coop.AdminProtocol.ReadText(r));
                        if (text.Length == 0)
                            return;
                        Log($"[Admin] [{peer.Name}] {text}");
                        Byte[] line = Memoria.Coop.AdminProtocol.Build(Memoria.Coop.AdminProtocol.MsgChatLine, w =>
                        {
                            Memoria.Coop.ClanBankStore.WriteString(w, peer.Name);
                            Memoria.Coop.AdminProtocol.WriteText(w, text);
                        });
                        foreach (Peer admin in Snapshot())
                            if (admin.Welcomed && admin.IsAdmin)
                                SendDuel(admin, line);
                        return;
                    }
                    if (type == Memoria.Coop.AdminProtocol.MsgCommand)
                    {
                        String command = Memoria.Coop.AdminProtocol.ReadText(r).Trim().TrimStart('/');
                        String word = command.Split(' ')[0].ToLowerInvariant();
                        if (word == "stop" || word == "quit" || word == "exit" || word == "admin")
                            return; // console only
                        Log($"[Admin] {peer.Name} : {command}");
                        s_capture = new StringBuilder();
                        String output;
                        try
                        {
                            RunCommand(command);
                        }
                        finally
                        {
                            output = s_capture.ToString();
                            s_capture = null;
                        }
                        SendDuel(peer, Memoria.Coop.AdminProtocol.Build(Memoria.Coop.AdminProtocol.MsgOutput, w => Memoria.Coop.AdminProtocol.WriteText(w, output)));
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Bad admin request from {peer.Name}: {ex.Message}");
            }
        }

        private static Boolean FixedEquals(String a, String b)
        {
            Byte[] x = Encoding.UTF8.GetBytes(a);
            Byte[] y = Encoding.UTF8.GetBytes(b);
            return x.Length == y.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(x, y);
        }

        /// <summary>Lowest free id: the server runs for days, ids must come back.</summary>
        private static Byte FreeId()
        {
            for (Byte id = 1; id < 255; id++)
                if (!Peers.Exists(p => p.Id == id))
                    return id;
            return 254;
        }

        private static void RemovePeer(Peer peer)
        {
            Boolean removed;
            lock (Lock)
                removed = Peers.Remove(peer);
            peer.Close();
            if (!removed)
                return;
            Broadcast(Frame(MessageType.PlayerLeave, w => w.Write(peer.Id)), null);
            Log($"{peer.Name} est parti, {Snapshot().Count} joueur(s)");
            if (peer.IsAdmin)
                BroadcastAdmins();
        }

        private static List<Peer> Snapshot()
        {
            lock (Lock)
                return new List<Peer>(Peers);
        }

        private static void Broadcast(Byte[] frame, Peer except)
        {
            foreach (Peer peer in Snapshot())
                if (peer != except && peer.Welcomed && !peer.Send(frame))
                    new Thread(() => RemovePeer(peer)) { IsBackground = true }.Start();
        }

        // ---------------- Frames: [UInt16 length][Byte type][payload] ----------------

        private static Byte[] Frame(MessageType type, Action<BinaryWriter> payload)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write((Byte)type);
                payload(w);
                w.Flush();
                return WithHeader(ms.ToArray());
            }
        }

        private static Byte[] WithHeader(Byte[] body)
        {
            Byte[] data = new Byte[body.Length + 2];
            data[0] = (Byte)(body.Length & 0xFF);
            data[1] = (Byte)(body.Length >> 8);
            Buffer.BlockCopy(body, 0, data, 2, body.Length);
            return data;
        }

        private static Byte[] ReadFrame(Stream stream)
        {
            Byte[] header = ReadExactly(stream, 2);
            if (header == null)
                return null;
            Int32 length = header[0] | (header[1] << 8);
            if (length <= 0 || length > MaxFrameSize)
                throw new IOException($"taille de message invalide ({length})");
            return ReadExactly(stream, length);
        }

        private static Byte[] ReadExactly(Stream stream, Int32 count)
        {
            Byte[] buffer = new Byte[count];
            Int32 offset = 0;
            while (offset < count)
            {
                Int32 read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    return null;
                offset += read;
            }
            return buffer;
        }

        private static void WriteString(BinaryWriter w, String s)
        {
            Byte[] bytes = Encoding.UTF8.GetBytes(s ?? String.Empty);
            w.Write((UInt16)bytes.Length);
            w.Write(bytes);
        }

        private static String ReadString(BinaryReader r)
        {
            return Encoding.UTF8.GetString(r.ReadBytes(r.ReadUInt16()));
        }

        private static String CleanChat(String text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Char c in text ?? String.Empty)
                if (!Char.IsControl(c) || Memoria.Coop.Lang.IsMark(c))
                    sb.Append(c);
            String clean = sb.ToString().Trim();
            Int32 max = Memoria.Coop.Lang.HasBoth(clean) ? MaxChatLength * 3 : MaxChatLength; // a text in both languages (Lang.B) is longer
            return clean.Length > max ? clean.Substring(0, max) : clean;
        }

        private static void Log(String text)
        {
            s_capture?.Append(text).Append('\n');
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {Memoria.Coop.Lang.Pick(text)}");
        }

        /// <summary>A connection with its own sending thread, so a slow player never delays the others.</summary>
        private sealed class Peer
        {
            public readonly NetworkStream Stream;
            public readonly String Address;
            private readonly TcpClient _tcp;
            private readonly Queue<Byte[]> _outgoing = new Queue<Byte[]>();
            private readonly AutoResetEvent _hasOutgoing = new AutoResetEvent(false);
            private volatile Boolean _closed;
            public Byte Id;
            public String Name = "?";
            public String Code = "";
            public volatile Boolean Welcomed;
            public volatile Boolean IsAdmin;

            public Peer(TcpClient tcp)
            {
                _tcp = tcp;
                _tcp.NoDelay = true;
                _tcp.SendTimeout = 5000;
                Stream = tcp.GetStream();
                Address = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
                new Thread(SendLoop) { IsBackground = true, Name = "send" }.Start();
            }

            public Boolean Send(Byte[] frame)
            {
                if (_closed)
                    return false;
                lock (_outgoing)
                {
                    if (_outgoing.Count > 256)
                        _outgoing.Dequeue();
                    _outgoing.Enqueue(frame);
                }
                _hasOutgoing.Set();
                return true;
            }

            private void SendLoop()
            {
                try
                {
                    while (!_closed)
                    {
                        _hasOutgoing.WaitOne(500);
                        while (true)
                        {
                            Byte[] frame;
                            lock (_outgoing)
                            {
                                if (_outgoing.Count == 0)
                                    break;
                                frame = _outgoing.Dequeue();
                            }
                            Stream.Write(frame, 0, frame.Length);
                        }
                    }
                }
                catch
                {
                    Close();
                }
            }

            public void Close()
            {
                _closed = true;
                _hasOutgoing.Set();
                try { _tcp.Close(); } catch { }
            }
        }
    }
}
