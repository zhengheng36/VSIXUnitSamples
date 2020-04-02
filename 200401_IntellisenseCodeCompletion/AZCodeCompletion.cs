using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _200401_IntellisenseCodeCompletion
{
   #region CompletionController
    /// Controller is used to control the work flow of Intellisense
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]   /// KEY: specify to apply 'text' kind of file
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            CommandFilter filter = new CommandFilter(view, CompletionBroker);

            IOleCommandTarget next;
            textViewAdapter.AddCommandFilter(filter, out next);
            filter.Next = next;
        }
    }

    internal sealed class CommandFilter : IOleCommandTarget
    {
        ICompletionSession _currentSession;

        public CommandFilter(IWpfTextView textView, ICompletionBroker broker)
        {
            _currentSession = null;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            /// KEY: executed when any typing occurs
            bool handled = false;
            int hresult = VSConstants.S_OK;

            /// KEY: Action when user select between different suggestions
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        handled = StartSession();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = Complete(false);
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Complete(true);
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Cancel();
                        break;
                }
            }

            if (!handled)
            {
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            if (ErrorHandler.Succeeded(hresult))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            char ch = GetTypeChar(pvaIn);   // get current typing char
                            if (ch == ' ')
                            {
                                /// KEY: start calling into CompletionSource
                                StartSession();
                            }
                            else if (_currentSession != null)
                            {
                                Filter();
                            }
                            break;
                        case VSConstants.VSStd2KCmdID.BACKSPACE:
                            Filter();
                            break;
                    }
                }
            }
            return hresult;
        }

        private char GetTypeChar(IntPtr pvaIn)
        {
            /// KEY: return current type-in char
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        /// Narrow down the list of options as the user types input
        private void Filter()
        {
            if (_currentSession == null)
            {
                return;
            }

            _currentSession.SelectedCompletionSet.SelectBestMatch();
            _currentSession.SelectedCompletionSet.Recalculate();
        }

        /// Cancel the auto-complete session, and leave the text unmodified
        bool Cancel()
        {
            if (_currentSession == null)
            {
                return false;
            }

            _currentSession.Dismiss();

            return true;
        }

        /// Auto-complete text using the specified token
        bool Complete(bool force)
        {
            if (_currentSession == null)
            {
                return false;
            }

            if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                _currentSession.Dismiss();
                return false;
            }
            else
            {
                _currentSession.Commit();
                return true;
            }
        }

        /// <summary>
        /// Display list of potential tokens
        /// </summary>
        bool StartSession()
        {
            if (_currentSession != null)
            {
                return false;
            }

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
            ITextSnapshot snapshot = caret.Snapshot;

            if (!Broker.IsCompletionActive(TextView))
            {
                _currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
            }
            else
            {
                _currentSession = Broker.GetSessions(TextView)[0];
            }
            _currentSession.Dismissed += (sender, args) => _currentSession = null;

            /// KEY: start the session and called class CompletionSourceProvider : ICompletionSourceProvider
            _currentSession.Start();

            return true;
        }

        /// Keep running on the background
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
    #endregion

    #region ICompletionSource and ICompletionSourceProvider
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("text")]
    [Name("AZCompletion")]
    class CompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new CompletionSource(textBuffer);
        }
    }

    class CompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private bool _disposed = false;
        public CompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            /// KEY: be called after session.Start()
            if (_disposed)
            {
                throw new ObjectDisposedException("AZCompletionSource");
            }

            /// KEY: the list will showing up in the Intellisense 
            List<Completion> completions = new List<Completion>()
            {
                new Completion("if"),
                new Completion("while"),
                new Completion("AaronZheng"),
                new Completion("Whatever"),
                new Completion("...")
            };

            /// Get the whole file from _buffer
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
            {
                return;
            }

            /// Get current typing line
            var line = triggerPoint.GetContainingLine();

            /// get current typing Point
            SnapshotPoint start = triggerPoint;

            /// (start - 1).GetChar(): getting current type in char
            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
            {
                start -= 1;
            }

            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);

            completionSets.Add(new CompletionSet("SomeName", "SomeName", applicableTo, completions, Enumerable.Empty<Completion>()));
        }

        ///implemented for interface
        public void Dispose()
        {
            _disposed = true;
        }
    }
    #endregion
}
