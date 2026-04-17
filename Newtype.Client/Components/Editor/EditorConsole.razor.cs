using Microsoft.AspNetCore.Components.Web;
using System.Text;

namespace Newtype.Client.Components.Editor
{

    public enum Mode
    {
        Normal,
        Insert,
        Visual
    }

    class BufferLine
    {
        public char[] Buffer = new char[50];
        public int GapStart = 0;
        public int GapEnd = 50;

        public BufferLine(){}
    }

    public partial class EditorConsole
    {
        private Mode CurrentMode = Mode.Normal;
        private int Cursor_X = 0;
        private int Cursor_Y = 0;
        private int Line = 1;
        private int Col = 1;
        private string VisibleText = "Welcome to Newtype";
        private bool shouldPreventDefault = true;

        //Buffer Management
        private List<BufferLine> Console = new List<BufferLine> { new BufferLine() };
        private Queue<char[]> BufferPool = new Queue<char[]>();

        private void ExpandBuffer(BufferLine line)
        {
            char[] NewBuffer = new char[line.Buffer.Length * 2];

            ReadOnlySpan<char> beforeGap = ExtractString(0, line.GapStart, line.Buffer);
            ReadOnlySpan<char> afterGap = 
                ExtractString(
                    line.GapEnd, 
                    line.Buffer.Length - line.GapEnd,
                    line.Buffer);

            line.GapEnd = NewBuffer.Length - afterGap.Length;

            beforeGap.CopyTo(NewBuffer.AsSpan(0, beforeGap.Length));
            afterGap.CopyTo( NewBuffer.AsSpan(line.GapEnd, afterGap.Length));

            line.Buffer = NewBuffer;
        }

        private void MoveLineGap(BufferLine line, int targetCol)
        {
            if(line.GapStart == targetCol) return;

            while(line.GapStart > targetCol)
            {
                line.GapStart--;
                line.GapEnd--;
                line.Buffer[line.GapEnd] = line.Buffer[line.GapStart];
            }
        }
        private void InsertCharacter(char c, BufferLine line)
        {
            if(line.GapStart == line.GapEnd) ExpandBuffer(line);
            line.Buffer[line.GapStart] = c;
            line.GapStart++;
        }

        private void Backspace(BufferLine line)
        {
            if(line.GapStart > 0)
            {
                line.GapStart--;
            }
        }

        private ReadOnlySpan<char> ExtractString(int start, int end, char[] Buffer)
        {
            ReadOnlySpan<char> extractedPart = Buffer.AsSpan(start, end); 
            return extractedPart;
        }

        private string GetVisibleText()
        {
            var sb = new StringBuilder();
            foreach(var line in Console)
            {
                ReadOnlySpan<char> beforeGap = ExtractString(0, line.GapStart, line.Buffer);
                ReadOnlySpan<char> afterGap = ExtractString(
                        line.GapEnd, 
                        line.Buffer.Length - line.GapEnd,
                        line.Buffer
                        );
                 sb.Append(string.Concat(beforeGap, afterGap));
                 sb.Append('\n');
            }

            return sb.ToString();
        }

        private static int GetBufferPosition(int x, int y)
        {
            return 0;
        }

        public void HandleKeyDown(KeyboardEventArgs e)
        {

            if(CurrentMode == Mode.Normal)
            {
                HandleNormalMode(e);
            }
            else if(CurrentMode == Mode.Visual)
            {
                HandleVisualMode(e);
            }
            else
            {
                HandleInsertMode(e);
            }
        }

        private void HandleNormalMode(KeyboardEventArgs e)
        {
            switch(e.Key)
            {
                case "i":
                    CurrentMode = Mode.Insert;
                    break;
                case "a":
                    Cursor_X++;
                    Col++;
                    CurrentMode = Mode.Insert;
                    break;

                case "v":
                    CurrentMode = Mode.Visual;
                    break;

                case "k":
                    if(Line > 1)
                    {
                        Cursor_Y--;
                        Line--;
                    }
                    break;

                case "j":
                    Cursor_Y++;
                    Line++;
                    break;

                case "l":
                    Cursor_X++;
                    Col++;
                    break;

                case "h":
                    if(Col > 1)
                    {
                        Cursor_X--;
                        Col--;
                    }
                    break;
            }
        }

        private void HandleVisualMode(KeyboardEventArgs e)
        {

            switch(e.Key)
            {
                case "Escape":
                    if(Col > 1)
                    {
                        Cursor_X--;
                        Col--;
                    }
                    CurrentMode = Mode.Normal;
                    break;
            }
        }

        private void HandleInsertMode(KeyboardEventArgs e)
        {
            switch(e.Key)
            {
                case "Escape":
                    if(Col > 1)
                    {
                        Cursor_X--;
                        Col--;
                    }
                    CurrentMode = Mode.Normal;
                    break;

                default:
                    if(e.Key.Length == 1)
                    {
                        Cursor_X++;
                        Col++;
                        var currentLine = Console[Line - 1];
                        MoveLineGap(currentLine, Col - 1);
                        InsertCharacter(e.Key[0], currentLine);
                        VisibleText = GetVisibleText();
                    }
                    break;
            }
        }
    }
}
