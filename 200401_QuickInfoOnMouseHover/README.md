
# Work Flow

Make provider with [ContentType("text")]
 
Open File:
IIntellisenseControllerProvider -> TryCreateIntellisenseController -> 

IIntellisenseController -> m_textView.MouseHover += this.OnTextViewMouseHover

Mouse Hover Word:
Mouse Hover -> OnTextViewMouseHover -> m_provider.QuickInfoBroker.TriggerQuickInfo ->

IQuickInfoSourceProvider -> TryCreateQuickInfoSource ->

(IQuickInfoSource -> IQuickInfoSource) && (AugmentQuickInfoSession) -> foreach (string key in m_dictionary.Keys) found the pair from dictionary

-> set up applicableToSpan && IList qiContent

==> Showing value in the dictionary for the key word





