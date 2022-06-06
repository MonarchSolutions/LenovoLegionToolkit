﻿using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Listeners
{
    public class PowerPlanListener : AbstractEventLogListener
    {
        private readonly PowerModeFeature _feature = new();

        public PowerPlanListener() : base("System", "*[System[Provider[@Name='Microsoft-Windows-UserModePowerService'] and EventID=12]]")
        {
        }

        protected override async Task OnChangedAsync()
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Power plan changed...");

            var vantageStatus = await Vantage.GetStatusAsync().ConfigureAwait(false);
            var activateWhenVantageEnabled = Settings.Instance.ActivatePowerProfilesWithVantageEnabled;
            if (vantageStatus == VantageStatus.Enabled && !activateWhenVantageEnabled)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ignoring. [vantage.status={vantageStatus}, activateWhenVantageEnabled={activateWhenVantageEnabled}]");
                return;
            }

            var powerPlans = await Power.GetPowerPlansAsync().ConfigureAwait(false);
            var activePowerPlan = powerPlans.First(pp => pp.IsActive);

            var powerModes = Power.GetMatchingPowerModes(activePowerPlan.Guid);
            if (powerModes.Length != 1)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ignoring. [matchingPowerModes={powerModes.Length}]");
                return;
            }

            var powerMode = powerModes[0];
            var currentPowerMode = await _feature.GetStateAsync().ConfigureAwait(false);
            if (powerMode == currentPowerMode)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Power mode already set.");
                return;
            }

            await _feature.SetStateAsync(powerMode).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Power mode set.");
        }
    }
}
