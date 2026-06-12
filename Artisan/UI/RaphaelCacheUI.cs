using Artisan.GameInterop;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using System.Linq;
using System.Numerics;
using Artisan.UI.Tables;
using ECommons;
using Artisan.CraftingLogic.Solvers;
using Dalamud.Interface.Utility.Raii;
using System;

namespace Artisan.UI
{
    internal class RaphaelCacheUI
    {
        public RaphaelCacheTable? Table;

        internal void Draw()
        {
            try
            {
                ImGui.TextWrapped("此标签页显示所有当前已保存的Raphael生成的宏。");

                if (Svc.ClientState.IsLoggedIn && Crafting.CurState is not Crafting.State.IdleNormal and not Crafting.State.IdleBetween)
                {
                    ImGui.Text($"正在制作中，停止制作前宏设置将不可用。");
                    return;
                }
                ImGui.Spacing();

                ImGui.TextWrapped($"已保存宏数：{P.Config.RaphaelSolverCacheV6.Keys.Count}");
                ImGui.Spacing();

                using (ImRaii.Child("##selector", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 32f.Scale()), true))
                {
                    // todo: search by recipe?
                    if (Table == null)
                    {
                        var cacheList = P.Config.RaphaelSolverCacheV6.Keys.ToList();
                        Table = new(cacheList);
                    }
                    Table.Draw(ImGui.GetTextLineHeightWithSpacing() - 4f);
                }

                var filterActive = Table.FilteredItems.Count != 0 && Table.FilteredItems.Count != P.Config.RaphaelSolverCacheV6.Keys.Count;
                var filterCount = filterActive ? $"{Table.FilteredItems.Count} " : "";

                if (!filterActive) ImGui.BeginDisabled();
                if (ImGuiEx.ButtonCtrl($"删除 {filterCount}个筛选出的宏", new Vector2(ImGui.GetContentRegionAvail().X / 2, ImGui.GetContentRegionAvail().Y)))
                {
                    var toDelete = Table.FilteredItems.JSONClone();
                    foreach ((RaphaelOptions key, int _) in toDelete)
                    {
                        P.Config.RaphaelSolverCacheV6.TryRemove(key, out _);
                    }
                    Table.FilteredItems.Clear();
                    P.Config.Save();
                }
                if (!filterActive) ImGui.EndDisabled();

                ImGui.SameLine();

                if (ImGuiEx.ButtonCtrl($"删除全部缓存", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y)))
                {
                    P.Config.RaphaelSolverCacheV6.Clear();
                    P.Config.Save();
                }
            }
            catch (Exception ex) { ex.Log(); }
        }
    }
}
