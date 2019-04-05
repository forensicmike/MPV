using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;

namespace MassPlistViewer
{
    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (ViewModel.ActiveSearchTerms == null || ViewModel.ActiveSearchTerms.Count() == 0)
            {
                return;
            }

            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;
            foreach (var term in ViewModel.ActiveSearchTerms)
            {
                while ((index = text.IndexOf(term, start)) >= 0)
                {
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + term.Length, // endOffset
                        (VisualLineElement element) =>
                        {
                        // This lambda gets called once for every VisualLineElement
                        // between the specified offsets.
                        Typeface tf = element.TextRunProperties.Typeface;
                        // Replace the typeface with a modified version of
                        // the same typeface
                        element.TextRunProperties.SetTypeface(new Typeface(
                                tf.FontFamily,
                                FontStyles.Italic,
                                FontWeights.Bold,
                                tf.Stretch
                            ));
                            element.TextRunProperties.SetFontRenderingEmSize(20.0);
                            element.TextRunProperties.SetBackgroundBrush(Brushes.LightGreen);
                        });
                    start = index + 1; // search for next occurrence
                }
            }
        }
    }
}
