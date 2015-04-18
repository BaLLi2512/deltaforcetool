// Originally contributed by Chinajade
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 4.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/4.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

#region Usings
using System;
using System.Diagnostics;
using System.Linq;

// using BuddyControlPanel.Resources.Localization;
using Styx;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

// ReSharper disable InconsistentNaming
#endregion


namespace BuddyControlPanel
{
    // By its nature, only static elements belong in the Utility class...
    public static class FlightorMonitor
    {
	    public static WoWPoint FindBestLandingSpot(WoWPoint destination)
	    {
			using (StyxWoW.Memory.AcquireFrame())
			{
				var npcWithinLoS =
					(from wowUnit in ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
					where
						!wowUnit.IsFlying
						&& IsSuitableLandingAnchor(wowUnit, destination)
					orderby
						// Prefer non-hostile units first...
						(wowUnit.IsHostile ? (1000*1000) : 0) + wowUnit.Location.DistanceSqr(destination)
					select wowUnit)
					.FirstOrDefault();

				if (npcWithinLoS != null)
					return Utility.RandomizedWowPointForLanding(npcWithinLoS.Location, destination);
		
				var objectWithinLoS =
					(from wowObject in ObjectManager.GetObjectsOfType<WoWObject>(false, false)
						where
						IsSuitableLandingAnchor(wowObject, destination)
						orderby
						wowObject.Location.DistanceSqr(destination)
						select wowObject)
					.FirstOrDefault();

				if (objectWithinLoS != null)
					return Utility.RandomizedWowPointForLanding(objectWithinLoS.Location, destination);


				var gameObjectWithinLoS =
					(from wowGameObject in ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
						where
						IsSuitableLandingAnchor(wowGameObject, destination)
						orderby 
						wowGameObject.Location.DistanceSqr(destination)
						select wowGameObject)
					.FirstOrDefault();

				if (gameObjectWithinLoS != null)
					return Utility.RandomizedWowPointForLanding(gameObjectWithinLoS.Location, destination);

				// N.B.: Future improvements could be to start with our location, and keep tracelining
				// ground-based locations that can navigation to our destination.  This will be crazy
				// expensive.  So, we'll defer that problem until it actually needs to be solved.
				const string message = "Stopping bot--Unable to find suitable landing spot.";
				PluginLog.Error(message);
				TreeRoot.Stop(message);
			}

			return WoWPoint.Zero;
	    }


	    private static bool IsSuitableLandingAnchor(WoWObject wowObject, WoWPoint destination)
	    {
		    return
			    !GameWorld.TraceLine(wowObject.Location,
				    wowObject.Location.Add(0.0f, 0.0f, 100.0f),
					// Rule out anything 'inside' or 'swimming'...
				    TraceLineHitFlags.Collision | TraceLineHitFlags.LiquidAll)
				&& Navigator.CanNavigateFully(wowObject.Location, destination);
	    }


		/// <summary>
		/// <para>Returns 'true', if Flightor is stuck in one of its back-and-forth patterns,
		/// because it can't figure out how to get to destination.  This is not the same thing
		/// as a 'stuck', because Flightor recovers from stucks just fine.</para>
		/// <para>Once we declare Flightor as 'confused', we'll maintain the assertion
		/// for a few seconds.  This provides hysteresis so the caller will prefer
		/// ground travel for a bit.</para>
		/// </summary>
		/// <returns></returns>
	    public static bool IsFlightorConfused()
		{
			// Confusion only happen when flying...
			if (!StyxWoW.Me.IsFlying)
			{
				Clear();
				return false;
			}

			CaptureSample();

			// If not enough information to make 'confused' determination, assume we're good...
			if (sampleCount < Tunable_MinSampleThreshold)
				return false;

			// If we're too close to our average position, then Flightor is confused...
			return StyxWoW.Me.Location.Distance(averagePosition) <= Tunable_DistanceToAverageForConfusionDeclaration;
		}	    

		private static readonly Stopwatch SampleTimer = new Stopwatch();
		private static readonly TimeSpan SampleTime = TimeSpan.FromMilliseconds(1000);

	    private const int Tunable_MinSampleThreshold = 7;
	    private const double Tunable_DistanceToAverageForConfusionDeclaration = 10.0;

		private static WoWPoint averagePosition;
	    private static int sampleCount;

		private static void Clear()
		{
			SampleTimer.Reset();
			averagePosition = WoWPoint.Zero;
			sampleCount = 0;
		}

		private static void CaptureSample()
		{
			// Place upper bound on our sampling interval...
			if (SampleTimer.IsRunning && (SampleTimer.Elapsed <= SampleTime))
				return;

			// N.B.: We use low-pass filter rather than an actual average for this...
			// This makes detection much more responsive, as we don't care about where we were
			// >60 seconds ago to determine if we're gummed up.
			const float lowpassFilterCoefficient = 0.75f; // [0.0..1.0]
			var myLocation = StyxWoW.Me.Location;

			if (sampleCount == 0)
				averagePosition = myLocation;

			averagePosition.X = (lowpassFilterCoefficient * averagePosition.X)
								+ ((1 - lowpassFilterCoefficient) * myLocation.X);

			averagePosition.Y = (lowpassFilterCoefficient * averagePosition.Y)
								+ ((1 - lowpassFilterCoefficient) * myLocation.Y);

			averagePosition.Z = (lowpassFilterCoefficient * averagePosition.Z)
								+ ((1 - lowpassFilterCoefficient) * myLocation.Z);

			++sampleCount;
			SampleTimer.Restart();
		}
    }
}