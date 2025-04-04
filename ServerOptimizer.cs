using System;
using System.Collections.Generic;
using Carbon.Core;
using Carbon.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Carbon.Plugins
{
    [Info("ServerOptimizer", "Jaie55", "1.0.1")]
    [Description("Dynamically optimizes server FPS based on player count to improve performance")]
    public class ServerOptimizer : CarbonPlugin
    {
        #region Fields

        private Configuration _config;
        private object _fpsCheckTimer;
        private int _currentFps;
        private readonly List<string> _supportedLanguages = new List<string> { 
            "en", "es", "es-ES", "ca", "af", "ar", "zh-CN", "zh-TW", "cs", 
            "da", "nl", "fi", "fr", "de", "el", "he", "hu", "it", "ja", 
            "ko", "no", "en-PT", "pl", "pt", "pt-BR", "ro", "ru", "sr-Cyrl", 
            "sv", "tr", "uk", "vi" 
        };

        #endregion

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Enable dynamic FPS adjustment")]
            public bool DynamicFpsEnabled = true;

            [JsonProperty("FPS when server is empty")]
            public int EmptyServerFps = 9;

            [JsonProperty("Base FPS with at least one player")]
            public int BaseFps = 26;

            [JsonProperty("Server maximum FPS")]
            public int MaxFps = 60;

            [JsonProperty("FPS increment per player")]
            public float FpsIncrementPerPlayer = 1.5f;

            [JsonProperty("Check interval (seconds)")]
            public float CheckInterval = 30f;

            [JsonProperty("Show debug messages")]
            public bool ShowDebugMessages = false;
            
            [JsonProperty("Notify when FPS changes")]
            public bool NotifyFpsChange = true;

            [JsonProperty("Chat prefix")]
            public string ChatPrefix = "[<color=#00AAFF>ServerOptimizer</color>] ";
            
            [JsonProperty("Default language")]
            public string DefaultLanguage = "en";

            [JsonProperty("Force initialization")]
            public bool ForceInitialization = true;
            
            [JsonProperty("Show initialization message")]
            public bool ShowInitMessage = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                PrintError("Error loading configuration: " + ex.Message);
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating default configuration");
            _config = new Configuration();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Hooks

        private void Init()
        {
            PrintWarning("Loading ServerOptimizer...");
            RegisterPermissions();
            StartFpsCheckTimer();
            Unsubscribe(nameof(OnPlayerConnected));
            Unsubscribe(nameof(OnPlayerDisconnected));
            Subscribe(nameof(OnPlayerConnected));
            Subscribe(nameof(OnPlayerDisconnected));
            
            if (_config.ShowInitMessage)
            {
                PrintWarning($"ServerOptimizer v1.0.0 initialized - Current FPS: {_currentFps}");
            }
            
            PrintWarning("ServerOptimizer has been loaded successfully - Optimized for Raw Community");
            LoadLanguages();
            
            if (_config.ForceInitialization)
            {
                timer.Once(1f, () => {
                    AdjustServerFps();
                    PrintWarning($"FPS initialized again: {_currentFps}");
                });
            }
        }

        private void Unload()
        {
            if (_fpsCheckTimer != null)
            {
                _fpsCheckTimer = null;
            }
            
            ConsoleSystem.Run(ConsoleSystem.Option.Server, "fps.limit", 60);
            PrintWarning("ServerOptimizer has been unloaded successfully");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (_config.DynamicFpsEnabled)
            {
                AdjustServerFps();
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (_config.DynamicFpsEnabled)
            {
                AdjustServerFps();
            }
        }

        #endregion

        #region FPS Management

        private void StartFpsCheckTimer()
        {
            AdjustServerFps();
            _fpsCheckTimer = timer.Every(_config.CheckInterval, () => AdjustServerFps());
        }

        private void AdjustServerFps()
        {
            int playerCount = BasePlayer.activePlayerList?.Count ?? 0;
            int newFps;
            
            if (playerCount == 0)
            {
                newFps = _config.EmptyServerFps;
            }
            else
            {
                newFps = Mathf.Min(
                    _config.BaseFps + Mathf.FloorToInt(playerCount * _config.FpsIncrementPerPlayer),
                    _config.MaxFps
                );
            }
            
            if (newFps != _currentFps)
            {
                if (_config.ShowDebugMessages)
                {
                    PrintWarning($"Adjusting server FPS: {_currentFps} -> {newFps} (Players: {playerCount})");
                }
                
                _currentFps = newFps;
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "fps.limit", newFps);
                
                if (_config.NotifyFpsChange && playerCount > 0)
                {
                    foreach (var basePlayer in BasePlayer.activePlayerList)
                    {
                        if (basePlayer.IsAdmin || basePlayer.IsDeveloper)
                        {
                            basePlayer.ChatMessage($"{_config.ChatPrefix}{GetLang("FPS Adjusted", basePlayer.UserIDString).Replace("{0}", newFps.ToString()).Replace("{1}", playerCount.ToString())}");
                        }
                    }
                }
            }
        }

        #endregion
        
        #region Permissions

        private void RegisterPermissions()
        {
            permission.RegisterPermission("serveroptimizer.admin", this);
            permission.RegisterPermission("serveroptimizer.status", this);
            permission.RegisterPermission("serveroptimizer.toggle", this);
            
            PrintWarning("ServerOptimizer permissions registered:");
            PrintWarning("- serveroptimizer.admin: Access to all plugin commands");
            PrintWarning("- serveroptimizer.status: View current FPS status");
            PrintWarning("- serveroptimizer.toggle: Enable/disable dynamic adjustment");
        }

        private bool HasPermission(BasePlayer player, string perm)
        {
            if (player == null) return false;
            
            return player.IsAdmin || permission.UserHasPermission(player.UserIDString, "serveroptimizer.admin") || permission.UserHasPermission(player.UserIDString, perm);
        }

        #endregion
        
        #region Commands
        
        [Command("serveroptimizer.status", "so.status")]
        private void CmdStatus(BasePlayer player)
        {
            if (!HasPermission(player, "serveroptimizer.status"))
            {
                SendReply(player, GetLang("No Permission", player.UserIDString));
                return;
            }

            int playerCount = BasePlayer.activePlayerList?.Count ?? 0;
            SendReply(player, $"{_config.ChatPrefix}{GetLang("Status Header", player.UserIDString)}");
            SendReply(player, $"{GetLang("Status Current FPS", player.UserIDString).Replace("{0}", _currentFps.ToString())}");
            SendReply(player, $"{GetLang("Status Player Count", player.UserIDString).Replace("{0}", playerCount.ToString())}");
            SendReply(player, $"{GetLang("Status Dynamic Mode", player.UserIDString).Replace("{0}", _config.DynamicFpsEnabled ? GetLang("Enabled", player.UserIDString) : GetLang("Disabled", player.UserIDString))}");
        }

        [Command("serveroptimizer.toggle", "so.toggle")]
        private void CmdToggle(BasePlayer player)
        {
            if (!HasPermission(player, "serveroptimizer.toggle"))
            {
                SendReply(player, GetLang("No Permission", player.UserIDString));
                return;
            }

            _config.DynamicFpsEnabled = !_config.DynamicFpsEnabled;
            SaveConfig();
            
            if (_config.DynamicFpsEnabled)
            {
                SendReply(player, $"{_config.ChatPrefix}{GetLang("Toggle On", player.UserIDString)}");
                AdjustServerFps();
            }
            else
            {
                int maxFps = _config.MaxFps;
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "fps.limit", maxFps);
                _currentFps = maxFps;
                SendReply(player, $"{_config.ChatPrefix}{GetLang("Toggle Off", player.UserIDString)}");
                SendReply(player, $"{_config.ChatPrefix}{GetLang("Default FPS Restored", player.UserIDString).Replace("{0}", maxFps.ToString())}");
            }
        }
        
        [Command("serveroptimizer.help", "so.help", "so")]
        private void CmdHelp(BasePlayer player)
        {
            if (player == null)
            {
                Puts("ServerOptimizer Help (Console):");
                Puts("- so.status - View current server status");
                Puts("- so.toggle - Enable/disable dynamic adjustment");
                Puts("- so.help - View this help");
                Puts($"Current FPS: {_currentFps}, Dynamic adjustment: {(_config.DynamicFpsEnabled ? "Enabled" : "Disabled")}");
                return;
            }

            SendReply(player, $"{_config.ChatPrefix}{GetLang("Help Header", player.UserIDString)}");
            SendReply(player, "");
            SendReply(player, $"<color=#FFAA00>Available commands:</color>");
            SendReply(player, "- <color=#00AAFF>/so.status</color> - View current server status");
            SendReply(player, "- <color=#00AAFF>/so.toggle</color> - Enable/disable dynamic adjustment");
            SendReply(player, "- <color=#00AAFF>/so.help</color> - View this help");
            SendReply(player, "");
            SendReply(player, $"<color=#FFAA00>Permissions:</color>");
            SendReply(player, "- serveroptimizer.admin: Access to all commands");
            SendReply(player, "- serveroptimizer.status: View FPS status");
            SendReply(player, "- serveroptimizer.toggle: Enable/disable adjustment");
            SendReply(player, "");
            SendReply(player, $"<color=#FFAA00>Server FPS:</color> {_currentFps}");
            
            if (player.IsAdmin)
            {
                int playerCount = BasePlayer.activePlayerList?.Count ?? 0;
                SendReply(player, "");
                SendReply(player, "<color=#FFAA00>Admin information:</color>");
                SendReply(player, $"- Current FPS: {_currentFps}");
                SendReply(player, $"- Players: {playerCount}");
                SendReply(player, $"- Dynamic adjustment: {(_config.DynamicFpsEnabled ? "<color=green>Enabled</color>" : "<color=red>Disabled</color>")}");
                SendReply(player, $"- Base FPS: {_config.BaseFps}");
                SendReply(player, $"- Maximum FPS: {_config.MaxFps}");
                SendReply(player, $"- FPS without players: {_config.EmptyServerFps}");
                SendReply(player, $"- Increment per player: {_config.FpsIncrementPerPlayer}");
            }
        }
        
        #endregion
        
        #region Localization
        
        private void LoadLanguages()
        {
            foreach (string language in _supportedLanguages)
            {
                LoadLanguageMessages(language);
            }
            
            PrintWarning($"Default language: {_config.DefaultLanguage}");
            
            if (!_supportedLanguages.Contains(_config.DefaultLanguage, StringComparer.OrdinalIgnoreCase))
            {
                PrintWarning($"Warning: The configured language '{_config.DefaultLanguage}' is not supported. Using 'en' instead.");
                _config.DefaultLanguage = "en";
                SaveConfig();
            }
        }

        private void LoadLanguageMessages(string language)
        {
            var messages = new Dictionary<string, string>();
            
            switch (language)
            {
                case "en":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "You don't have permission to use this command",
                        ["FPS Adjusted"] = "Server FPS adjusted to {0} ({1} players online)",
                        ["Status Header"] = "ServerOptimizer Status:",
                        ["Status Current FPS"] = "- Current FPS: {0}",
                        ["Status Player Count"] = "- Players Online: {0}",
                        ["Status Dynamic Mode"] = "- Dynamic adjustment: {0}",
                        ["Enabled"] = "Enabled",
                        ["Disabled"] = "Disabled",
                        ["Toggle On"] = "Dynamic FPS adjustment enabled",
                        ["Toggle Off"] = "Dynamic FPS adjustment disabled",
                        ["Default FPS Restored"] = "FPS restored to {0}",
                        ["Help Header"] = "ServerOptimizer Help:"
                    };
                    break;
                
                case "en-PT":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "Ye don't have permission to use this command, matey!",
                        ["FPS Adjusted"] = "Ship's FPS be adjusted to {0} ({1} sailors aboard)",
                        ["Status Header"] = "ServerOptimizer Ship's Log:",
                        ["Status Current FPS"] = "- Current FPS: {0}",
                        ["Status Player Count"] = "- Crew Members: {0}",
                        ["Status Dynamic Mode"] = "- Dynamic adjustment: {0}",
                        ["Enabled"] = "Aye",
                        ["Disabled"] = "Nay",
                        ["Toggle On"] = "Dynamic FPS adjustment be enabled, arr!",
                        ["Toggle Off"] = "Dynamic FPS adjustment be disabled, arr!",
                        ["Default FPS Restored"] = "FPS restored to {0}, full sail ahead!",
                        ["Help Header"] = "ServerOptimizer Treasure Map:"
                    };
                    break;
                
                case "es":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "No tienes permiso para usar este comando",
                        ["FPS Adjusted"] = "FPS del servidor ajustado a {0} ({1} jugadores online)",
                        ["Status Header"] = "Estado del ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS Actual: {0}",
                        ["Status Player Count"] = "- Jugadores Online: {0}",
                        ["Status Dynamic Mode"] = "- Ajuste dinámico: {0}",
                        ["Enabled"] = "Activado",
                        ["Disabled"] = "Desactivado",
                        ["Toggle On"] = "Ajuste dinámico de FPS activado",
                        ["Toggle Off"] = "Ajuste dinámico de FPS desactivado",
                        ["Default FPS Restored"] = "FPS restaurado a {0}",
                        ["Help Header"] = "Ayuda de ServerOptimizer:"
                    };
                    break;
                
                case "es-ES":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "No tienes permiso para utilizar este comando",
                        ["FPS Adjusted"] = "FPS del servidor ajustados a {0} ({1} jugadores conectados)",
                        ["Status Header"] = "Estado del ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS Actuales: {0}",
                        ["Status Player Count"] = "- Jugadores Conectados: {0}",
                        ["Status Dynamic Mode"] = "- Ajuste dinámico: {0}",
                        ["Enabled"] = "Activado",
                        ["Disabled"] = "Desactivado",
                        ["Toggle On"] = "Ajuste dinámico de FPS activado",
                        ["Toggle Off"] = "Ajuste dinámico de FPS desactivado",
                        ["Default FPS Restored"] = "FPS restaurados a {0}",
                        ["Help Header"] = "Ayuda de ServerOptimizer:"
                    };
                    break;
                
                case "ca":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "No tens permís per utilitzar aquesta ordre",
                        ["FPS Adjusted"] = "FPS del servidor ajustats a {0} ({1} jugadors connectats)",
                        ["Status Header"] = "Estat del ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS Actuals: {0}",
                        ["Status Player Count"] = "- Jugadors Connectats: {0}",
                        ["Status Dynamic Mode"] = "- Ajust dinàmic: {0}",
                        ["Enabled"] = "Activat",
                        ["Disabled"] = "Desactivat",
                        ["Toggle On"] = "Ajust dinàmic de FPS activat",
                        ["Toggle Off"] = "Ajust dinàmic de FPS desactivat",
                        ["Default FPS Restored"] = "FPS restaurats a {0}",
                        ["Help Header"] = "Ajuda de ServerOptimizer:"
                    };
                    break;
                
                case "fr":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "Vous n'avez pas la permission d'utiliser cette commande",
                        ["FPS Adjusted"] = "FPS du serveur ajustés à {0} ({1} joueurs en ligne)",
                        ["Status Header"] = "État du ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS actuels: {0}",
                        ["Status Player Count"] = "- Joueurs en ligne: {0}",
                        ["Status Dynamic Mode"] = "- Ajustement dynamique: {0}",
                        ["Enabled"] = "Activé",
                        ["Disabled"] = "Désactivé",
                        ["Toggle On"] = "Ajustement dynamique des FPS activé",
                        ["Toggle Off"] = "Ajustement dynamique des FPS désactivé",
                        ["Default FPS Restored"] = "FPS restaurés à {0}",
                        ["Help Header"] = "Aide de ServerOptimizer:"
                    };
                    break;
                
                case "de":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "Sie haben keine Berechtigung, diesen Befehl zu verwenden",
                        ["FPS Adjusted"] = "Server-FPS auf {0} eingestellt ({1} Spieler online)",
                        ["Status Header"] = "ServerOptimizer-Status:",
                        ["Status Current FPS"] = "- Aktuelle FPS: {0}",
                        ["Status Player Count"] = "- Spieler online: {0}",
                        ["Status Dynamic Mode"] = "- Dynamische Anpassung: {0}",
                        ["Enabled"] = "Aktiviert",
                        ["Disabled"] = "Deaktiviert",
                        ["Toggle On"] = "Dynamische FPS-Anpassung aktiviert",
                        ["Toggle Off"] = "Dynamische FPS-Anpassung deaktiviert",
                        ["Default FPS Restored"] = "FPS auf {0} zurückgesetzt",
                        ["Help Header"] = "ServerOptimizer-Hilfe:"
                    };
                    break;
                
                case "it":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "Non hai il permesso di usare questo comando",
                        ["FPS Adjusted"] = "FPS del server regolati a {0} ({1} giocatori online)",
                        ["Status Header"] = "Stato ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS attuali: {0}",
                        ["Status Player Count"] = "- Giocatori online: {0}",
                        ["Status Dynamic Mode"] = "- Regolazione dinamica: {0}",
                        ["Enabled"] = "Attivato",
                        ["Disabled"] = "Disattivato",
                        ["Toggle On"] = "Regolazione dinamica FPS attivata",
                        ["Toggle Off"] = "Regolazione dinamica FPS disattivata",
                        ["Default FPS Restored"] = "FPS ripristinati a {0}",
                        ["Help Header"] = "Aiuto ServerOptimizer:"
                    };
                    break;
                
                case "ru":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "У вас нет разрешения на использование этой команды",
                        ["FPS Adjusted"] = "FPS сервера установлен на {0} ({1} игроков онлайн)",
                        ["Status Header"] = "Статус ServerOptimizer:",
                        ["Status Current FPS"] = "- Текущий FPS: {0}",
                        ["Status Player Count"] = "- Игроков онлайн: {0}",
                        ["Status Dynamic Mode"] = "- Динамическая настройка: {0}",
                        ["Enabled"] = "Включено",
                        ["Disabled"] = "Выключено",
                        ["Toggle On"] = "Динамическая настройка FPS включена",
                        ["Toggle Off"] = "Динамическая настройка FPS выключена",
                        ["Default FPS Restored"] = "FPS восстановлен до {0}",
                        ["Help Header"] = "Справка ServerOptimizer:"
                    };
                    break;
                    
                case "pt":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "Você não tem permissão para usar este comando",
                        ["FPS Adjusted"] = "FPS do servidor ajustado para {0} ({1} jogadores online)",
                        ["Status Header"] = "Status do ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS atual: {0}",
                        ["Status Player Count"] = "- Jogadores online: {0}",
                        ["Status Dynamic Mode"] = "- Ajuste dinâmico: {0}",
                        ["Enabled"] = "Ativado",
                        ["Disabled"] = "Desativado",
                        ["Toggle On"] = "Ajuste dinâmico de FPS ativado",
                        ["Toggle Off"] = "Ajuste dinâmico de FPS desativado",
                        ["Default FPS Restored"] = "FPS restaurado para {0}",
                        ["Help Header"] = "Ajuda do ServerOptimizer:"
                    };
                    break;

                case "pt-BR":
                    messages = new Dictionary<string, string>
                    {
                        ["No Permission"] = "Você não tem permissão para usar este comando",
                        ["FPS Adjusted"] = "FPS do servidor ajustado para {0} ({1} jogadores online)",
                        ["Status Header"] = "Status do ServerOptimizer:",
                        ["Status Current FPS"] = "- FPS atual: {0}",
                        ["Status Player Count"] = "- Jogadores online: {0}",
                        ["Status Dynamic Mode"] = "- Ajuste dinâmico: {0}",
                        ["Enabled"] = "Ativado",
                        ["Disabled"] = "Desativado",
                        ["Toggle On"] = "Ajuste dinâmico de FPS ativado",
                        ["Toggle Off"] = "Ajuste dinâmico de FPS desativado",
                        ["Default FPS Restored"] = "FPS restaurado para {0}",
                        ["Help Header"] = "Ajuda do ServerOptimizer:"
                    };
                    break;
                    
                default:
                    LoadMoreLanguages(language, messages);
                    break;
            }
            
            lang.RegisterMessages(messages, this, language);
        }

        private void LoadMoreLanguages(string language, Dictionary<string, string> messages)
        {
            switch (language)
            {
                case "af":
                    messages["No Permission"] = "Jy het nie toestemming om hierdie opdrag te gebruik nie";
                    messages["FPS Adjusted"] = "Bediener FPS aangepas na {0} ({1} spelers aanlyn)";
                    messages["Status Header"] = "ServerOptimizer Status:";
                    messages["Status Current FPS"] = "- Huidige FPS: {0}";
                    messages["Status Player Count"] = "- Spelers aanlyn: {0}";
                    messages["Status Dynamic Mode"] = "- Dinamiese aanpassing: {0}";
                    messages["Enabled"] = "Geaktiveer";
                    messages["Disabled"] = "Gedeaktiveer";
                    messages["Toggle On"] = "Dinamiese FPS-aanpassing geaktiveer";
                    messages["Toggle Off"] = "Dinamiese FPS-aanpassing gedeaktiveer";
                    messages["Default FPS Restored"] = "FPS herstel na {0}";
                    messages["Help Header"] = "ServerOptimizer Hulp:";
                    break;
                    
                case "ar":
                    messages["No Permission"] = "ليس لديك إذن لاستخدام هذا الأمر";
                    messages["FPS Adjusted"] = "تم ضبط FPS الخادم إلى {0} ({1} لاعب متصل)";
                    messages["Status Header"] = "حالة ServerOptimizer:";
                    messages["Status Current FPS"] = "- FPS الحالي: {0}";
                    messages["Status Player Count"] = "- اللاعبين المتصلين: {0}";
                    messages["Status Dynamic Mode"] = "- الضبط الديناميكي: {0}";
                    messages["Enabled"] = "مفعل";
                    messages["Disabled"] = "معطل";
                    messages["Toggle On"] = "تم تفعيل ضبط FPS الديناميكي";
                    messages["Toggle Off"] = "تم تعطيل ضبط FPS الديناميكي";
                    messages["Default FPS Restored"] = "تمت استعادة FPS إلى {0}";
                    messages["Help Header"] = "مساعدة ServerOptimizer:";
                    break;
                    
                case "zh-CN":
                    messages["No Permission"] = "您没有权限使用此命令";
                    messages["FPS Adjusted"] = "服务器FPS已调整为 {0} ({1} 玩家在线)";
                    messages["Status Header"] = "ServerOptimizer 状态:";
                    messages["Status Current FPS"] = "- 当前 FPS: {0}";
                    messages["Status Player Count"] = "- 在线玩家: {0}";
                    messages["Status Dynamic Mode"] = "- 动态调整: {0}";
                    messages["Enabled"] = "已启用";
                    messages["Disabled"] = "已禁用";
                    messages["Toggle On"] = "已启用动态FPS调整";
                    messages["Toggle Off"] = "已禁用动态FPS调整";
                    messages["Default FPS Restored"] = "FPS已恢复至{0}";
                    messages["Help Header"] = "ServerOptimizer 帮助:";
                    break;
                    
                case "zh-TW":
                    messages["No Permission"] = "您沒有權限使用此命令";
                    messages["FPS Adjusted"] = "伺服器FPS已調整為 {0} ({1} 玩家線上)";
                    messages["Status Header"] = "ServerOptimizer 狀態:";
                    messages["Status Current FPS"] = "- 目前 FPS: {0}";
                    messages["Status Player Count"] = "- 線上玩家: {0}";
                    messages["Status Dynamic Mode"] = "- 動態調整: {0}";
                    messages["Enabled"] = "已啟用";
                    messages["Disabled"] = "已禁用";
                    messages["Toggle On"] = "已啟用動態FPS調整";
                    messages["Toggle Off"] = "已禁用動態FPS調整";
                    messages["Default FPS Restored"] = "FPS已恢復至{0}";
                    messages["Help Header"] = "ServerOptimizer 幫助:";
                    break;
                    
                case "cs":
                    messages["No Permission"] = "Nemáte oprávnění k použití tohoto příkazu";
                    messages["FPS Adjusted"] = "FPS serveru nastaveno na {0} ({1} hráčů online)";
                    messages["Status Header"] = "Stav ServerOptimizer:";
                    messages["Status Current FPS"] = "- Aktuální FPS: {0}";
                    messages["Status Player Count"] = "- Hráčů online: {0}";
                    messages["Status Dynamic Mode"] = "- Dynamické nastavení: {0}";
                    messages["Enabled"] = "Povoleno";
                    messages["Disabled"] = "Zakázáno";
                    messages["Toggle On"] = "Dynamické nastavení FPS povoleno";
                    messages["Toggle Off"] = "Dynamické nastavení FPS zakázáno";
                    messages["Default FPS Restored"] = "FPS obnoveno na {0}";
                    messages["Help Header"] = "Nápověda ServerOptimizer:";
                    break;
                    
                case "da":
                    messages["No Permission"] = "Du har ikke tilladelse til at bruge denne kommando";
                    messages["FPS Adjusted"] = "Server FPS justeret til {0} ({1} spillere online)";
                    messages["Status Header"] = "ServerOptimizer Status:";
                    messages["Status Current FPS"] = "- Nuværende FPS: {0}";
                    messages["Status Player Count"] = "- Spillere online: {0}";
                    messages["Status Dynamic Mode"] = "- Dynamisk justering: {0}";
                    messages["Enabled"] = "Aktiveret";
                    messages["Disabled"] = "Deaktiveret";
                    messages["Toggle On"] = "Dynamisk FPS-justering aktiveret";
                    messages["Toggle Off"] = "Dynamisk FPS-justering deaktiveret";
                    messages["Default FPS Restored"] = "FPS gendannet til {0}";
                    messages["Help Header"] = "ServerOptimizer Hjælp:";
                    break;
                    
                case "nl":
                    messages["No Permission"] = "Je hebt geen toestemming om dit commando te gebruiken";
                    messages["FPS Adjusted"] = "Server FPS aangepast naar {0} ({1} spelers online)";
                    messages["Status Header"] = "ServerOptimizer Status:";
                    messages["Status Current FPS"] = "- Huidige FPS: {0}";
                    messages["Status Player Count"] = "- Spelers online: {0}";
                    messages["Status Dynamic Mode"] = "- Dynamische aanpassing: {0}";
                    messages["Enabled"] = "Ingeschakeld";
                    messages["Disabled"] = "Uitgeschakeld";
                    messages["Toggle On"] = "Dynamische FPS-aanpassing ingeschakeld";
                    messages["Toggle Off"] = "Dynamische FPS-aanpassing uitgeschakeld";
                    messages["Default FPS Restored"] = "FPS hersteld naar {0}";
                    messages["Help Header"] = "ServerOptimizer Hulp:";
                    break;

                case "fi":
                    messages["No Permission"] = "Sinulla ei ole lupaa käyttää tätä komentoa";
                    messages["FPS Adjusted"] = "Palvelimen FPS säädetty arvoon {0} ({1} pelaajaa online)";
                    messages["Status Header"] = "ServerOptimizer Tila:";
                    messages["Status Current FPS"] = "- Nykyinen FPS: {0}";
                    messages["Status Player Count"] = "- Pelaajia online: {0}";
                    messages["Status Dynamic Mode"] = "- Dynaaminen säätö: {0}";
                    messages["Enabled"] = "Käytössä";
                    messages["Disabled"] = "Pois käytöstä";
                    messages["Toggle On"] = "Dynaaminen FPS-säätö otettu käyttöön";
                    messages["Toggle Off"] = "Dynaaminen FPS-säätö poistettu käytöstä";
                    messages["Default FPS Restored"] = "FPS palautettu arvoon {0}";
                    messages["Help Header"] = "ServerOptimizer Ohje:";
                    break;
                    
                case "el":
                    messages["No Permission"] = "Δεν έχετε άδεια να χρησιμοποιήσετε αυτήν την εντολή";
                    messages["FPS Adjusted"] = "Τα FPS του διακομιστή προσαρμόστηκαν σε {0} ({1} παίκτες σε σύνδεση)";
                    messages["Status Header"] = "Κατάσταση ServerOptimizer:";
                    messages["Status Current FPS"] = "- Τρέχοντα FPS: {0}";
                    messages["Status Player Count"] = "- Παίκτες σε σύνδεση: {0}";
                    messages["Status Dynamic Mode"] = "- Δυναμική προσαρμογή: {0}";
                    messages["Enabled"] = "Ενεργοποιημένο";
                    messages["Disabled"] = "Απενεργοποιημένο";
                    messages["Toggle On"] = "Η δυναμική προσαρμογή FPS ενεργοποιήθηκε";
                    messages["Toggle Off"] = "Η δυναμική προσαρμογή FPS απενεργοποιήθηκε";
                    messages["Default FPS Restored"] = "Τα FPS επαναφέρθηκαν στο {0}";
                    messages["Help Header"] = "Βοήθεια ServerOptimizer:";
                    break;
                    
                case "he":
                    messages["No Permission"] = "אין לך הרשאה להשתמש בפקודה זו";
                    messages["FPS Adjusted"] = "ה-FPS של השרת הותאם ל- {0} ({1} שחקנים מחוברים)";
                    messages["Status Header"] = "מצב ServerOptimizer:";
                    messages["Status Current FPS"] = "- FPS נוכחי: {0}";
                    messages["Status Player Count"] = "- שחקנים מחוברים: {0}";
                    messages["Status Dynamic Mode"] = "- התאמה דינמית: {0}";
                    messages["Enabled"] = "מופעל";
                    messages["Disabled"] = "מושבת";
                    messages["Toggle On"] = "התאמת FPS דינמית הופעלה";
                    messages["Toggle Off"] = "התאמת FPS דינמית הושבתה";
                    messages["Default FPS Restored"] = "ה-FPS שוחזר ל-{0}";
                    messages["Help Header"] = "עזרה ServerOptimizer:";
                    break;
                    
                case "hu":
                    messages["No Permission"] = "Nincs engedélye ennek a parancsnak a használatára";
                    messages["FPS Adjusted"] = "Szerver FPS beállítva {0} értékre ({1} játékos online)";
                    messages["Status Header"] = "ServerOptimizer Állapot:";
                    messages["Status Current FPS"] = "- Jelenlegi FPS: {0}";
                    messages["Status Player Count"] = "- Játékosok online: {0}";
                    messages["Status Dynamic Mode"] = "- Dinamikus beállítás: {0}";
                    messages["Enabled"] = "Engedélyezve";
                    messages["Disabled"] = "Letiltva";
                    messages["Toggle On"] = "Dinamikus FPS beállítás engedélyezve";
                    messages["Toggle Off"] = "Dinamikus FPS beállítás letiltva";
                    messages["Default FPS Restored"] = "FPS visszaállítva {0}-ra";
                    messages["Help Header"] = "ServerOptimizer Súgó:";
                    break;

                case "ja":
                    messages["No Permission"] = "このコマンドを使用する権限がありません";
                    messages["FPS Adjusted"] = "サーバーFPSを {0} に調整しました ({1} 人のプレイヤーがオンライン)";
                    messages["Status Header"] = "ServerOptimizer 状態:";
                    messages["Status Current FPS"] = "- 現在のFPS: {0}";
                    messages["Status Player Count"] = "- オンラインプレイヤー: {0}";
                    messages["Status Dynamic Mode"] = "- 動的調整: {0}";
                    messages["Enabled"] = "有効";
                    messages["Disabled"] = "無効";
                    messages["Toggle On"] = "動的FPS調整が有効になりました";
                    messages["Toggle Off"] = "動的FPS調整が無効になりました";
                    messages["Default FPS Restored"] = "FPSが{0}に復元されました";
                    messages["Help Header"] = "ServerOptimizer ヘルプ:";
                    break;
                    
                case "ko":
                    messages["No Permission"] = "이 명령을 사용할 권한이 없습니다";
                    messages["FPS Adjusted"] = "서버 FPS가 {0}로 조정되었습니다 (온라인 플레이어 {1}명)";
                    messages["Status Header"] = "ServerOptimizer 상태:";
                    messages["Status Current FPS"] = "- 현재 FPS: {0}";
                    messages["Status Player Count"] = "- 온라인 플레이어: {0}";
                    messages["Status Dynamic Mode"] = "- 동적 조정: {0}";
                    messages["Enabled"] = "활성화됨";
                    messages["Disabled"] = "비활성화됨";
                    messages["Toggle On"] = "동적 FPS 조정이 활성화되었습니다";
                    messages["Toggle Off"] = "동적 FPS 조정이 비활성화되었습니다";
                    messages["Default FPS Restored"] = "FPS가 {0}으로 복원되었습니다";
                    messages["Help Header"] = "ServerOptimizer 도움말:";
                    break;

                case "no":
                    messages["No Permission"] = "Du har ikke tillatelse til å bruke denne kommandoen";
                    messages["FPS Adjusted"] = "Server FPS justert til {0} ({1} spillere pålogget)";
                    messages["Status Header"] = "ServerOptimizer Status:";
                    messages["Status Current FPS"] = "- Nåværende FPS: {0}";
                    messages["Status Player Count"] = "- Spillere pålogget: {0}";
                    messages["Status Dynamic Mode"] = "- Dynamisk justering: {0}";
                    messages["Enabled"] = "Aktivert";
                    messages["Disabled"] = "Deaktivert";
                    messages["Toggle On"] = "Dynamisk FPS-justering aktivert";
                    messages["Toggle Off"] = "Dynamisk FPS-justering deaktivert";
                    messages["Default FPS Restored"] = "FPS gjenopprettet til {0}";
                    messages["Help Header"] = "ServerOptimizer Hjelp:";
                    break;

                case "pl":
                    messages["No Permission"] = "Nie masz uprawnień do użycia tej komendy";
                    messages["FPS Adjusted"] = "FPS serwera dostosowano do {0} ({1} graczy online)";
                    messages["Status Header"] = "Status ServerOptimizer:";
                    messages["Status Current FPS"] = "- Obecny FPS: {0}";
                    messages["Status Player Count"] = "- Gracze online: {0}";
                    messages["Status Dynamic Mode"] = "- Dynamiczna regulacja: {0}";
                    messages["Enabled"] = "Włączone";
                    messages["Disabled"] = "Wyłączone";
                    messages["Toggle On"] = "Dynamiczna regulacja FPS włączona";
                    messages["Toggle Off"] = "Dynamiczna regulacja FPS wyłączona";
                    messages["Default FPS Restored"] = "FPS przywrócony do {0}";
                    messages["Help Header"] = "Pomoc ServerOptimizer:";
                    break;
                    
                case "ro":
                    messages["No Permission"] = "Nu ai permisiunea de a folosi această comandă";
                    messages["FPS Adjusted"] = "FPS-ul serverului ajustat la {0} ({1} jucători online)";
                    messages["Status Header"] = "Status ServerOptimizer:";
                    messages["Status Current FPS"] = "- FPS Actual: {0}";
                    messages["Status Player Count"] = "- Jucători online: {0}";
                    messages["Status Dynamic Mode"] = "- Ajustare dinamică: {0}";
                    messages["Enabled"] = "Activat";
                    messages["Disabled"] = "Dezactivat";
                    messages["Toggle On"] = "Ajustare dinamică FPS activată";
                    messages["Toggle Off"] = "Ajustare dinamică FPS dezactivată";
                    messages["Default FPS Restored"] = "FPS restaurat la {0}";
                    messages["Help Header"] = "Ajutor ServerOptimizer:";
                    break;

                case "sr-Cyrl":
                    messages["No Permission"] = "Немате дозволу да користите ову команду";
                    messages["FPS Adjusted"] = "ФПС сервера подешен на {0} ({1} играча на мрежи)";
                    messages["Status Header"] = "Статус ServerOptimizer:";
                    messages["Status Current FPS"] = "- Тренутни FPS: {0}";
                    messages["Status Player Count"] = "- Играчи на мрежи: {0}";
                    messages["Status Dynamic Mode"] = "- Динамичко подешавање: {0}";
                    messages["Enabled"] = "Омогућено";
                    messages["Disabled"] = "Онемогућено";
                    messages["Toggle On"] = "Динамичко подешавање FPS-а омогућено";
                    messages["Toggle Off"] = "Динамичко подешавање FPS-а онемогућено";
                    messages["Default FPS Restored"] = "FPS враћен на {0}";
                    messages["Help Header"] = "Помоћ за ServerOptimizer:";
                    break;
                    
                case "sv":
                    messages["No Permission"] = "Du har inte behörighet att använda detta kommando";
                    messages["FPS Adjusted"] = "Server FPS justerat till {0} ({1} spelare online)";
                    messages["Status Header"] = "ServerOptimizer Status:";
                    messages["Status Current FPS"] = "- Nuvarande FPS: {0}";
                    messages["Status Player Count"] = "- Spelare online: {0}";
                    messages["Status Dynamic Mode"] = "- Dynamisk justering: {0}";
                    messages["Enabled"] = "Aktiverad";
                    messages["Disabled"] = "Inaktiverad";
                    messages["Toggle On"] = "Dynamisk FPS-justering aktiverad";
                    messages["Toggle Off"] = "Dynamisk FPS-justering inaktiverad";
                    messages["Default FPS Restored"] = "FPS återställt till {0}";
                    messages["Help Header"] = "ServerOptimizer Hjälp:";
                    break;

                case "tr":
                    messages["No Permission"] = "Bu komutu kullanma izniniz yok";
                    messages["FPS Adjusted"] = "Sunucu FPS {0} olarak ayarlandı ({1} oyuncu çevrimiçi)";
                    messages["Status Header"] = "ServerOptimizer Durumu:";
                    messages["Status Current FPS"] = "- Mevcut FPS: {0}";
                    messages["Status Player Count"] = "- Çevrimiçi oyuncular: {0}";
                    messages["Status Dynamic Mode"] = "- Dinamik ayarlama: {0}";
                    messages["Enabled"] = "Etkin";
                    messages["Disabled"] = "Devre Dışı";
                    messages["Toggle On"] = "Dinamik FPS ayarlaması etkinleştirildi";
                    messages["Toggle Off"] = "Dinamik FPS ayarlaması devre dışı bırakıldı";
                    messages["Default FPS Restored"] = "FPS {0}'a geri yüklendi";
                    messages["Help Header"] = "ServerOptimizer Yardım:";
                    break;

                case "uk":
                    messages["No Permission"] = "У вас немає дозволу на використання цієї команди";
                    messages["FPS Adjusted"] = "FPS сервера налаштовано на {0} ({1} гравців онлайн)";
                    messages["Status Header"] = "Статус ServerOptimizer:";
                    messages["Status Current FPS"] = "- Поточний FPS: {0}";
                    messages["Status Player Count"] = "- Гравців онлайн: {0}";
                    messages["Status Dynamic Mode"] = "- Динамічне налаштування: {0}";
                    messages["Enabled"] = "Увімкнено";
                    messages["Disabled"] = "Вимкнено";
                    messages["Toggle On"] = "Динамічне налаштування FPS увімкнено";
                    messages["Toggle Off"] = "Динамічне налаштування FPS вимкнено";
                    messages["Default FPS Restored"] = "FPS відновлено до {0}";
                    messages["Help Header"] = "Довідка ServerOptimizer:";
                    break;

                case "vi":
                    messages["No Permission"] = "Bạn không có quyền sử dụng lệnh này";
                    messages["FPS Adjusted"] = "FPS máy chủ được điều chỉnh thành {0} ({1} người chơi trực tuyến)";
                    messages["Status Header"] = "Trạng thái ServerOptimizer:";
                    messages["Status Current FPS"] = "- FPS hiện tại: {0}";
                    messages["Status Player Count"] = "- Người chơi trực tuyến: {0}";
                    messages["Status Dynamic Mode"] = "- Điều chỉnh động: {0}";
                    messages["Enabled"] = "Đã bật";
                    messages["Disabled"] = "Đã tắt";
                    messages["Toggle On"] = "Điều chỉnh FPS động đã được bật";
                    messages["Toggle Off"] = "Điều chỉnh FPS động đã được tắt";
                    messages["Default FPS Restored"] = "FPS được khôi phục về {0}";
                    messages["Help Header"] = "Trợ giúp ServerOptimizer:";
                    break;
            }
        }

        private string GetLang(string key, string userId = null)
        {
            string playerLanguage = _config.DefaultLanguage;
            string message = lang.GetMessage(key, this, playerLanguage);
            
            if (string.IsNullOrEmpty(message) || message == key)
            {
                message = lang.GetMessage(key, this, "en");
            }
            
            return message;
        }

        protected override void LoadDefaultMessages()
        {
            LoadLanguages();
        }

        #endregion

        #region License
        /*
         * MIT License
         * 
         * Copyright (c) 2025 Jaie55
         * 
         * Permission is hereby granted, free of charge, to any person obtaining a copy
         * of this software and associated documentation files (the "Software"), to deal
         * in the Software without restriction, including without limitation the rights
         * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
         * copies of the Software, and to permit persons to whom the Software is
         * furnished to do so, subject to the following conditions:
         * 
         * The above copyright notice and this permission notice shall be included in all
         * copies or substantial portions of the Software.
         * 
         * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
         * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
         * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
         * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
         * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
         * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
         * SOFTWARE.
         */
        #endregion
    }
}
