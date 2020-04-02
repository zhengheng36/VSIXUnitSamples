using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _200401_QuickInfoOnMouseHover
{
    #region QuickInfoController and QuickInfoControllerProvider

    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("ToolTip QuickInfo Controller")]
    [ContentType("text")]       /// KEY: call this provider when opening text type files
    internal class QuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            /// KEY: Entry: triggered when opening a file
            return new QuickInfoController(textView, subjectBuffers, this);
        }
    }

    internal class QuickInfoController : IIntellisenseController
    {
        private ITextView m_textView;
        private IList<ITextBuffer> m_subjectBuffers;
        private QuickInfoControllerProvider m_provider;
        private IQuickInfoSession m_session;

        internal QuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, QuickInfoControllerProvider provider)
        {
            m_textView = textView;              
            m_subjectBuffers = subjectBuffers; 
            m_provider = provider;             

            /// KEY: Set up mouse hover event listener
            m_textView.MouseHover += this.OnTextViewMouseHover;
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            //find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = m_textView.BufferGraph.MapDownToFirstMatch
                 (new SnapshotPoint(m_textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                snapshot => m_subjectBuffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor);

            if(point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);

                if(!m_provider.QuickInfoBroker.IsQuickInfoActive(m_textView)) // check the quick info feather is enable in this textView
                {
                    /// Trigger QUickInfoProvider below
                    m_session = m_provider.QuickInfoBroker.TriggerQuickInfo(m_textView, triggerPoint, true);
                }
            }
        }

        /// Implemented Interfaces
        public void Detach(ITextView textView)
        {
            if (m_textView == textView)
            {
                m_textView.MouseHover -= this.OnTextViewMouseHover; // detached event when detach this provider
                m_textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }
    }
    #endregion

    #region QuickInfoSource and QuickInfoSourceProvider
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("ToolTip QuickInfo Source")]                  
    [Order(Before = "Default Quick Info Presenter")]    
    [ContentType("text")]                               
    internal class QuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }  // used to get current mouse hover word

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            /// KEY: Triggered by TriggerQuickInfo above.
            ///       Make use of 'this' to pass current class members into next level
            return new QuickInfoSource(this, textBuffer);
        }
    }

    internal class QuickInfoSource : IQuickInfoSource
    {
        private QuickInfoSourceProvider m_provider;
        private ITextBuffer m_subjectBuffer;
        private Dictionary<string, string> m_dictionary;

        public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            m_provider = provider;
            m_subjectBuffer = subjectBuffer;

            /// KEY: The pair of words and descriptions, used to look up
            m_dictionary = new Dictionary<string, string>();
            m_dictionary.Add("add", "AaronZheng: \nint add(int firstInt, int secondInt)\nAdds one integer to another.");
            m_dictionary.Add("subtract", "AaronZheng: \nint subtract(int firstInt, int secondInt)\nSubtracts one integer from another.");
            m_dictionary.Add("multiply", "AaronZheng: \nint multiply(int firstInt, int secondInt)\nMultiplies one integer by another.");
            m_dictionary.Add("divide", "AaronZheng: \nint divide(int firstInt, int secondInt)\nDivides one integer by another.");
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            /// KEY: Triggered each time when calling m_provider.QuickInfoBroker.TriggerQuickInfo above
            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                applicableToSpan = null;
                return;
            }

            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            //SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

            /// Get the word mouse hover right now
            ITextStructureNavigator navigator = m_provider.NavigatorService.GetTextStructureNavigator(m_subjectBuffer);
            TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);
            string searchText = extent.Span.GetText();

            // Iterate every keys in the dictionary
            foreach (string key in m_dictionary.Keys)
            {
                /// find current hover text from dictionary
                int foundIndex = searchText.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);
                if (foundIndex > -1)
                {
                    /// Found
                    applicableToSpan = currentSnapshot.CreateTrackingSpan
                        (
                            //querySpan.Start.Add(foundIndex).Position, 9, SpanTrackingMode.EdgeInclusive
                            extent.Span.Start + foundIndex, key.Length, SpanTrackingMode.EdgeInclusive
                        ); 

                    string value;
                    m_dictionary.TryGetValue(key, out value);
                    if(value != null)
                    {
                        /// Add description for this word into return IList<object>
                        qiContent.Add(value);
                    }
                    else
                    {
                        qiContent.Add("");
                    }
                    return;
                }
            }

            applicableToSpan = null;    // Must assign 'out' parameter before return
        }


        /// Implemented interface
        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
    #endregion
}
