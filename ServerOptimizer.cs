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
    [Info("ServerOptimizer", "Jaie55", "1.0.0")]
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
                ConsoleSystem.Run(ConsoleSystem.Option.Server, "fps.limit", 60);
                _currentFps = 60;
                SendReply(player, $"{_config.ChatPrefix}{GetLang("Toggle Off", player.UserIDString)}");
                SendReply(player, $"{_config.ChatPrefix}{GetLang("Default FPS Restored", player.UserIDString)}");
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
                        ["Default FPS Restored"] = "FPS restored to 60",
                        ["Help Header"] = "ServerOptimizer Help:"
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
                        ["Default FPS Restored"] = "FPS restaurado a 60",
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
                        ["Default FPS Restored"] = "FPS restaurados a 60",
                        ["Help Header"] = "Ayuda de ServerOptimizer:"
                    };
                    break;
                
                default:
                    LoadDefaultLanguageMessages(language, messages);
                    break;
            }
            
            lang.RegisterMessages(messages, this, language);
        }

        private void LoadDefaultLanguageMessages(string language, Dictionary<string, string> messages)
        {
            if (language == "fr")
            {
                messages["No Permission"] = "Vous n'avez pas la permission d'utiliser cette commande";
                messages["FPS Adjusted"] = "FPS du serveur ajustés à {0} ({1} joueurs en ligne)";
                messages["Status Header"] = "État du ServerOptimizer:";
                messages["Enabled"] = "Activé";
                messages["Disabled"] = "Désactivé";
            }
            else if (language == "de")
            {
                messages["No Permission"] = "Sie haben keine Berechtigung, diesen Befehl zu verwenden";
                messages["FPS Adjusted"] = "Server-FPS auf {0} eingestellt ({1} Spieler online)";
                messages["Status Header"] = "ServerOptimizer-Status:";
                messages["Enabled"] = "Aktiviert";
                messages["Disabled"] = "Deaktiviert";
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
