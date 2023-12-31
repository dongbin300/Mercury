﻿using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Indentation;

using MercuryEditor.Commands;

using System.IO;
using System.Windows.Input;
using System.Xml;

namespace MercuryEditor.Editor
{
    public class MercuryEditorEntire
    {
        public static IHighlightingDefinition MercuryLightHighlighting = default!;
        public static IHighlightingDefinition MercuryDarkHighlighting = default!;
        public static FoldingManager FoldingManager = default!;
        public static XmlFoldingStrategy FoldingStrategy = default!;
        public static DefaultIndentationStrategy IndentationStrategy = default!;
        private static char[] WordSeparator =
        {
            ' ', '=', ';', '/'
        };

        public static void Init()
        {
            InitHighlighting();
        }

        private static void InitHighlighting()
        {
            using (Stream? s = typeof(MainWindow).Assembly.GetManifestResourceStream("Gaten.Stock.MercuryEditor.Editor.MercuryHighlighting-Light.xshd"))
            {
                if (s == null)
                {
                    return;
                }
                using XmlReader reader = new XmlTextReader(s);
                MercuryLightHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            using (Stream? s = typeof(MainWindow).Assembly.GetManifestResourceStream("Gaten.Stock.MercuryEditor.Editor.MercuryHighlighting-Dark.xshd"))
            {
                if(s == null)
                {
                    return;
                }
                using XmlReader reader = new XmlTextReader(s);
                MercuryDarkHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            HighlightingManager.Instance.RegisterHighlighting("Mercury_Light", new string[] { ".tm" }, MercuryLightHighlighting);
            HighlightingManager.Instance.RegisterHighlighting("Mercury_Dark", new string[] { ".tm" }, MercuryDarkHighlighting);
        }

        public static void InitCommand(TextEditor textEditor)
        {
            AvalonEditCommands.DeleteLine.InputGestures.Clear();
            textEditor.InputBindings.Add(new InputBinding(new DuplicateCommand(textEditor), new KeyGesture(Key.D, ModifierKeys.Control)));
            textEditor.InputBindings.Add(new InputBinding(new NewCommand(textEditor), new KeyGesture(Key.N, ModifierKeys.Control)));
            textEditor.InputBindings.Add(new InputBinding(new OpenCommand(textEditor), new KeyGesture(Key.O, ModifierKeys.Control)));
            textEditor.InputBindings.Add(new InputBinding(new SaveCommand(textEditor), new KeyGesture(Key.S, ModifierKeys.Control)));
            textEditor.InputBindings.Add(new InputBinding(new SaveAsCommand(textEditor), new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt)));
            textEditor.InputBindings.Add(new InputBinding(new FullScreenCommand(), new KeyGesture(Key.F11)));
            textEditor.InputBindings.Add(new InputBinding(new InspectionCommand(textEditor), new KeyGesture(Key.F5)));
        }

        public static void InitStrategy(TextEditor textEditor)
        {
            FoldingManager = FoldingManager.Install(textEditor.TextArea);
            FoldingStrategy = new XmlFoldingStrategy();
            IndentationStrategy = new DefaultIndentationStrategy();
        }

        public static void UpdateFolding(TextEditor textEditor)
        {
            FoldingStrategy.UpdateFoldings(FoldingManager, textEditor.Document);
        }

        public static string GetCurrentWord(TextArea textArea)
        {
            int i;
            int j;
            var caretOffset = textArea.Caret.Offset;
            
            for (i = caretOffset - 1; i > 0; i--)
            {
                if (textArea.Document.Text[i] == ' ' || textArea.Document.Text[i] == '\n')
                {
                    i++;
                    break;
                }
            }

            for (j = caretOffset - 1; j < textArea.Document.Text.Length; j++)
            {
                if (textArea.Document.Text[j] == ' ' || textArea.Document.Text[j] == '\n')
                {
                    break;
                }
            }

            return i > j ? string.Empty : textArea.Document.Text[i..j];
        }
    }
}
