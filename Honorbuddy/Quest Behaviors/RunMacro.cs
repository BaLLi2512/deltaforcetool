// Behavior originally contributed by AxaZol.
//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

#region Summary and Documentation
// DOCUMENTATION:
//     http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Custom_Behavior:_RunMacro
//
#endregion


#region Examples
#endregion


#region Usings
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
#endregion



namespace Honorbuddy.Quest_Behaviors.RunMacro
{
	[CustomBehaviorFileName(@"RunMacro")]
	public class RunMacro : QuestBehaviorBase
	{
		public RunMacro(Dictionary<string, string> args)
			: base(args)
		{
			QBCLog.BehaviorLoggingContext = this;

			try
			{
				GoalText = GetAttributeAs<string>("GoalText", false, ConstrainAs.StringNonEmpty, null) ?? "";
				Macro = GetAttributeAs<string>("Macro", true, ConstrainAs.StringNonEmpty, null) ?? "";
				NumOfTimes = GetAttributeAsNullable<int>("NumOfTimes", false, ConstrainAs.RepeatCount, null) ?? 1;
				WaitTime = GetAttributeAsNullable<int>("WaitTime", false, ConstrainAs.Milliseconds, null) ?? 1500;

				if (string.IsNullOrEmpty(GoalText))
				{ GoalText = "Running Macro"; }
			}

			catch (Exception except)
			{
				// Maintenance problems occur for a number of reasons.  The primary two are...
				// * Changes were made to the behavior, and boundary conditions weren't properly tested.
				// * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
				// In any case, we pinpoint the source of the problem area here, and hopefully it
				// can be quickly resolved.
				QBCLog.Exception(except);
				IsAttributeProblem = true;
			}
		}


		// Attributes provided by caller
		public string GoalText { get; private set; }
		public string Macro { get; private set; }
		public int NumOfTimes { get; private set; }
		public int WaitTime { get; private set; }

		// DON'T EDIT THESE--they are auto-populated by Subversion
		public override string SubversionId { get { return ("$Id: RunMacro.cs 2106 2015-08-13 15:17:32Z Dogan $"); } }
		public override string SubversionRevision { get { return ("$Revision: 2106 $"); } }


        #region Overrides of QuestBehaviorBase

        public override void OnStart()
		{
			// This reports problems, and stops BT processing if there was a problem with attributes...
			// We had to defer this action, as the 'profile line number' is not available during the element's
			// constructor call.
			OnStart_HandleAttributeProblem();

			this.UpdateGoalText(QuestId, GoalText);
		}

        protected override void EvaluateUsage_DeprecatedAttributes(XElement xElement)
        {
            //// EXAMPLE: 
            //UsageCheck_DeprecatedAttribute(xElement,
            //    Args.Keys.Contains("Nav"),
            //    "Nav",
            //    context => string.Format("Automatically converted Nav=\"{0}\" attribute into MovementBy=\"{1}\"."
            //                              + "  Please update profile to use MovementBy, instead.",
            //                              Args["Nav"], MovementBy));
        }

        protected override void EvaluateUsage_SemanticCoherency(XElement xElement)
        {
            //// EXAMPLE:
            //UsageCheck_SemanticCoherency(xElement,
            //    (!MobIds.Any() && !FactionIds.Any()),
            //    context => "You must specify one or more MobIdN, one or more FactionIdN, or both.");
            //
            //const double rangeEpsilon = 3.0;
            //UsageCheck_SemanticCoherency(xElement,
            //    ((RangeMax - RangeMin) < rangeEpsilon),
            //    context => string.Format("Range({0}) must be at least {1} greater than MinRange({2}).",
            //                  RangeMax, rangeEpsilon, RangeMin)); 
        }

	    protected override Composite CreateMainBehavior()
	    {
	        return new ActionRunCoroutine(ctx => MainCoroutine());
	    }

        private async Task<bool>MainCoroutine()
        {
            if (IsDone)
                return false;

            for (int counter = 1; counter <= NumOfTimes; ++counter)
            {
                this.UpdateGoalText(QuestId, string.Format("Running macro {0} times", NumOfTimes));
                TreeRoot.StatusText = string.Format("RunMacro {0}/{1} Times", counter, NumOfTimes);

                Lua.DoString(string.Format("RunMacroText(\"{0}\")", Macro), 0);
                await Coroutine.Sleep(WaitTime);
            }

            BehaviorDone();
            return true;
        }

	    #endregion
	}
}