﻿using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common.Threading;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using Ensage.SDK.Extensions;

using SharpDX;

namespace SkywrathMagePlus
{
    internal class SpamMode
    {
        private Config Config { get; }

        private SkywrathMagePlus Main { get; }

        private IServiceContext Context { get; }

        private TaskHandler Handler { get; set; }

        private Unit Target { get; set; }

        public SpamMode(Config config, IServiceContext context)
        {
            Config = config;
            Main = config.SkywrathMagePlus;
            Context = context;

            config.SpamKeyItem.PropertyChanged += SpamKeyChanged;

            Handler = UpdateManager.Run(ExecuteAsync, true, false);
        }

        public void Dispose()
        {
            Config.SpamKeyItem.PropertyChanged -= SpamKeyChanged;
        }

        private void SpamKeyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Config.SpamKeyItem.Value)
            {
                Handler.RunAsync();
            }
            else
            {
                Handler?.Cancel();

                if (Target != null)
                {
                    if (!Context.TargetSelector.IsActive)
                    {
                        Context.TargetSelector.Activate();
                    }

                    Target = null;
                }

                Context.Particle.Remove("SpamTarget");
            }
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            if (Config.TargetItem.Value.SelectedValue.Contains("Lock")
                && (Target == null || !Target.IsValid || !Target.IsAlive))
            {
                if (!Context.TargetSelector.IsActive)
                {
                    Context.TargetSelector.Activate();
                }

                if (Context.TargetSelector.IsActive)
                {
                    if (Config.SpamUnitItem.Value)
                    {
                        Target = EntityManager<Unit>.Entities.OrderBy(
                        order => order.Distance2D(Game.MousePosition)).FirstOrDefault(
                        x => !x.IsIllusion &&
                        x.IsAlive &&
                        x.IsVisible &&
                        x.IsValid &&
                        (x.IsNeutral ||
                        x.Name == "npc_dota_roshan" ||
                        (x.Team != Context.Owner.Team &&
                        x as Creep != null))
                        && x.Distance2D(Game.MousePosition) <= 100);
                    }
                    
                    if (Target == null)
                    {
                        Target = Context.TargetSelector.Active.GetTargets().FirstOrDefault();
                    }
                }

                if (Target != null)
                {
                    if (Context.TargetSelector.IsActive)
                    {
                        Context.TargetSelector.Deactivate();
                    }
                }
            } 

            if (Target != null)
            {
                Context.Particle.DrawTargetLine(
                    Context.Owner,
                    "SpamTarget",
                    Target.Position,
                    Color.Green);

                if (!Target.IsMagicImmune())
                {
                    // ArcaneBolt
                    if (Main.ArcaneBolt != null
                        && Config.AbilityToggler.Value.IsEnabled(Main.ArcaneBolt.Ability.Name)
                        && Main.ArcaneBolt.CanBeCasted)
                    {
                        Main.ArcaneBolt.UseAbility(Target);
                        await Await.Delay(Main.ArcaneBolt.GetCastDelay(), token);
                    }
                }
                if (Target == null || Target.IsAttackImmune() || Target.IsInvulnerable())
                {
                    Context.Orbwalker.Move(Game.MousePosition);
                }
                else if (Target != null)
                {
                    Context.Orbwalker.OrbwalkTo(Target);
                }
            }
            else
            {
                Context.Orbwalker.Move(Game.MousePosition);
                Context.Particle.Remove("SpamTarget");
            }
        }
    }
}
