
# Key Steps and Key method

1. Add a new Editor Window to the project and delete all generated files. The whole purpose is to add MEF into asset

2. internal sealed class AZClassifierProvider : ITaggerProvider
   * CreateTagger will be called
   * AggregatorFactory.CreateTagAggregator will be called
     * Will call into Step 3 below
   * new class AZClassifier : ITagger will be created
     * public IEnumerable GetTags(NormalizedSnapshotSpanCollection spans) will be called when loading file

3. internal sealed class AZTokenTagProvider : ITaggerProvider
   * CreateTagger will be called
   * new class AZTokenTagger : ITagger will be created
     * public IEnumerable GetTags(NormalizedSnapshotSpanCollection spans) will be called directly by GetTags in step2

# Work Flow

internal sealed class AZClassifierProvider : ITaggerProvider

                |
                V

            CreateTagger

                |
                V

aggregatorFactory.CreateTagAggregator

                |
                V

internal sealed class AZTokenTagProvider : ITaggerProvider

                |
                V

            CreateTagger
            
                |
                V

internal sealed class AZTokenTagger : ITagger 

                +

Set up public IEnumerable<ITagSpan<AZTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)

                |
                V

GetTags inside AZClassifierProvider : tagSpan.Span.GetSpans

                |
                V

GetTags inside AZTokenTagProvider : ITaggerProvider: process NormalizedSnapshotSpanCollection spans and return AZClassifierProvider

                |
                V

AZClassifierProvider: yield return new TagSpan<<ClassificationTag>>

                |
                V

      ClassificationTypeDefinition

                |
                V

      ClassificationFormatDefinition

                |
                V

            Apply Format









