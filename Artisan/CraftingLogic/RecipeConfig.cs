using Artisan.Autocraft;
using Artisan.CraftingLogic.Solvers;
using Artisan.GameInterop;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Artisan.UI;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Numerics;
using static Artisan.CraftingLogic.Solvers.ExpertSolverSettings;

namespace Artisan.CraftingLogic;

[Serializable]
public class RecipeConfig
{
    public const uint Default = 0;
    public const uint Disabled = 1;

    [NonSerialized]
    public string TempSolverType = "";
    [NonSerialized]
    public int TempSolverFlavour = -1;

    public string CurrentSolverType => TempSolverType != "" ? TempSolverType : SolverType;
    public int CurrentSolverFlavour => TempSolverFlavour != -1 ? TempSolverFlavour : SolverFlavour;

    [NonSerialized]
    public uint TempRequiredFood = 0;
    [NonSerialized]
    public bool TempFoodHQ = true;
    [NonSerialized]
    public uint TempRequiredPotion = 0;
    [NonSerialized]
    public bool TempPotionHQ = true;
    [NonSerialized]
    public uint TempRequiredManual = 0;
    [NonSerialized]
    public uint TempRequiredSquadronManual = 0;

    [NonSerialized]
    public int? TempExpertProfileID = null;
    [NonSerialized]
    public uint? TempExpertMaxSteadyUses = null;
    [NonSerialized]
    public bool? TempExpertUseMaterialMiracle = null; // deprecated
    [NonSerialized]
    public uint? TempExpertMaxMaterialMiracleUses = null;
    [NonSerialized]
    public uint? TempExpertMinimumStepsBeforeMiracle = null;
    [NonSerialized]
    public uint? TempRaphaelMaxStellarHand = null;

    public string SolverType = ""; // TODO: ideally it should be a Type?, but that causes problems for serialization
    public int SolverFlavour;
    public int expertProfileID = (int)Default;

    public uint expertMaxSteadyUses = Default;
    public uint expertMaxMaterialMiracleUses = Default;
    public uint expertMinimumStepsBeforeMiracle = Default;
    public MMSet expertUseMMWhen = MMSet.Steps;
    public uint? raphaelMaxStellarHand = null;

    public uint requiredFood = Default;
    public uint requiredPotion = Default;
    public uint requiredManual = Default;
    public uint requiredSquadronManual = Default;
    public bool requiredFoodHQ = true;
    public bool requiredPotionHQ = true;


    public bool FoodEnabled => RequiredFood != Disabled;
    public bool PotionEnabled => RequiredPotion != Disabled;
    public bool ManualEnabled => RequiredManual != Disabled;
    public bool SquadronManualEnabled => RequiredSquadronManual != Disabled;


    public uint RequiredFood => TempRequiredFood != 0 ? TempRequiredFood : (requiredFood == Default ? P.Config.DefaultConsumables.requiredFood : requiredFood);
    public uint RequiredPotion => TempRequiredPotion != 0 ? TempRequiredPotion : (requiredPotion == Default ? P.Config.DefaultConsumables.requiredPotion : requiredPotion);
    public uint RequiredManual => TempRequiredManual != 0 ? TempRequiredManual : (requiredManual == Default ? P.Config.DefaultConsumables.requiredManual : requiredManual);
    public uint RequiredSquadronManual => TempRequiredSquadronManual != 0 ? TempRequiredSquadronManual : (requiredSquadronManual == Default ? P.Config.DefaultConsumables.requiredSquadronManual : requiredSquadronManual);
    public bool RequiredFoodHQ => TempRequiredFood != 0 ? TempFoodHQ : (requiredFood == Default ? P.Config.DefaultConsumables.requiredFoodHQ : requiredFoodHQ);
    public bool RequiredPotionHQ => TempRequiredPotion != 0 ? TempPotionHQ : (requiredPotion == Default ? P.Config.DefaultConsumables.requiredPotionHQ : requiredPotionHQ);


    public string FoodName => requiredFood == Default && TempRequiredFood == 0 ? $"{P.Config.DefaultConsumables.FoodName}（默认）" : RequiredFood == Disabled ? "已禁用" : $"{(RequiredFoodHQ ? " " : "")}{ConsumableChecker.Food.FirstOrDefault(x => x.Id == RequiredFood).Name}（数量：{ConsumableChecker.NumberOfConsumable(RequiredFood, RequiredFoodHQ)})";
    public string PotionName => requiredPotion == Default && TempRequiredPotion == 0 ? $"{P.Config.DefaultConsumables.PotionName}（默认）" : RequiredPotion == Disabled ? "已禁用" : $"{(RequiredPotionHQ ? " " : "")}{ConsumableChecker.Pots.FirstOrDefault(x => x.Id == RequiredPotion).Name}（数量：{ConsumableChecker.NumberOfConsumable(RequiredPotion, RequiredPotionHQ)})";
    public string ManualName => requiredManual == Default && TempRequiredManual == 0 ? $"{P.Config.DefaultConsumables.ManualName}（默认）" : RequiredManual == Disabled ? "已禁用" : $"{ConsumableChecker.Manuals.FirstOrDefault(x => x.Id == RequiredManual).Name}（数量：{ConsumableChecker.NumberOfConsumable(RequiredManual, false)})";
    public string SquadronManualName => requiredSquadronManual == Default && TempRequiredSquadronManual == 0 ? $"{P.Config.DefaultConsumables.SquadronManualName}（默认）" : RequiredSquadronManual == Disabled ? "已禁用" : $"{ConsumableChecker.SquadronManuals.FirstOrDefault(x => x.Id == RequiredSquadronManual).Name}（数量：{ConsumableChecker.NumberOfConsumable(RequiredSquadronManual, false)})";

    public int ExpertProfileID => TempExpertProfileID ?? expertProfileID;
    public uint ExpertMaxSteadyUses => TempExpertMaxSteadyUses ?? expertMaxSteadyUses;
    public bool ExpertUseMaterialMiracle => TempExpertUseMaterialMiracle ?? (expertMaxMaterialMiracleUses > 0); // deprecated
    public uint ExpertMaxMaterialMiracleUses => TempExpertUseMaterialMiracle != null ? TempExpertUseMaterialMiracle == true ? (uint)1 : (uint)0 : TempExpertMaxMaterialMiracleUses ?? expertMaxMaterialMiracleUses;
    public uint ExpertMinimumStepsBeforeMiracle => TempExpertMinimumStepsBeforeMiracle ?? expertMinimumStepsBeforeMiracle;
    [Newtonsoft.Json.JsonIgnore]
    public uint? RaphaelMaxStellarHand => TempRaphaelMaxStellarHand ?? raphaelMaxStellarHand;

    public float GetLargestName()
    {
        try
        {
            return 32f + 350f; //Bandaid fix for the time being as below might crash
        }
        catch (Exception ex)
        {
            ex.Log();
            return 0;
        }
    }

    public bool SolverIsRaph => CurrentSolverType == typeof(RaphaelSolverDefintion).FullName!;
    public bool SolverIsStandard => CurrentSolverType == typeof(StandardSolverDefinition).FullName!;
    public bool SolverIsExpert => CurrentSolverType == typeof(ExpertSolverDefinition).FullName!;

    public bool Draw(uint recipeId)
    {
        var recipe = LuminaSheets.RecipeSheet[recipeId];
        ImGuiEx.LineCentered($"###RecipeName{recipeId}", () => { ImGuiEx.TextUnderlined($"{recipe.ItemResult.Value.Name.ToDalamudString().ToString()}"); });
        var config = this;
        var stats = CharacterStats.GetBaseStatsForClassHeuristic((Job)((uint)Job.CRP + recipe.CraftType.RowId));
        stats.AddConsumables(new(config.RequiredFood, config.RequiredFoodHQ), new(config.RequiredPotion, config.RequiredPotionHQ), CharacterInfo.FCCraftsmanshipbuff);
        var craft = Crafting.BuildCraftStateForRecipe(stats, (Job)((uint)Job.CRP + recipe.CraftType.RowId), recipe);
        if (craft.InitialQuality == 0)
            craft.InitialQuality = Simulator.GetStartingQuality(recipe, false, craft.StatLevel);
        var liveStats = Player.ClassJob.RowId == craft.Recipe.CraftType.RowId + 8;
        bool changed = false;
        changed |= DrawFood();
        changed |= DrawPotion();
        changed |= DrawManual();
        changed |= DrawSquadronManual();
        changed |= DrawSolver(craft, liveStats: liveStats);
        if (P.Config.ExpertSolverConfig.EnableExpertProfiles)
            changed |= DrawExpertProfiles(craft);
        DrawWarnings(craft);
        RaphaelCache.DrawRaphaelDropdown(craft, liveStats);
        ImGui.Separator();
        DrawSimulator(craft);
        return changed;
    }

    public bool DrawFood(bool hasButton = false)
    {
        bool changed = false;
        ImGuiEx.TextV("食物：");
        ImGui.SameLine(130f.Scale());
        if (hasButton) ImGuiEx.SetNextItemFullWidth(-120);
        else ImGui.PushItemWidth(GetLargestName());
        if (ImGui.BeginCombo("##foodBuff", FoodName))
        {
            if (this != P.Config.DefaultConsumables)
            {
                if (ImGui.Selectable($"{P.Config.DefaultConsumables.FoodName}（默认）"))
                {
                    requiredFood = Default;
                    requiredFoodHQ = false;
                    changed = true;
                }
            }
            if (ImGui.Selectable("禁用"))
            {
                requiredFood = Disabled;
                requiredFoodHQ = false;
                changed = true;
            }
            foreach (var x in ConsumableChecker.GetFood(true))
            {
                if (ImGui.Selectable($"{x.Name}（数量：{ConsumableChecker.NumberOfConsumable(x.Id, false)})"))
                {
                    requiredFood = x.Id;
                    requiredFoodHQ = false;
                    changed = true;
                }
            }
            foreach (var x in ConsumableChecker.GetFood(true, true))
            {
                if (ImGui.Selectable($" {x.Name}（数量：{ConsumableChecker.NumberOfConsumable(x.Id, true)})"))
                {
                    requiredFood = x.Id;
                    requiredFoodHQ = true;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    public bool DrawPotion(bool hasButton = false)
    {
        bool changed = false;
        ImGuiEx.TextV("爆发药：");
        ImGui.SameLine(130f.Scale());
        if (hasButton) ImGuiEx.SetNextItemFullWidth(-120);
        else ImGui.PushItemWidth(GetLargestName());
        if (ImGui.BeginCombo("##potBuff", PotionName))
        {
            if (this != P.Config.DefaultConsumables)
            {
                if (ImGui.Selectable($"{P.Config.DefaultConsumables.PotionName}（默认）"))
                {
                    requiredPotion = Default;
                    requiredPotionHQ = false;
                    changed = true;
                }
            }
            if (ImGui.Selectable("禁用"))
            {
                requiredPotion = Disabled;
                requiredPotionHQ = false;
                changed = true;
            }
            foreach (var x in ConsumableChecker.GetPots(true))
            {
                if (ImGui.Selectable($"{x.Name}（数量：{ConsumableChecker.NumberOfConsumable(x.Id, false)})"))
                {
                    requiredPotion = x.Id;
                    requiredPotionHQ = false;
                    changed = true;
                }
            }
            foreach (var x in ConsumableChecker.GetPots(true, true))
            {
                if (ImGui.Selectable($" {x.Name}（数量：{ConsumableChecker.NumberOfConsumable(x.Id, true)})"))
                {
                    requiredPotion = x.Id;
                    requiredPotionHQ = true;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    public bool DrawManual(bool hasButton = false)
    {
        bool changed = false;
        ImGuiEx.TextV("指南：");
        ImGui.SameLine(130f.Scale());
        if (hasButton) ImGuiEx.SetNextItemFullWidth(-120);
        else ImGui.PushItemWidth(GetLargestName());
        if (ImGui.BeginCombo("##manualBuff", ManualName))
        {
            if (this != P.Config.DefaultConsumables)
            {
                if (ImGui.Selectable($"{P.Config.DefaultConsumables.ManualName}（默认）"))
                {
                    requiredManual = Default;
                    changed = true;
                }
            }
            if (ImGui.Selectable("禁用"))
            {
                requiredManual = Disabled;
                changed = true;
            }
            foreach (var x in ConsumableChecker.GetManuals(true))
            {
                if (ImGui.Selectable($"{x.Name}（数量：{ConsumableChecker.NumberOfConsumable(x.Id, false)})"))
                {
                    requiredManual = x.Id;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    public bool DrawSquadronManual(bool hasButton = false)
    {
        bool changed = false;
        ImGuiEx.TextV("部队指南：");
        ImGui.SameLine(130f.Scale());
        if (hasButton) ImGuiEx.SetNextItemFullWidth(-120);
        else ImGui.PushItemWidth(GetLargestName());
        if (ImGui.BeginCombo("##squadronManualBuff", SquadronManualName))
        {
            if (this != P.Config.DefaultConsumables)
            {
                if (ImGui.Selectable($"{P.Config.DefaultConsumables.SquadronManualName}（默认）"))
                {
                    requiredSquadronManual = Default;
                    changed = true;
                }
            }
            if (ImGui.Selectable("禁用"))
            {
                requiredSquadronManual = Disabled;
                changed = true;
            }
            foreach (var x in ConsumableChecker.GetSquadronManuals(true))
            {
                if (ImGui.Selectable($"{x.Name}（数量：{ConsumableChecker.NumberOfConsumable(x.Id, false)})"))
                {
                    requiredSquadronManual = x.Id;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        return changed;
    }

    public bool DrawSolver(CraftState craft, bool hasButton = false, bool liveStats = true)
    {
        bool changed = false;
        var solver = CraftingProcessor.GetSolverForRecipe(this, craft);
        bool exists = P.Config.RecipeConfigs.ContainsKey(craft.RecipeId);
        if (!exists && P.Config.RaphaelSolverConfig.DefaultRaphSolver)
        {
            if (craft.StatLevel < 7 && P.Config.RaphaelSolverConfig.FallbackToSolverIfRaphaelLocked)
            {
                this.SolverType = P.Config.RaphaelSolverConfig.FallbackSolverType;
                this.SolverFlavour = P.Config.RaphaelSolverConfig.FallbackSolverFlavour;
                changed = true;
            }
            else
            {
                this.SolverFlavour = 3;
                this.SolverType = typeof(RaphaelSolverDefintion).FullName!;
                changed = true;
            }
        }
        else if (exists && craft.StatLevel < 7 && this.SolverType == typeof(RaphaelSolverDefintion).FullName! && P.Config.RaphaelSolverConfig.FallbackToSolverIfRaphaelLocked)
        {
            this.SolverType = P.Config.RaphaelSolverConfig.FallbackSolverType;
            this.SolverFlavour = P.Config.RaphaelSolverConfig.FallbackSolverFlavour;
            changed = true;
        }
        if (string.IsNullOrEmpty(solver.Name))
        {
            ImGuiEx.Text(ImGuiColors.DalamudRed, "无法选择默认求解器，请从下拉列表中选择。");
        }
        ImGuiEx.TextV($"求解器：");
        ImGui.SameLine(130f.Scale());
        if (hasButton) ImGuiEx.SetNextItemFullWidth(-120);

        if (ImGui.BeginCombo("##solver", solver.Name))
        {
            foreach (var opt in CraftingProcessor.GetAvailableSolversForRecipe(craft, true).OrderBy(x => x.Priority))
            {
                if (opt == default) continue;
                if (opt.UnsupportedReason.Length > 0)
                {
                    ImGui.Text($"{opt.Name} 不支持 - {opt.UnsupportedReason}");
                }
                else
                {
                    bool selected = opt.Name == solver.Name && opt.Flavour == solver.Flavour;
                    if (ImGui.Selectable($"{opt.Name}###{opt.Name}{opt.Flavour}", selected))
                    {
                        IPC.IPC.SetTempSolverBackToNormal(craft.RecipeId);
                        SolverType = opt.Def.GetType().FullName!;
                        SolverFlavour = opt.Flavour;
                        changed = true;
                    }
                }
            }

            ImGui.EndCombo();
        }

        return changed;
    }

    public bool DrawExpertProfiles(CraftState craft, bool hasButton = false)
    {
        bool changed = false;
        if (this.CurrentSolverType.Contains("Expert") || this.CurrentSolverType == "" && craft.CraftExpert)
        {
            var expertProfile = CraftingProcessor.GetExpertProfileForRecipe(this);
            if (string.IsNullOrEmpty(expertProfile.Name))
            {
                ImGuiEx.Text(ImGuiColors.DalamudRed, "无法选择专家求解器配置，请从下拉列表中选择。");
            }

            ImGuiEx.TextV($"专家配置：");
            ImGui.SameLine();

            ImGuiEx.IconWithTooltip(new Vector4(0.5f, 0.5f, 0.5f, 1f), FontAwesomeIcon.PencilAlt, "添加或编辑专家求解器配置");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                P.PluginUi.OpenWindow = OpenWindow.ExpertProfiles;
                P.PluginUi.IsOpen = true;
            }
            ImGui.SameLine(130f.Scale());

            if (hasButton) ImGuiEx.SetNextItemFullWidth(-120);
            if (ImGui.BeginCombo("##expertProfile", expertProfile.Name))
            {
                foreach (var c in P.Config.ExpertSolverProfiles.GetExpertProfilesWithDefault())
                {
                    bool selected = c.Name == expertProfile.Name;
                    if (ImGui.Selectable(c.Name, selected))
                    {
                        expertProfileID = c.ID;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
        }
        return changed;
    }

    public void DrawWarnings(CraftState craft)
    {
        if (craft.StatLevel >= 65 && !craft.UnlockedManipulation)
        {
            ImGuiEx.TextCentered(ImGuiColors.DalamudYellow, "You can now go unlock Manipulation for this job.");
            ImGuiEx.TextCentered(ImGuiColors.DalamudYellow, "It is highly recommended to do so as it will make crafting much easier.");
        }
        if (!Crafting.EnoughDelinsForCraft(this, craft, out var req))
        {
            ImGuiEx.TextCentered(ImGuiColors.DalamudRed, $"你没有足够的 {Svc.Data.GetExcelSheet<Item>().GetRow(28724).Name} 供该求解器使用（需要 {req} 个）。");
            if (this.CurrentSolverType.Contains("Raphael"))
            {
                ImGuiEx.TextCentered(ImGuiColors.DalamudYellow, $"开始制作时将使用/生成替代方案。");
            }
        }

        if (ConsumableChecker.SkippingConsumablesByConfig(craft.Recipe))
            ImGuiEx.Text(ImGuiColors.DalamudRed, "由于等级差设置，将不会使用消耗品。");
    }

    public unsafe void DrawSimulator(CraftState craft)
    {
        if (!P.Config.HideRecipeWindowSimulator)
        {
            var recipe = craft.Recipe;
            var config = this;
            var solverHint = Simulator.SimulatorResult(recipe, config, craft, out var hintColor);
            var solver = CraftingProcessor.GetSolverForRecipe(config, craft);

            if (!solver.Def.GetType().IsAssignableTo(typeof(ExpertSolverDefinition)))
            {
                if (craft.MissionHasMaterialMiracle && solver.Def.GetType().IsAssignableTo(typeof(StandardSolverDefinition)) && P.Config.StandardMMUses > 0)
                    ImGuiEx.TextCentered($"[s!MaterialMiracle] 会导致模拟器结果不一致。");
                else
                    if (solver.Def.GetType().IsAssignableTo(typeof(RaphaelSolverDefintion)) && !RaphaelCache.HasSolution(craft, out _))
                        ImGuiEx.TextCentered($"未生成 Raphael 方案前无法生成模拟器。");
                    else
                        ImGuiEx.TextCentered(hintColor, $"[{solver.Name.Split(' ')[0].Trim(":")}] {solverHint}");
            }
            else
                ImGuiEx.TextCentered($"【专家】请在模拟器中运行此配方以查看结果。");

            if (ImGui.IsItemClicked())
            {
                P.PluginUi.OpenWindow = OpenWindow.Simulator;
                P.PluginUi.IsOpen = true;
                SimulatorUI.SelectedRecipe = recipe;
                SimulatorUI.ResetSim();
                if (config.PotionEnabled)
                {
                    SimulatorUI.SimMedicine ??= new();
                    SimulatorUI.SimMedicine.Id = config.RequiredPotion;
                    SimulatorUI.SimMedicine.ConsumableHQ = config.RequiredPotionHQ;
                    SimulatorUI.SimMedicine.Stats = new ConsumableStats(config.RequiredPotion, config.RequiredPotionHQ);
                }
                if (config.FoodEnabled)
                {
                    SimulatorUI.SimFood ??= new();
                    SimulatorUI.SimFood.Id = config.RequiredFood;
                    SimulatorUI.SimFood.ConsumableHQ = config.RequiredFoodHQ;
                    SimulatorUI.SimFood.Stats = new ConsumableStats(config.RequiredFood, config.RequiredFoodHQ);
                }

                foreach (ref var gs in RaptureGearsetModule.Instance()->Entries)
                {
                    if ((Job)gs.ClassJob == (Job)((uint)Job.CRP + recipe.CraftType.RowId))
                    {
                        if (SimulatorUI.SimGS is null || (Job)SimulatorUI.SimGS.Value.ClassJob != (Job)((uint)Job.CRP + recipe.CraftType.RowId))
                        {
                            SimulatorUI.SimGS = gs;
                        }

                        if (SimulatorUI.SimGS.Value.ItemLevel < gs.ItemLevel)
                            SimulatorUI.SimGS = gs;
                    }
                }

                var rawSolver = CraftingProcessor.GetSolverForRecipe(config, craft);
                SimulatorUI._selectedSolver = new(rawSolver.Name, rawSolver.Def.Create(craft, rawSolver.Flavour));
            }

            if (ImGui.IsItemHovered())
            {
                ImGuiEx.Tooltip($"点击在模拟器中打开");
            }


        }
    }
}
