using System;
using System.Collections.Generic;

namespace Teniko.Models
{
    public enum AnnotationType
    {
        Unknown = 0,
        TypedText = 1,
        AttachANote = 2,
        Highlighter = 3,
        StraightLine = 4,
        FreehandLine = 5,
        HollowRectangle = 6,
        FilledRectangle = 7
    }

    public abstract class WangAnnotation
    {
        public AnnotationType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[] Color { get; set; } = new int[3]; // RGB
        public int CreationTime { get; set; }
    }

    public class TextAnnotation : WangAnnotation
    {
        public string Text { get; set; } = string.Empty;
        public string FontName { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;

        public TextAnnotation()
        {
            Type = AnnotationType.TypedText;
        }
    }

    public class NoteAnnotation : WangAnnotation
    {
        public string Text { get; set; } = string.Empty;
        public int[] BackgroundColor { get; set; } = new int[] { 255, 255, 0 }; // Yellow
        
        public NoteAnnotation()
        {
            Type = AnnotationType.AttachANote;
        }
    }

    public class LineAnnotation : WangAnnotation
    {
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int Thickness { get; set; } = 1;
        
        public LineAnnotation()
        {
            Type = AnnotationType.StraightLine;
        }
    }

    public class HighlightAnnotation : WangAnnotation
    {
        public HighlightAnnotation()
        {
            Type = AnnotationType.Highlighter;
        }
    }

    public class RectangleAnnotation : WangAnnotation
    {
        public bool IsFilled { get; set; }
        public int Thickness { get; set; } = 1;

        public RectangleAnnotation()
        {
            Type = AnnotationType.HollowRectangle;
        }
    }
}
