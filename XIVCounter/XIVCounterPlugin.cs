// Decompiled with JetBrains decompiler
// Type: XIVCounter.XIVCounterPlugin
// Assembly: XIVCounter, Version=0.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: B49E11ED-B3DF-46EC-9AE9-5449EAADE882
// Assembly location: C:\Users\Raymond\code\XIVCounter.dll

using Dalamud.Game.Text;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiScene;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace XIVCounter
{
    public class XIVCounterPlugin : IDalamudPlugin, IDisposable
    {
        public PluginConfiguration settings;
        private DalamudPluginInterface dpi;
        private Automation auto;
        private XIVCounterInterface counterInterface;
        private int toDeleteQueue = -1;
        internal List<Counter> counters;

        public string Name => "Simple Counter";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.dpi = pluginInterface;
            if (pluginInterface.GetPluginConfig() is not PluginConfiguration pluginConfiguration)
                pluginConfiguration = new PluginConfiguration();
            this.settings = pluginConfiguration;
            this.settings.Initialize(pluginInterface);
            try
            {
                this.auto = new Automation(this);
                this.counterInterface = new XIVCounterInterface(this);
                this.ApplySettings();
                this.dpi.UiBuilder.OnBuildUi += this.BuildInterface;
                var uiBuilder = this.dpi.UiBuilder;
                uiBuilder.OnOpenConfigUi += (s, a) => this.OnDisplaySettingsCommand("", "");
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Failed to initialize Automation or Interface in XIVCounter", Array.Empty<object>());
                this.auto?.Dispose();
                this.counterInterface?.Dispose();
            }
            var commandManager1 = this.dpi.CommandManager;
            CommandInfo commandInfo1 = new(OnDisplayCommand)
            {
                HelpMessage = "Displays the counter interface.",
                ShowInHelp = true
            };
            commandManager1.AddHandler("/counter", commandInfo1);
            var commandManager2 = this.dpi.CommandManager;
            CommandInfo commandInfo2 = new(OnDisplaySettingsCommand);
            commandInfo2.HelpMessage = "Displays the counter settings.";
            commandInfo2.ShowInHelp = true;
            commandManager2.AddHandler("/countersettings", commandInfo2);
        }

        public void ApplySettings()
        {
            this.counters = this.settings.Counters;
            this.counterInterface.InitSettingsVariables();
            foreach (Counter counter in this.counters)
            {
                if (counter.Automated && !this.auto.Enabled)
                {
                    this.auto.StartUpdate();
                    break;
                }
            }
        }

        public void Dispose()
        {
            this.settings.Counters = this.counters;
            this.settings.SaveSettings();
            this.auto.Dispose();
            this.dpi.UiBuilder.OnBuildUi -= this.BuildInterface;
            this.dpi.CommandManager.RemoveHandler("/counter");
            this.dpi.CommandManager.RemoveHandler("/countersettings");
            this.counters = null;
            this.counterInterface.Dispose();
        }

        public void EnableAutomation(int hash)
        {
            foreach (Counter counter in this.counters)
            {
                if (counter.GetHashCode() == hash)
                {
                    counter.Automated = true;
                    if (this.auto.Enabled)
                        break;
                    this.auto.StartUpdate();
                    break;
                }
            }
        }

        public void EnableAllAutomations()
        {
            foreach (Counter counter in this.counters)
                counter.Automated = true;
            if (this.auto.Enabled)
                return;
            this.auto.StartUpdate();
        }

        public void DisableAutomation(int hash)
        {
            bool flag = false;
            foreach (Counter counter in this.counters)
            {
                if (counter.GetHashCode() == hash)
                    counter.Automated = false;
                else if (counter.Automated)
                    flag = true;
            }
            if (flag)
                return;
            this.auto.StopUpdate();
        }

        public void DisableAllAutomations()
        {
            foreach (Counter counter in this.counters)
                counter.Automated = false;
            if (!this.auto.Enabled)
                return;
            this.auto.StopUpdate();
        }

        public int GetChangeAmount()
        {
            bool flag1 = this.dpi.ClientState.KeyState[(int)VirtualKeyCode.Ctrl];
            bool flag2 = this.dpi.ClientState.KeyState[(int)VirtualKeyCode.Shft];
            if (flag1 & flag2)
                return this.settings.BothMod;
            return flag2 ? this.settings.CtrltMod : (flag1 ? this.settings.ShiftMod : 1);
        }

        public void IncrementAutomations(string name)
        {
            foreach (Counter counter in this.counters)
            {
                if (counter.Automated && counter.Name == name)
                    ++counter.Count;
            }
        }

        public void CreateCounter(string name) => this.counters.Add(new Counter(name));

        public void QueueDeleteCounter(int hash)
        {
            for (int index = 0; index < this.counters.Count; ++index)
            {
                if (this.counters[index].GetHashCode() == hash)
                {
                    this.toDeleteQueue = index;
                    break;
                }
            }
        }

        public void ExecuteDeleteQueue()
        {
            if (this.toDeleteQueue < 0)
                return;
            this.counters.RemoveAt(this.toDeleteQueue);
            this.toDeleteQueue = -1;
            this.settings.Counters = this.counters;
            this.settings.SaveSettings();
        }

        private void BuildInterface()
        {
            this.counterInterface.Draw();
            this.counterInterface.DrawSettings();
        }

        private void OnDisplayCommand(string cmd, string args) => this.counterInterface.Visible = !this.counterInterface.Visible;

        private void OnDisplaySettingsCommand(string cmd, string args) => this.counterInterface.SettingsVisible = !this.counterInterface.SettingsVisible;

        public void PostChatMessage(string message, XivChatType type = XivChatType.Echo)
        {
            ChatGui chat = this.dpi.Framework.Gui.Chat;
            XivChatEntry xivChatEntry = new()
            {
                Type = type,
                MessageBytes = Encoding.UTF8.GetBytes(message)
            };
            chat.PrintChat(xivChatEntry);
        }

        public int GetPlayerTargetID()
        {
            int num = 0;
            try
            {
                num = this.dpi.ClientState.LocalPlayer.TargetActorID;
            }
            catch (Exception)
            {
            }
            return num;
        }

        public ushort GetPlayerTerritoryType() => this.dpi.ClientState.TerritoryType;

        public string GetPlayerTargetName(ActorTable table = null)
        {
            ActorTable actorTable = table ?? this.GetActorTable();
            int playerTargetId = this.GetPlayerTargetID();
            using (IEnumerator<Actor> enumerator = actorTable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Actor current = enumerator.Current;
                    try
                    {
                        if (current.ActorId == playerTargetId)
                            return current.Name;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return "No Target";
        }

        public ActorTable GetActorTable() => this.dpi.ClientState.Actors;
    }
}
