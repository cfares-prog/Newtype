using Microsoft.AspNetCore.Components.Web;

namespace Newtype.Client.Components.Editor
{

    public enum Mode
    {
        Normal,
        Insert,
        Visual
    }

    public partial class EditorConsole
    {
        private Mode CurrentMode = Mode.Normal;
        private int Cursor_X = 0;
        private int Cursor_Y = 0;
        private int Line = 1;
        private int Col = 1;
        readonly static int ConsoleWidth = 120;
        private string VisibleText = "Welcome to Newtype";
        private bool shouldPreventDefault = true;

        //Buffer Management
        private char[] Buffer = new char[ConsoleWidth * 50];
        private int gapStart = 0;
        private int gapEnd;

        private void MoveGapTo(int targetIndex)
        {

            if(gapStart == targetIndex) return;

            if(targetIndex < gapStart)
            {
                int distance = gapStart - targetIndex;
                while(distance > 0)
                {
                    gapStart--;
                    gapEnd--;
                    Buffer[gapEnd] = Buffer[gapStart];
                    distance--;
                }
            }

            if(targetIndex > gapStart)
            {
                int distance = targetIndex - targetIndex;
                while(distance > 0)
                {
                    Buffer[gapStart] = Buffer[gapEnd];
                    gapStart++;
                    gapEnd++;
                    distance--;
                }
            }
        }

        private void ExpandBuffer()
        {
            char[] NewBuffer = new char[Buffer.Length * 2];

            ReadOnlySpan<char> beforeGap = ExtractString(0,gapStart);
            ReadOnlySpan<char> afterGap = ExtractString(gapEnd, Buffer.Length - gapEnd);

            gapEnd = NewBuffer.Length - afterGap.Length;

            beforeGap.CopyTo(NewBuffer.AsSpan(0, beforeGap.Length));
            afterGap.CopyTo( NewBuffer.AsSpan(gapEnd, afterGap.Length));

            Buffer = NewBuffer;
        }

        private void InsertCharacter(char c)
        {
            if(gapStart == gapEnd) ExpandBuffer();
            Buffer[gapStart] = c;
            gapStart++;
        }

        private void Backspace()
        {
            if(gapStart > 0)
            {
                gapStart--;
            }
        }

        private ReadOnlySpan<char> ExtractString(int start, int end)
        {
            ReadOnlySpan<char> extractedPart = Buffer.AsSpan(start, end); 
            return extractedPart;
        }

        private string GetVisibleText()
        {
            ReadOnlySpan<char> beforeGap = ExtractString(0, gapStart);
            ReadOnlySpan<char> afterGap = ExtractString(gapEnd, Buffer.Length - gapEnd);
            return string.Concat(beforeGap, afterGap);
        }

        private static int GetBufferPosition(int x, int y)
        {
            return (y * ConsoleWidth) + x;
        }

        protected override void OnInitialized()
        {
            for(int i = 0; i < Buffer.Length;i++)
            {
                Buffer[i] = ' ';
            }
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
                        int index = GetBufferPosition(Cursor_X, Cursor_Y);
                        MoveGapTo(index);
                        InsertCharacter(char.Parse(e.Key));
                        VisibleText = GetVisibleText();
                    }
                    break;
            }
        }
    }
}
