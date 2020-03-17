namespace _200317_OnBeforeSave
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell;

	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid("e0a2565d-62c1-459e-b8a9-f96e9e7a14e0")]
	public class OnBeforeSave : ToolWindowPane
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OnBeforeSave"/> class.
		/// </summary>
		public OnBeforeSave() : base(null)
		{
			this.Caption = "OnBeforeSave";

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			this.Content = new OnBeforeSaveControl();
		}
	}
}
