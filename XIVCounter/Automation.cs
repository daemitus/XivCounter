using Dalamud.Game.Text;
using Dalamud.Game.ClientState.Actors.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace XIVCounter
{
    internal class Automation : IDisposable
    {
        private CancellationTokenSource cancellationToken = new();
        private XIVCounterPlugin core;
        public bool Enabled;
        private int lastTargetID;
        private int trackedPos;
        private TrackedMob[] trackedTargets;
        private int[] trackedIDs;
        private int updateNum;
        private int autoSaveNum;
        private ushort lastRegionID;

        public Automation(XIVCounterPlugin corePlugin)
        {
            this.core = corePlugin;
            this.trackedTargets = new TrackedMob[this.core.settings.MaxTargetCache];
            this.trackedIDs = new int[this.core.settings.MaxTargetCache];
            for (int index = 0; index < this.core.settings.MaxTargetCache; ++index)
            {
                this.trackedTargets[index] = new TrackedMob(0, "");
                this.trackedIDs[index] = 0;
            }
        }

        public void Dispose() => this.cancellationToken.Cancel();

        public void StartUpdate()
        {
            this.cancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew<Task>(async () =>
           {
               this.Enabled = true;
               this.lastRegionID = this.core.GetPlayerTerritoryType();
               while (!this.cancellationToken.IsCancellationRequested)
               {
                   this.Update();
                   await Task.Delay(this.core.settings.UpdateWaitMs);
               }
               this.Enabled = false;
           }, this.cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void StopUpdate()
        {
            this.cancellationToken.Cancel();
            this.ResetTracked();
        }

        private void ResetTracked()
        {
            for (int index = 0; index < this.core.settings.MaxTargetCache; ++index)
            {
                this.trackedTargets[index] = new TrackedMob(0, "");
                this.trackedIDs[index] = 0;
            }
        }

        public void Update()
        {
            int playerTargetId = this.core.GetPlayerTargetID();
            if (this.lastTargetID != playerTargetId && playerTargetId != 0 && !((IEnumerable<int>)this.trackedIDs).Contains<int>(playerTargetId) && playerTargetId > 0)
            {
                string playerTargetName = this.core.GetPlayerTargetName();
                this.trackedTargets[this.trackedPos] = new TrackedMob(playerTargetId, playerTargetName);
                this.trackedIDs[this.trackedPos] = playerTargetId;
                this.lastTargetID = playerTargetId;
                this.IncrPos();
            }
            this.IncrUpdateNum();
            if (this.autoSaveNum == 0)
            {
                this.core.settings.Counters = this.core.counters;
                this.core.settings.SaveSettings();
            }
            if ((uint)this.updateNum > 0U)
                return;
            ushort playerTerritoryType = this.core.GetPlayerTerritoryType();
            if (this.lastRegionID != playerTerritoryType)
            {
                this.core.DisableAllAutomations();
                this.core.PostChatMessage("[Counter] All Automations have been disabled due to a region change.", (XivChatType)56);
                this.lastRegionID = playerTerritoryType;
            }
            using (IEnumerator<Actor> enumerator = this.core.GetActorTable().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Actor current = enumerator.Current;
                    try
                    {
                        if (current.ActorId > 0)
                        {
                            for (int index = 0; index < this.trackedIDs.Length; ++index)
                            {
                                if (this.trackedIDs[index] == current.ActorId)
                                    this.trackedTargets[index].Alive = true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            for (int index = 0; index < this.trackedTargets.Length; ++index)
            {
                if (!this.trackedTargets[index].Alive && (uint)this.trackedIDs[index] > 0U)
                {
                    this.core.IncrementAutomations(this.trackedTargets[index].Name);
                    this.trackedTargets[index] = new TrackedMob(0, "");
                    this.trackedIDs[index] = 0;
                    if (this.trackedIDs[index] == this.lastTargetID)
                        this.lastTargetID = 0;
                }
                else
                    this.trackedTargets[index].Alive = false;
            }
        }

        public void IncrPos()
        {
            ++this.trackedPos;
            if (this.trackedPos < this.core.settings.MaxTargetCache)
                return;
            this.trackedPos = 0;
        }

        public void IncrUpdateNum()
        {
            ++this.updateNum;
            if (this.updateNum >= this.core.settings.BigUpdateCounter)
                this.updateNum = 0;
            ++this.autoSaveNum;
            if (this.autoSaveNum < this.core.settings.AutoSaveCounter)
                return;
            this.autoSaveNum = 0;
        }
    }
}
