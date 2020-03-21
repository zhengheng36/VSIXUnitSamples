using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace _200311_CheckInPolicy_Lib
{
    [Serializable]
    public sealed class AZPolicy : PolicyBase
    {
        [NonSerialized]
        private IPendingCheckin pendingCheckin;

        #region base properties
        public override string Description
        {
            get { return "Aaron Zheng Check In Policy Demo"; }
        }

        public override string Type
        {
            get { return "Check In Policy by Aaron Zheng"; }
        }

        public override string TypeDescription
        {
            get { return "This policy is a demo to set up Check In Policy in Visual Studio 2019"; }
        }

        public override bool CanEdit
        {
            get { return true; }
        }
        #endregion

        #region handle change event
        public override void Initialize(IPendingCheckin pendingCheckin)
        {
            base.Initialize(pendingCheckin);

            this.pendingCheckin = pendingCheckin;
            pendingCheckin.WorkItems.CheckedWorkItemsChanged += WorkItems_CheckedWorkItemsChanged;
        }

        void WorkItems_CheckedWorkItemsChanged(object sender, EventArgs e)
        {
            if (!Disposed)
            {
                OnPolicyStateChanged(Evaluate());
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            pendingCheckin.WorkItems.CheckedWorkItemsChanged -= WorkItems_CheckedWorkItemsChanged;
        }
        #endregion

        #region serialization stuff
        public override string GetAssemblyName()
        {
            return AZPolicySerializationBinding.PolicyAsmName;
        }

        public override BinaryFormatter GetBinaryFormatter()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Binder = new AZPolicySerializationBinding();
            return formatter;
        }
        #endregion

        public override bool Edit(IPolicyEditArgs policyEditArgs)
        {
            // always return true, replaced if needs to
            // preform before adding this Check-in Policy
            return true;
        }

        public override PolicyFailure[] Evaluate()
        {
            List<PolicyFailure> failures = new List<PolicyFailure>();
            string message = null;

            // Try to submit the items to Database
            // 
            // 1. Get the code with name and details
            // 2. Submit and return errors
            PendingChange[] getPendingChanges = pendingCheckin.PendingChanges.AllPendingChanges;

            foreach (PendingChange item in getPendingChanges)
            {
                if (!UpdateItemInDB(item, ref message))
                {
                    failures.Add(new PolicyFailure(message, this));
                    return failures.ToArray();
                }
            }

            //failures.Add(new PolicyFailure("AZ DebugBreak()", this));
            return failures.ToArray();
        }

        private bool UpdateItemInDB(PendingChange item, ref string msg)
        {
            ChangeType chgTyp = item.ChangeType;
            string fileLocation = item.LocalItem;
            string ruleName = Path.GetFileNameWithoutExtension(fileLocation);
            string ruleContent = File.ReadAllText(fileLocation);

            switch (chgTyp)
            {
                case ChangeType.Delete:
                    //DeleteRule(ruleName);
                    break;

                case ChangeType.Add:
                case ChangeType.Branch:
                case ChangeType.Edit:
                case ChangeType.Encoding:
                case ChangeType.Lock:
                case ChangeType.Merge:
                case ChangeType.None:
                case ChangeType.Property:
                case ChangeType.Rename:
                case ChangeType.Rollback:
                case ChangeType.SourceRename:
                case ChangeType.Undelete:
                default:
                    //MaintRule(ruleName, ruleContent);
                    break;
            }
            return true;
        }
        public override string InstallationInstructions
        {
            get
            {
                return Constants.InstallationInstructions;
            }
        }
    }
}
