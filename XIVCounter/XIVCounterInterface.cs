using Dalamud.Game.Text;
using ImGuiNET;
using System;
using System.Numerics;

namespace XIVCounter
{
    internal class XIVCounterInterface : IDisposable
    {
        private XIVCounterPlugin _core;
        private bool _visible;
        private bool _settingsVisible;
        private string _newInput = "";
        private Vector4 AUTOMATED_COLOR = new(0.28f, 0.82f, 0.8f, 1f);
        private int[] COLUMN_WIDTHS = new int[4] { 160, 35, 50, 35 };
        private int _updateWaitMs;
        private int _bigUpdateCounter;
        private int _autoSaveCounter;
        private int _maxTargetCache;
        private int _shiftMod;
        private int _ctrlMod;
        private int _bothMod;

        public bool Visible
        {
            get => this._visible;
            set => this._visible = value;
        }

        public bool SettingsVisible
        {
            get => this._settingsVisible;
            set => this._settingsVisible = value;
        }

        public XIVCounterInterface(XIVCounterPlugin corePlugin) => this._core = corePlugin;

        public void InitSettingsVariables()
        {
            this._updateWaitMs = this._core.settings.UpdateWaitMs;
            this._bigUpdateCounter = this._core.settings.BigUpdateCounter;
            this._autoSaveCounter = this._core.settings.AutoSaveCounter;
            this._maxTargetCache = this._core.settings.MaxTargetCache;
            this._shiftMod = this._core.settings.ShiftMod;
            this._ctrlMod = this._core.settings.CtrltMod;
            this._bothMod = this._core.settings.BothMod;
        }

        public void Dispose()
        {
            this.Visible = false;
            this.SettingsVisible = false;
        }

        public void Draw()
        {
            if (!this.Visible)
                return;
            ImGui.SetNextWindowSize(new Vector2(300f, 300f), ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(300f, 160f));
            if (ImGui.Begin("Counter", ref this._visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.AlignTextToFramePadding();
                if (ImGui.ArrowButton("##fillTargetName", ImGuiDir.Right))
                    this._newInput = this._core.GetPlayerTargetName();
                ImGui.SameLine();
                ImGui.InputText("##newCounterInput", ref this._newInput, 48U);
                ImGui.SameLine();
                if (ImGui.Button("Create"))
                    this._core.CreateCounter(this._newInput);
                ImGui.Separator();
                ImGui.BeginChild("##counterList", new Vector2(0.0f, -35f));
                this.DrawCounterList();
                ImGui.EndChild();
                ImGui.Separator();
                if (ImGui.Button("Save Counters"))
                {
                    this._core.settings.Counters = this._core.counters;
                    this._core.settings.SaveSettings();
                }
                ImGui.SameLine();
                if (ImGui.Button("Automate All"))
                    this._core.EnableAllAutomations();
                ImGui.SameLine();
                if (ImGui.Button("De-Automate All"))
                    this._core.DisableAllAutomations();
            }
            ImGui.End();
            ImGui.PopStyleVar();
            this._core.ExecuteDeleteQueue();
        }

        private void DrawCounterList()
        {
            ImGui.BeginChild("##counterlist_internal");
            ImGui.Columns(4, "counter columns", false);
            int num = this.COLUMN_WIDTHS[0];
            for (int column_index = 1; column_index < this.COLUMN_WIDTHS.Length; ++column_index)
            {
                ImGui.SetColumnWidth(column_index, this.COLUMN_WIDTHS[column_index]);
                num += this.COLUMN_WIDTHS[column_index];
            }
            ImGui.SetColumnWidth(0, this.COLUMN_WIDTHS[0] + (ImGui.GetWindowSize().X - num));
            foreach (Counter counter in this._core.counters)
            {
                if (!counter.Automated)
                    ImGui.Text(counter.Name);
                else
                    ImGui.TextColored(this.AUTOMATED_COLOR, counter.Name);
                ImGui.SameLine();
                if (ImGui.BeginPopupContextItem("##counterPopup" + counter.GetHashCode().ToString()))
                {
                    if (!counter.Automated)
                    {
                        if (ImGui.Selectable("Enable Automation"))
                            this._core.EnableAutomation(counter.GetHashCode());
                    }
                    else if (ImGui.Selectable("Disable Automation"))
                        this._core.DisableAutomation(counter.GetHashCode());
                    if (ImGui.Selectable("Delete"))
                        this._core.QueueDeleteCounter(counter.GetHashCode());
                    ImGui.EndPopup();
                }
                ImGui.NextColumn();
                if (ImGui.ArrowButton("##counterDec" + counter.GetHashCode().ToString(), ImGuiDir.Down))
                    counter.Count -= this._core.GetChangeAmount();
                ImGui.NextColumn();
                string str = counter.Count.ToString();
                ImGui.SetCursorPosX((float)(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2.0) - (ImGui.CalcTextSize(str).X / 2.0)) - ImGui.GetStyle().ItemSpacing.X);
                ImGui.Text(str);
                ImGui.NextColumn();
                if (ImGui.ArrowButton("##counterInc" + counter.GetHashCode().ToString(), ImGuiDir.Up))
                    counter.Count += this._core.GetChangeAmount();
                ImGui.NextColumn();
            }
            ImGui.EndChild();
        }

        public void DrawSettings()
        {
            if (!this.SettingsVisible)
                return;
            ImGui.SetNextWindowSize(new Vector2(300f, 285f), ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(300f, 285f));
            if (ImGui.Begin("Counter Settings", ref this._settingsVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Milliseconds between updates:");
                ImGui.SameLine();
                ImGui.InputInt("##updateWaitMs", ref this._updateWaitMs);
                ImGui.Text("Check for kills every x updates:");
                ImGui.SameLine();
                ImGui.InputInt("##bigUpdate", ref this._bigUpdateCounter);
                ImGui.Text("Autosave counters every x updates:");
                ImGui.SameLine();
                ImGui.InputInt("##autoSave", ref this._autoSaveCounter);
                ImGui.Text("Mark x last targets for automation:");
                ImGui.SameLine();
                ImGui.InputInt("##targetCache", ref this._maxTargetCache);
                ImGui.Separator();
                ImGui.Text("Shift modifier on button:");
                ImGui.SameLine();
                ImGui.InputInt("##shiftMod", ref this._shiftMod);
                ImGui.Text("Control modifier on button:");
                ImGui.SameLine();
                ImGui.InputInt("##ctrlMod", ref this._ctrlMod);
                ImGui.Text("Shift+Control modifier on button:");
                ImGui.SameLine();
                ImGui.InputInt("##bothMod", ref this._bothMod);
                ImGui.Separator();
                if (ImGui.Button("Restore Defaults"))
                {
                    this._core.settings = new PluginConfiguration();
                    this._updateWaitMs = this._core.settings.UpdateWaitMs;
                    this._bigUpdateCounter = this._core.settings.BigUpdateCounter;
                    this._autoSaveCounter = this._core.settings.AutoSaveCounter;
                    this._maxTargetCache = this._core.settings.MaxTargetCache;
                    this._shiftMod = this._core.settings.ShiftMod;
                    this._ctrlMod = this._core.settings.CtrltMod;
                    this._bothMod = this._core.settings.BothMod;
                    this._core.settings.Counters = this._core.counters;
                    this._core.settings.SaveSettings();
                }
                if (ImGui.Button("Save"))
                {
                    this._core.settings.UpdateWaitMs = this._updateWaitMs;
                    this._core.settings.BigUpdateCounter = this._bigUpdateCounter;
                    this._core.settings.AutoSaveCounter = this._autoSaveCounter;
                    this._core.settings.MaxTargetCache = this._maxTargetCache;
                    this._core.settings.ShiftMod = this._shiftMod;
                    this._core.settings.CtrltMod = this._ctrlMod;
                    this._core.settings.BothMod = this._bothMod;
                    this._core.settings.Counters = this._core.counters;
                    this._core.settings.SaveSettings();
                    this._core.PostChatMessage("[Counter] New settings saved and applied.", (XivChatType)56);
                }
                ImGui.SameLine();
                if (ImGui.Button("Save and Close"))
                {
                    this._core.settings.UpdateWaitMs = this._updateWaitMs;
                    this._core.settings.BigUpdateCounter = this._bigUpdateCounter;
                    this._core.settings.AutoSaveCounter = this._autoSaveCounter;
                    this._core.settings.MaxTargetCache = this._maxTargetCache;
                    this._core.settings.ShiftMod = this._shiftMod;
                    this._core.settings.CtrltMod = this._ctrlMod;
                    this._core.settings.BothMod = this._bothMod;
                    this._core.settings.Counters = this._core.counters;
                    this._core.settings.SaveSettings();
                    this._core.PostChatMessage("[Counter] New settings saved and applied.", (XivChatType)56);
                    this.SettingsVisible = false;
                }
            }
            ImGui.End();
            ImGui.PopStyleVar();
        }
    }
}
