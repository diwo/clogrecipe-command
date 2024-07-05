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
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

        public string Name => "Crafting Log Recipe Command";
        private const string CommandName = "/clogrecipe";
        private Dictionary<string, uint> itemNameToRecipeId = new();

        public Plugin()
        {
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

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand));
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(CommandName);
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
