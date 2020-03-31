using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace _200331_SyntaxHighlight
{
    #region Type definition
    internal static class AZClassificationDefinition
    {
        /// KEY: defined the Classification Type, so it can be found by method GetClassificationType below
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("AaronZheng")]
        internal static ClassificationTypeDefinition AZ = null;
    }
    #endregion

    #region Format definition
    /// KEY: Defines the editor format
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "AaronZheng")] /// KEY: Matched ClassificationTypeDefinition Name above
    [Name("AaronZheng")]
    [UserVisible(false)]                //this should be visible to the end user
    [Order(Before = Priority.Default)]   //set the priority to be after the default classifiers
    internal sealed class AZFormatDefinition : ClassificationFormatDefinition
    {
        public AZFormatDefinition()
        {
            /// The child class have access to any member inside base class
            /// for more format settings refer to ClassificationFormatDefinition
            DisplayName = "AaronZheng"; 
            ForegroundColor = Colors.DodgerBlue;
        }
    }
    #endregion 

    #region TaggerClassifier
    /// <summary>
    /// !!!!!!!!!!!!!!!!!!!!!!!!!!!MANI ENTRY!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("AZHightLight")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class AZClassifierProvider : ITaggerProvider
    {
        [Export]
        [Name("AZHightLight")]
        [BaseDefinition("code")]        /// KEY: specify type of content
        internal static ContentTypeDefinition AZContentType = null; 

        [Export]
        [FileExtension(".az")]          /// KEY: apply to specified type of file, specified file extension
        [ContentType("AZHightLight")]   /// KEY: used to link to ContentType (Name) above
        internal static FileExtensionToContentTypeDefinition AZFileType = null; 

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

        [Import]
        internal IStandardClassificationService Standards = null;   // used visual studio default standard instead of self defined

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            /// buffer contains all text inside this file
            /// 
            /// Set up a group of Tagger by CreateTagAggregator, And looking fro all method marked with the same type: [TagType(typeof(AZTokenTag))]
            /// Which will trigger: internal sealed class AZTokenTagProvider : ITaggerProvider below.
            ITagAggregator<AZTokenTag> ookTagAggregator = aggregatorFactory.CreateTagAggregator<AZTokenTag>(buffer);

            return new AZClassifier(buffer, ookTagAggregator, ClassificationTypeRegistry, Standards) as ITagger<T>;
        }
    }

    internal sealed class AZClassifier : ITagger<ClassificationTag>
    {
        ITextBuffer _buffer;
        ITagAggregator<AZTokenTag> _aggregator;
        IDictionary<AZTokenTypes, IClassificationType> _AZTypes;

        internal AZClassifier(ITextBuffer buffer,
                               ITagAggregator<AZTokenTag> ookTagAggregator,
                               IClassificationTypeRegistryService typeService,
                               IStandardClassificationService standards)
        {
            _buffer = buffer;
            _aggregator = ookTagAggregator;

            // Different Dictionary from <IKey, AZTokenTypes>
            _AZTypes = new Dictionary<AZTokenTypes, IClassificationType>();

            /// KEY: get matched ClassificationTypeDefinition above. Add more for more types
            _AZTypes[AZTokenTypes.azTyp1] = typeService.GetClassificationType("AaronZheng"); 
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0) yield break; // Skip processing current line

            /// KEY: Call GetTags from every Taggers inside _aggreator
            ///      which will trigger GetTags inside internal sealed class AZTokenTagger : ITagger<AZTokenTag>
            foreach (var tagSpan in _aggregator.GetTags(spans))
            {
                var tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);

                /// KEY: Trigger ClassificationTypeDefinition and apply ClassificationFormatDefinition linked to it
                yield return new TagSpan<ClassificationTag>(tagSpans[0], new ClassificationTag(_AZTypes[tagSpan.Tag.type]));
            }
        }
    }
    #endregion

    #region Tagger
    [Export(typeof(ITaggerProvider))]
    [ContentType("AZHightLight")]
    [TagType(typeof(AZTokenTag))] /// KEY: used to match the type specified in CreateTagAggregator
    internal sealed class AZTokenTagProvider : ITaggerProvider /// KEY: Triggered when calling CreateTagAggregator
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag /// T must derived from ITag
        {
            return new AZTokenTagger(buffer) as ITagger<T>;
        }
    }

    internal sealed class AZTokenTagger : ITagger<AZTokenTag>
    {
        ITextBuffer _buffer;
        IDictionary<string, AZTokenTypes> _AZTypes;

        internal AZTokenTagger(ITextBuffer buffer)
        {
            _buffer = buffer;

            /// Initialized all TokenTypes into Dictionary, Add more for more types
            _AZTypes = new Dictionary<string, AZTokenTypes>();
            _AZTypes["aaronzheng"] = AZTokenTypes.azTyp1;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<AZTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            /// Called by GetTags from aggregator, spans is one line inside the file
            foreach (SnapshotSpan curSpan in spans)
            {
                // Get the line into ITextSnapshotLine
                ITextSnapshotLine containingLine = curSpan.Start.GetContainingLine(); 

                // Get the start position
                int curLoc = containingLine.Start.Position;

                // Truncated into different parts
                string[] tokens = containingLine.GetText().ToLower().Split(' '); 

                foreach (string currSubString in tokens)
                {
                    if (_AZTypes.ContainsKey(currSubString))
                    {
                        var tokenSpan = new SnapshotSpan(curSpan.Snapshot, new Span(curLoc, currSubString.Length));
                        if (tokenSpan.IntersectsWith(curSpan))
                        {
                            yield return new TagSpan<AZTokenTag>(tokenSpan, new AZTokenTag(_AZTypes[currSubString]));
                        }
                    }

                    //add an extra char location because of the space
                    curLoc += currSubString.Length + 1;
                }
            }
        }
    }
    #endregion

    #region types and class definition
    public class AZTokenTag : ITag
    {
        public AZTokenTypes type { get; private set; }

        public AZTokenTag(AZTokenTypes type)
        {
            this.type = type;
        }
    }
    
    // put more types into the enum
    public enum AZTokenTypes
    {
        azTyp1 //, azTyp2 ...
    }
    #endregion
}
