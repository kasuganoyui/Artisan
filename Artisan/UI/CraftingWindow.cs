using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.CraftingLogic.Solvers;
using Artisan.GameInterop;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using System;
using ECommons;
using Lumina.Excel.Sheets;

namespace Artisan.UI
{
    internal class CraftingWindow : Window, IDisposable
    {
        public bool RepeatTrial;
        private DateTime _estimatedCraftEnd;
        public int _delay;
        private int _doNextCounter;
        private int _doNextTotal;

        public CraftingWindow() : base("Artisan 制作窗口###MainCraftWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
        {
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            this.SizeConstraints = new()
            {
                MinimumSize = new System.Numerics.Vector2(150f, 0f),
                MaximumSize = new System.Numerics.Vector2(310f, 500f)
            };

            CraftingProcessor.SolverStarted += OnSolverStarted;
            CraftingProcessor.SolverFailed += OnSolverFailed;
            CraftingProcessor.SolverFinished += OnSolverFinished;
            CraftingProcessor.RecommendationReady += OnRecommendationReady;
            Crafting.CraftFinished += OnCraftFinished;
            Crafting.CraftAdvanced += OnCraftAdvanced;

            this.TitleBarButtons.Add(new()
            {
                Icon = FontAwesomeIcon.Cog,
                ShowTooltip = () => ImGuiEx.SetTooltip("打开配置"),
                Click = (x) => P.PluginUi.IsOpen = true,
            });

            _delay = P.Config.AutoDelay;
        }

        private void OnCraftAdvanced(Recipe? recipe, CraftState craft, StepState step)
        {
            _doNextCounter--;
        }

        private void OnCraftFinished(Recipe? recipe, CraftState craft, StepState finalStep, bool cancelled)
        {
            _doNextCounter = 0;
            _doNextTotal = 0;
        }

        public void Dispose()
        {
            CraftingProcessor.SolverStarted -= OnSolverStarted;
            CraftingProcessor.SolverFailed -= OnSolverFailed;
            CraftingProcessor.SolverFinished -= OnSolverFinished;
            CraftingProcessor.RecommendationReady -= OnRecommendationReady;
            Crafting.CraftFinished -= OnCraftFinished;
            Crafting.CraftAdvanced -= OnCraftAdvanced;
        }

        public override bool DrawConditions()
        {
            bool crafting = Crafting.CurState is Crafting.State.InProgress or Crafting.State.QuickCraft or Crafting.State.WaitAction;
            bool waitingForRaph = RaphaelCache.InProgressAny() && Crafting.CurState is Crafting.State.WaitStart;
            return crafting || waitingForRaph;
        }

        public override void PreDraw()
        {
            if (!P.Config.DisableTheme)
            {
                P.Style.Push();
                P.StylePushed = true;
            }
        }

        public override void PostDraw()
        {
            if (P.StylePushed)
            {
                P.Style.Pop();
                P.StylePushed = false;
            }
        }

        public override void Draw()
        {
            try
            {
                if (RaphaelCache.InProgressAny())
                {
                    ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, "Raphael 正在生成中，请稍候...");
                    if (ImGui.Button("紧急取消按钮"))
                    {
                        foreach (var t in RaphaelCache.Tasks)
                        {
                            try
                            {
                                t.Value.Cancellation.Cancel();
                            }
                            catch (Exception e)
                            {
                                e.Log("Emergency button pushed but couldn't cancel?");
                            }
                        }
                        RaphaelCache.Tasks.Clear();
                    }
                    return;
                }

                if (!P.Config.DisableHighlightedAction)
                    Hotbars.MakeButtonsGlow(CraftingProcessor.NextRec.Action);

                if (Crafting.CurCraft != null && !Crafting.CurCraft.CraftExpert && Crafting.CurRecipe?.SecretRecipeBook.RowId > 0 && Crafting.CurCraft?.CraftLevel == Crafting.CurCraft?.StatLevel && !CraftingProcessor.ActiveSolver.IsType<MacroSolver>())
                {
                    ImGui.Dummy(new System.Numerics.Vector2(12f));
                    ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, "这是当前等级的大师配方。成功率可能有所不同，建议使用 Artisan 宏或手动制作。");
                }

                bool autoMode = P.Config.AutoMode;
                if (ImGui.Checkbox("自动行动模式", ref autoMode))
                {
                    P.Config.AutoMode = autoMode;
                    P.Config.Save();
                }

                if (autoMode && !P.Config.ReplicateMacroDelay)
                {
                    ImGui.PushItemWidth(200);
                    ImGui.SliderInt("设置延迟（毫秒）", ref _delay, 0, 1000);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        if (_delay < 0) _delay = 0;
                        if (_delay > 1000) _delay = 1000;

                        P.Config.AutoDelay = _delay;
                        P.Config.Save();
                    }
                }

                if (Endurance.RecipeID != 0 && !CraftingListUI.Processing && Endurance.Enable)
                {
                    if (ImGui.Button("禁用耐力模式"))
                    {
                        Endurance.ToggleEndurance(false);
                        P.TM.Abort();
                        CraftingListFunctions.CLTM.Abort();
                        PreCrafting.Tasks.Clear();
                    }
                }

                if (!Endurance.Enable && Crafting.IsTrial)
                    ImGui.Checkbox("重复试做", ref RepeatTrial);

                if (CraftingProcessor.ActiveSolver)
                {
                    var text = $"正在使用 {CraftingProcessor.ActiveSolver.Name}";
                    if (CraftingProcessor.NextRec.Comment.Length > 0)
                        text += $" ({CraftingProcessor.NextRec.Comment})";
                    ImGuiEx.TextWrapped(text.Replace("%", ""));
                }

                if (P.Config.CraftingX && Endurance.Enable)
                    ImGui.Text($"剩余制作次数：{P.Config.CraftX}");

                if (_estimatedCraftEnd != default)
                {
                    var diff = _estimatedCraftEnd - DateTime.Now;
                    string duration = string.Format("{0:D2}h {1:D2}m {2:D2}s", diff.Hours, diff.Minutes, diff.Seconds);
                    ImGui.Text($"预计剩余时间：{duration}");
                }

                if (!P.Config.AutoMode)
                {
                    ImGui.Text("半手动模式");

                    var action = CraftingProcessor.NextRec.Action;
                    using var disable = ImRaii.Disabled(action == Skills.None);

                    if (P.Config.UseDoNextX)
                    {
                        if (_doNextCounter <= 0)
                        {
                            if (ImGui.Button($"执行接下来 {P.Config.DoNextXAmount} 个动作"))
                            {
                                _doNextCounter = P.Config.DoNextXAmount;
                                _doNextTotal = P.Config.DoNextXAmount;
                                ActionManagerEx.UseSkill(action);
                            }
                        }
                        else
                        {
                            if (ImGui.Button("取消队列"))
                            {
                                _doNextCounter = 0;
                                _doNextTotal = 0;
                            }
                        }

                        if (_doNextCounter > 0)
                        {
                            var remaining = _doNextTotal - _doNextCounter;
                            ImGuiEx.Text($"正在处理动作 {remaining}/{_doNextTotal}");
                        }
                    }
                    else
                    {
                        if (ImGui.Button("执行推荐行动"))
                        {
                            ActionManagerEx.UseSkill(action);
                        }
                    }
                    if (ImGui.Button("获取推荐"))
                    {
                        ShowRecommendation(action);
                    }
                }
            }
            catch { } //Idaf about your error windows
        }

        private void ShowRecommendation(Skills action)
        {
            if (!P.Config.DisableToasts)
            {
                QuestToastOptions options = new() { IconId = action.IconOfAction(CharacterInfo.JobID) };
                Svc.Toasts.ShowQuest($"使用 {action.NameOfAction()}", options);
            }
        }

        private void OnSolverStarted(Lumina.Excel.Sheets.Recipe recipe, SolverRef solver, CraftState craft, StepState initialStep)
        {
            if (P.Config.AutoMode && solver)
            {
                var estimatedTime = SolverUtils.EstimateCraftTime(solver.Clone()!, craft, initialStep.Quality);
                var count = P.Config.CraftingX && Endurance.Enable ? P.Config.CraftX : 1;
                _estimatedCraftEnd = DateTime.Now + count * estimatedTime;
            }
        }

        private void OnSolverFailed(Lumina.Excel.Sheets.Recipe recipe, string reason)
        {
            var text = $"{reason}。Artisan 将不会继续。";
            Svc.Toasts.ShowError(text);
            DuoLog.Error(text);
        }

        private void OnSolverFinished(Lumina.Excel.Sheets.Recipe? recipe, SolverRef solver, CraftState craft, StepState finalStep)
        {
            _estimatedCraftEnd = default;
        }

        private void OnRecommendationReady(Lumina.Excel.Sheets.Recipe? recipe, SolverRef solver, CraftState craft, StepState step, Solver.Recommendation recommendation)
        {
            if (!Simulator.CanUseAction(craft, step, recommendation.Action))
            {
                return;
            }
            ShowRecommendation(recommendation.Action);
            if (P.Config.AutoMode || Endurance.IPCOverride || _doNextCounter > 1)
            {
                Svc.Log.Debug($"{_doNextCounter} donext");
                if (!P.Config.ReplicateMacroDelay)
                    P.CTM.DelayNext(P.Config.AutoDelay);
                P.CTM.Enqueue(() => Crafting.CurState == Crafting.State.InProgress, 3000, true, "WaitForStateToUseAction");
                P.CTM.Enqueue(() => ActionManagerEx.UseSkill(recommendation.Action));
                if (P.Config.ReplicateMacroDelay)
                    P.CTM.DelayNext(Calculations.ActionIsLengthyAnimation(recommendation.Action) ? 3000 : 2000);
            }
        }
    }
}
