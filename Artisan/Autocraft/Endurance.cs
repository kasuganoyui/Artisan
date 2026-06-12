using Artisan.CraftingLists;
using Artisan.CraftingLogic.Solvers;
using Artisan.GameInterop;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Artisan.Sounds;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.CircularBuffers;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using static ECommons.GenericHelpers;

namespace Artisan.Autocraft
{
    public class EnduranceIngredients
    {
        public int HQSet { get; set; }
        public int IngredientSlot { get; set; }
        public int NQSet { get; set; }
    }

    internal static unsafe class Endurance
    {
        internal static bool IPCOverride = false;
        internal static bool SkipBuffs = false;
        internal static CircularBuffer<long> Errors = new(5);
        static CircularBuffer<long> FailedStarts = new(5);

        internal static List<int>? HQData = null;

        internal static ushort RecipeID
        {
            get;
            set
            {
                if (field != value)
                {
                    P.Config.CraftingX = false;
                    P.Config.CraftX = 0;
                }
                field = value;
            }
        }

        internal static EnduranceIngredients[] SetIngredients = new EnduranceIngredients[6];

        internal static readonly List<uint> UnableToCraftErrors = new List<uint>()
        {
            1134,1135,1136,1137,1138,1139,1140,1141,1142,1143,1144,1145,1146,1148,1149,1198,1199,1222,1223,1224,
        };

        internal static bool Enable
        {
            get => enable;
            set
            {
                enable = value;
            }
        }

        internal static string RecipeName
        {
            get => RecipeID == 0 ? "未选择配方" : LuminaSheets.RecipeSheet[RecipeID].ItemResult.Value.Name.ToDalamudString().ToString().Trim();
        }

        internal static void ToggleEndurance(bool enable)
        {
            if (RecipeID > 0 && enable)
            {
                Enable = enable;
            }
            else if (Enable)
            {
                Svc.Log.Debug("Endurance toggled off");
                Enable = false;
                IPCOverride = false;
                PreCrafting.Tasks.Clear();
            }
        }

        internal static void Dispose()
        {
            Svc.Toasts.ErrorToast -= Toasts_ErrorToast;
            Svc.Toasts.ErrorToast -= CheckNonMaxQuantityModeFinished;
        }

        internal static void Draw()
        {
            try
            {
                if (CraftingListUI.Processing)
                {
                    ImGui.TextWrapped("正在处理清单...");
                    return;
                }

                ImGui.TextWrapped("耐力模式是 Artisan 用来反复制作同一配方的方式，可以制作指定次数或直到材料耗尽。它具备完整的功能：当某件装备耐久低于设定百分比时自动修理、使用食物/爆发药/指南，以及从精炼度满的装备中精制魔晶石。请注意，这些设置独立于制作清单设置，仅用于重复制作单个物品。");
                ImGui.Separator();
                ImGui.Spacing();

                if (RecipeID == 0)
                {
                    ImGuiEx.TextV(ImGuiColors.DalamudRed, "未选择配方");
                }
                else
                {
                    if (!CraftingListFunctions.HasItemsForRecipe(RecipeID))
                        ImGui.BeginDisabled();

                    if (ImGui.Checkbox("启用耐力模式", ref enable))
                    {
                        ToggleEndurance(enable);
                    }

                    if (!CraftingListFunctions.HasItemsForRecipe(RecipeID))
                    {
                        ImGui.EndDisabled();

                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"您无法开始耐力模式，因为您没有制作此配方的材料。");
                            ImGui.EndTooltip();
                        }
                    }

                    ImGuiComponents.HelpMarker("要开始耐力模式，您应先在制作菜单中选择配方。\n耐力模式将自动重复选中的配方，类似于自动制作，但在开始前会考虑食物/爆发药增益效果。");

                    ImGuiEx.Text($"配方：{RecipeName} {(RecipeID != 0 ? $"({LuminaSheets.ClassJobSheet[LuminaSheets.RecipeSheet[RecipeID].CraftType.RowId + 8].Abbreviation})" : "")}");
                }

                bool repairs = P.Config.Repair;
                if (ImGui.Checkbox("自动修理", ref repairs))
                {
                    P.Config.Repair = repairs;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"启用后，当任意装备耐久达到设定的修理阈值时，Artisan 将自动修理您的装备。\n\n当前最低装备耐久为 {RepairManager.GetMinEquippedPercent()}%，在NPC处修理费用为 {RepairManager.GetNPCRepairPrice()} 金币。\n\n如果无法使用暗物质修理，将尝试寻找附近的修理NPC。");
                if (P.Config.Repair)
                {
                    //ImGui.SameLine();
                    ImGui.PushItemWidth(200);
                    int percent = P.Config.RepairPercent;
                    if (ImGui.SliderInt("##repairp", ref percent, 10, 100, $"%d%%"))
                    {
                        P.Config.RepairPercent = percent;
                        P.Config.Save();
                    }
                }

                if (!CharacterInfo.MateriaExtractionUnlocked())
                    ImGui.BeginDisabled();

                bool materia = P.Config.Materia;
                if (ImGui.Checkbox("自动提取魔晶石", ref materia))
                {
                    P.Config.Materia = materia;
                    P.Config.Save();
                }

                if (!CharacterInfo.MateriaExtractionUnlocked())
                {
                    ImGui.EndDisabled();

                    ImGuiComponents.HelpMarker("该角色尚未解锁魔晶石精制。此设置将被忽略。");
                }
                else
                    ImGuiComponents.HelpMarker("当已装备的装备精炼度达到100%时将自动提取魔晶石");

                ImGui.Checkbox("仅制作X次", ref P.Config.CraftingX);
                if (P.Config.CraftingX)
                {
                    ImGui.Text("制作次数：");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(200);
                    if (ImGui.InputInt("###TimesRepeat", ref P.Config.CraftX))
                    {
                        if (P.Config.CraftX < 0)
                            P.Config.CraftX = 0;
                    }
                }

                if (ImGui.Checkbox("尽可能使用简易制作", ref P.Config.QuickSynthMode))
                {
                    P.Config.Save();
                }

                bool stopIfFail = P.Config.EnduranceStopFail;
                if (ImGui.Checkbox("制作失败时禁用耐力模式", ref stopIfFail))
                {
                    P.Config.EnduranceStopFail = stopIfFail;
                    P.Config.Save();
                }

                bool stopIfNQ = P.Config.EnduranceStopNQ;
                if (ImGui.Checkbox("制作出 NQ 物品时禁用耐力模式", ref stopIfNQ))
                {
                    P.Config.EnduranceStopNQ = stopIfNQ;
                    P.Config.Save();
                }

                if (ImGui.Checkbox("最大数量模式", ref P.Config.MaxQuantityMode))
                {
                    P.Config.Save();
                }

                ImGuiComponents.HelpMarker("将为您自动设置材料，以最大化可制作次数。");

                if (ImGui.Checkbox("完成后退出制作姿态", ref P.Config.ExitCraftStanceEndurance))
                {
                    P.Config.Save();
                }
            }
            catch { }
        }

        internal static void DrawRecipeData()
        {
            var curRec = Operations.GetSelectedRecipeEntry();
            if (curRec is null || curRec->RecipeId == 0)
                return;

            RecipeID = curRec->RecipeId;
            try
            {
                for (int i = 0; i < curRec->IngredientsSpan.Length; i++)
                {
                    var ing = curRec->IngredientsSpan[i];
                    if (ing.ItemId == 0)
                        break;
                    var nq = ing.NumAssignedNQ;
                    var hq = ing.NumAssignedHQ;

                    SetIngredients[i] = new EnduranceIngredients()
                    {
                        NQSet = nq,
                        HQSet = hq,
                    };

                    //Svc.Log.Debug($"Assigned {nq}NQ, {hq}HQ {ing.ItemId.NameOfItem()}");
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "Setting Recipe ID");
                RecipeID = 0;
            }


        }

        internal static void Init()
        {
            Svc.Toasts.ErrorToast += Toasts_ErrorToast;
            Svc.Toasts.ErrorToast += CheckNonMaxQuantityModeFinished;
        }

        private static bool enable = false;
        private static void CheckNonMaxQuantityModeFinished(ref SeString message, ref bool isHandled)
        {
            if (!P.Config.MaxQuantityMode && Enable &&
                (message.GetText() == Svc.Data.GetExcelSheet<LogMessage>()?.First(x => x.RowId == 1147).Text.GetText() ||
                 message.GetText() == Svc.Data.GetExcelSheet<LogMessage>()?.First(x => x.RowId == 1146).Text.GetText() ||
                 message.GetText() == Svc.Data.GetExcelSheet<LogMessage>()?.First(x => x.RowId == 1145).Text.GetText() ||
                 message.GetText() == Svc.Data.GetExcelSheet<LogMessage>()?.First(x => x.RowId == 1144).Text.GetText()))
            {
                if (P.Config.PlaySoundFinishEndurance)
                    SoundPlayer.PlaySound();

                ToggleEndurance(false);

                if (P.Config.ExitCraftStanceEndurance)
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), default));
            }
        }

        public static void Update()
        {
            if (!Enable) return;
            var needToRepair = P.Config.Repair && RepairManager.GetMinEquippedPercent() < P.Config.RepairPercent && (RepairManager.CanRepairAny() || RepairManager.RepairNPCNearby(out _));
            if ((Crafting.CurState == Crafting.State.QuickCraft && Crafting.QuickSynthCompleted) || needToRepair ||
                (P.Config.Materia && Spiritbond.IsSpiritbondReadyAny() && CharacterInfo.MateriaExtractionUnlocked()))
            {
                Operations.CloseQuickSynthWindow();
            }

            if (!P.TM.IsBusy && Crafting.CurState is Crafting.State.IdleNormal or Crafting.State.IdleBetween)
            {
                var isCrafting = Svc.Condition[ConditionFlag.Crafting];
                var preparing = Svc.Condition[ConditionFlag.PreparingToCraft];
                var recipe = LuminaSheets.RecipeSheet[RecipeID];
                if (PreCrafting.Tasks.Count > 0)
                {
                    return;
                }

                if ((P.Config.CraftingX && P.Config.CraftX == 0) || PreCrafting.GetNumberCraftable(recipe) == 0)
                {
                    Svc.Log.Debug($"Run out items to craft {P.Config.CraftingX} {P.Config.CraftX} {PreCrafting.GetNumberCraftable(recipe)}");
                    ToggleEndurance(false);
                    P.Config.CraftingX = false;
                    if (P.Config.PlaySoundFinishEndurance)
                        SoundPlayer.PlaySound();
                    if (P.Config.ExitCraftStanceEndurance)
                        PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), default));

                    return;
                }

                if (RecipeID == 0)
                {
                    Svc.Toasts.ShowError("尚未为耐力模式设置配方。正在禁用耐力模式。");
                    DuoLog.Error("尚未为耐力模式设置配方。正在禁用耐力模式。");
                    ToggleEndurance(false);
                    return;
                }

                if ((Job)LuminaSheets.RecipeSheet[RecipeID].CraftType.RowId + 8 != CharacterInfo.JobID)
                {
                    PreCrafting.equipGearsetLoops = 0;
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), TimeSpan.FromMilliseconds(200)));
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskClassChange((Job)LuminaSheets.RecipeSheet[RecipeID].CraftType.RowId + 8), TimeSpan.FromMilliseconds(200)));
                    return;
                }

                bool needEquipItem = recipe.ItemRequired.RowId > 0 && !PreCrafting.IsItemEquipped(recipe.ItemRequired.RowId);
                if (needEquipItem)
                {
                    PreCrafting.equipAttemptLoops = 0;
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskEquipItem(recipe.ItemRequired.RowId), TimeSpan.FromMilliseconds(200)));
                    return;
                }

                if (!Spiritbond.ExtractMateriaTask(P.Config.Materia))
                {
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), TimeSpan.FromMilliseconds(200)));
                    return;
                }

                if (P.Config.Repair && !RepairManager.ProcessRepair())
                {
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), TimeSpan.FromMilliseconds(200)));
                    return;
                }

                var config = P.Config.RecipeConfigs.GetValueOrDefault(RecipeID) ?? new();
                PreCrafting.CraftType type = P.Config.QuickSynthMode && recipe.CanQuickSynth && P.ri.HasRecipeCrafted(recipe.RowId) ? PreCrafting.CraftType.Quick : PreCrafting.CraftType.Normal;
                bool needConsumables = PreCrafting.NeedsConsumablesCheck(type, config, recipe);
                bool hasConsumables = PreCrafting.HasConsumablesCheck(config);

                if (P.Config.AbortIfNoFoodPot && needConsumables && !hasConsumables)
                {
                    PreCrafting.MissingConsumablesMessage(recipe, config);
                    ToggleEndurance(false);
                    return;
                }

                var skipping = ConsumableChecker.SkippingConsumablesByConfig(recipe);

                if (!skipping)
                {
                    bool needFood = config != default && ConsumableChecker.HasItem(config.RequiredFood, config.RequiredFoodHQ) && !ConsumableChecker.IsFooded(config);
                    bool needPot = config != default && ConsumableChecker.HasItem(config.RequiredPotion, config.RequiredPotionHQ) && !ConsumableChecker.IsPotted(config);
                    bool needManual = config != default && ConsumableChecker.HasItem(config.RequiredManual, false) && !ConsumableChecker.IsManualled(config);
                    bool needSquadronManual = config != default && ConsumableChecker.HasItem(config.RequiredSquadronManual, false) && !ConsumableChecker.IsSquadronManualled(config);

                    if (needFood || needPot || needManual || needSquadronManual)
                    {
                        if (!P.TM.IsBusy && !PreCrafting.Occupied())
                        {
                            if (needManual || needSquadronManual)
                                P.TM.Enqueue(() => PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), TimeSpan.FromMilliseconds(200))));
                            P.TM.Enqueue(() => PreCrafting.Tasks.Add((() => PreCrafting.TaskUseConsumables(config, type), TimeSpan.FromMilliseconds(200))));
                            P.TM.DelayNext(100);
                        }
                        return;
                    }
                }

                if (Crafting.CurState is Crafting.State.IdleBetween or Crafting.State.IdleNormal && !PreCrafting.Occupied())
                {
                    if (!P.TM.IsBusy)
                    {
                        PreCrafting.Tasks.Add((() => PreCrafting.TaskSelectRecipe(recipe), TimeSpan.FromMilliseconds(500)));

                        if (!CraftingListFunctions.RecipeWindowOpen() && !CraftingListFunctions.CosmicLogOpen()) return;

                        if (type == PreCrafting.CraftType.Quick)
                        {
                            P.TM.Enqueue(() => Operations.QuickSynthItem(P.Config.CraftingX ? P.Config.CraftX : 99), "EnduranceQSStart");
                            P.TM.Enqueue(() => Crafting.CurState is Crafting.State.WaitStart, 5000, "EnduranceQSWaitStart");
                        }
                        else if (type == PreCrafting.CraftType.Normal)
                        {
                            P.TM.DelayNext(200);

                            if (P.Config.MaxQuantityMode)
                                P.TM.Enqueue(() => CraftingListFunctions.SetIngredients(), "EnduranceSetIngredientsNonLayout");
                            else
                                P.TM.Enqueue(() => CraftingListFunctions.SetIngredients(SetIngredients), "EnduranceSetIngredientsLayout");

                            P.TM.Enqueue(() => Operations.RepeatActualCraft(), 500, "EnduranceNormalStart");
                            P.TM.Enqueue(() => Crafting.CurState is Crafting.State.WaitStart, 500, "EnduranceNormalWaitStart");
                            P.TM.Enqueue(() =>
                            {
                                if (!RaphaelCache.InProgressAny())
                                {
                                    if (FailedStarts.Count() >= 5 && FailedStarts.All(x => x > Environment.TickCount64 - (10 * 1000)))
                                    {
                                        FailedStarts.Clear();
                                        if (Crafting.CurState is not Crafting.State.QuickCraft and not Crafting.State.InProgress and not Crafting.State.WaitStart)
                                        {
                                            if (!IPCOverride)
                                            {
                                                DuoLog.Error($"无法开始制作。正在禁用耐力模式。 {(!P.Config.MaxQuantityMode ? "请启用最大数量模式或在开始前设置材料。" : "")}");
                                            }
                                            else
                                            {
                                                DuoLog.Error($"在另一个插件尝试控制 Artisan 时出现了问题。正在禁用耐力模式。");
                                            }
                                            ToggleEndurance(false);
                                        }
                                    }
                                    else
                                    {
                                        FailedStarts.PushBack(Environment.TickCount64);
                                    }
                                }
                            });

                        }
                    }

                }
            }
        }

        private static void Toasts_ErrorToast(ref SeString message, ref bool isHandled)
        {
            if (Enable || (CraftingListUI.Processing && !CraftingListFunctions.Paused))
            {
                //foreach (uint errorId in UnableToCraftErrors)
                //{
                //    if (message.GetText() == Svc.Data.GetExcelSheet<LogMessage>()?.First(x => x.RowId == errorId).Text.GetText())
                //    {
                //        Svc.Toasts.ShowError($"Current crafting mode has been {(Enable ? "disabled" : "paused")} due to unable to craft error.");
                //        DuoLog.Error($"Current crafting mode has been {(Enable ? "disabled" : "paused")} due to unable to craft error.");
                //        if (enable)
                //            ToggleEndurance(false);
                //        if (CraftingListUI.Processing)
                //            CraftingListFunctions.Paused = true;
                //        PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), default));

                //        P.TM.Abort();
                //        CraftingListFunctions.CLTM.Abort();
                //    }
                //}

                Errors.PushBack(Environment.TickCount64);
                Svc.Log.Warning($"Error Warnings [{Errors.Count(x => x > Environment.TickCount64 - 10 * 1000)}]: {message}");
                if (Errors.Count() >= 5 && Errors.All(x => x > Environment.TickCount64 - 10 * 1000))
                {
                    Svc.Toasts.ShowError($"由于连续出现过多错误，当前制作模式已{(Enable ? "禁用" : "暂停")}。");
                    DuoLog.Error($"由于连续出现过多错误，当前制作模式已{(Enable ? "禁用" : "暂停")}。");
                    if (enable)
                        ToggleEndurance(false);
                    if (CraftingListUI.Processing)
                        CraftingListFunctions.Paused = true;
                    if (P.Config.PlaySoundError)
                        SoundPlayer.PlaySound();
                    Errors.Clear();
                    PreCrafting.Tasks.Add((() => PreCrafting.TaskExitCraft(), default));

                    P.TM.Abort();
                    CraftingListFunctions.CLTM.Abort();
                }
            }
        }
    }
}
