using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace CLogRecipeCommand
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Crafting Log Recipe Command";
        private const string CommandName = "/clogrecipe";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private IDataManager DataManager { get; set; }
        private IChatGui ChatGui { get; set; }
        private IPluginLog PluginLog { get; init; }

        private Dictionary<string, uint> itemNameToRecipeId = new();

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IDataManager dataManager,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] IPluginLog pluginLog)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.DataManager = dataManager;
            this.ChatGui = chatGui;
            this.PluginLog = pluginLog;

            var recipes = DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Recipe>()?.ToList() ?? [];
            foreach (var recipe in recipes) {
                var recipeId = recipe.RowId;
                var itemName = recipe.ItemResult.Value?.Name ?? "";
                if (itemName != "") {
                    var normalizedName = normalizeItemName(itemName);
                    PluginLog.Debug($"Adding itemname={itemName} normalizedName={normalizedName} recipeId={recipeId}");
                    itemNameToRecipeId.TryAdd(normalizedName, recipeId);
                }
            }

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand));
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(CommandName);
        }

        private unsafe void OnCommand(string command, string args)
        {
            var normalizedName = normalizeItemName(args);

            if (args == "") {
                ChatGui.Print($"Usage: {CommandName} <itemname>");
                return;
            }

            if (itemNameToRecipeId.TryGetValue(normalizedName, out var recipeId)) {
                AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId);
            } else {
                ChatGui.Print($"Cannot find recipe for item '{args}'");
            }
        }

        private string normalizeItemName(string name) {
            return Regex.Replace(name.ToLower(), "[^a-z0-9 ]", "");
        }
    }
}
