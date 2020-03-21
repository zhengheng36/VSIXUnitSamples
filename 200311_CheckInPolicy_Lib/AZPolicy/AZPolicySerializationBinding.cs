using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _200311_CheckInPolicy_Lib
{
	internal sealed class AZPolicySerializationBinding : BaseSerializationBinding
	{
		internal const string PolicyAsmName = "AaronZhengCheckInPolicy, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

		public override string AsmName
		{
			get { return PolicyAsmName; }
		}
	}
}
