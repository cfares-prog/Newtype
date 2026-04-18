using Microsoft.AspNetCore.Components.Web;
using System.Text;

namespace Newtype.Client.Components.Editor
{
    public enum Mode { Normal, Insert, Visual }
    class BufferLine
    {
        public char[] Buffer;
        public int GapStart;
        public int GapEnd;

        public BufferLine(int capacity = 100)
        {
            Buffer = new char[capacity];
            GapStart = 0;
            GapEnd = capacity; 
        }

        public int Length => GapStart + (Buffer.Length - GapEnd);
        public ReadOnlySpan<char> GetBefore() => Buffer.AsSpan(0, GapStart);
        public ReadOnlySpan<char> GetAfter() => Buffer.AsSpan(GapEnd);

        public override string ToString() => string.Concat(GetBefore(), GetAfter());

        public void MoveGap(int targetCol)
        {
            targetCol = Math.Clamp(targetCol, 0, Length);

            while (GapStart < targetCol)
            {
                Buffer[GapStart++] = Buffer[GapEnd++];
            }
            while (GapStart > targetCol)
            {
                Buffer[--GapEnd] = Buffer[--GapStart];
            }
        }

        public void Insert(char c, int col)
        {
            MoveGap(col);
            if (GapStart == GapEnd) ExpandBuffer();
            Buffer[GapStart++] = c;
        }

        public void Delete(int col)
        {
            MoveGap(col);
            if (GapStart > 0)
            {
                GapStart--;
            }
        }

        private void ExpandBuffer()
        {
            char[] newBuffer = new char[Buffer.Length * 2];
            ReadOnlySpan<char> before = GetBefore();
            ReadOnlySpan<char> after = GetAfter();

            before.CopyTo(newBuffer.AsSpan(0, before.Length));

            GapEnd = newBuffer.Length - after.Length;
            after.CopyTo(newBuffer.AsSpan(GapEnd, after.Length));

            Buffer = newBuffer;
        }
    }
    public partial class EditorConsole
    {
        private Mode CurrentMode = Mode.Normal;

        private int Row = 0; 
        private int Col = 0; 

        private string VisibleText = "Welcome to Newtype";
        private bool shouldPreventDefault = true;

        private List<BufferLine> Console = new List<BufferLine> { new BufferLine() };

        private void ClampCursor()
        {
            var currentLine = Console[Row];
            int length = currentLine.Length;

            if (CurrentMode == Mode.Normal)
            {
                int max = length > 0 ? length - 1 : 0;
                Col = Math.Clamp(Col, 0, max);
            }
            else
            {
                Col = Math.Clamp(Col, 0, length);
            }
}

        private void HandleEnter()
        {
            var currentLine = Console[Row];
            currentLine.MoveGap(Col);

            var newLine = new BufferLine();
            ReadOnlySpan<char> textMovingDown = currentLine.GetAfter();

            if (textMovingDown.Length > 0)
            {
                textMovingDown.CopyTo(newLine.Buffer.AsSpan());
                newLine.GapEnd = newLine.Buffer.Length - textMovingDown.Length;
            }

            currentLine.GapEnd = currentLine.Buffer.Length; 
            Console.Insert(Row + 1, newLine);
        }

        private void HandleBackspace()
        {
            if (Col > 0)
            {
                Console[Row].Delete(Col);
                Col--;
            }
            else if (Row > 0) 
            {
                var currentLine = Console[Row];
                var previousLine = Console[Row - 1];

                int newCol = previousLine.Length;
                previousLine.MoveGap(previousLine.Length);

                foreach(char c in currentLine.ToString())
                {
                    previousLine.Insert(c, previousLine.Length);
                }

                Console.RemoveAt(Row);
                Row--;
                Col = newCol;
            }
        }

        private string GetVisibleText()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Console.Count; i++)
            {
                sb.Append(Console[i].ToString());
                if (i < Console.Count - 1) sb.Append('\n');
            }
            return sb.ToString();
        }

        public void HandleKeyDown(KeyboardEventArgs e)
        {
            switch (CurrentMode)
            {
                case Mode.Normal: HandleNormalMode(e); break;
                case Mode.Visual: HandleVisualMode(e); break;
                case Mode.Insert: HandleInsertMode(e); break;
            }
            VisibleText = GetVisibleText();
        }

        private void HandleNormalMode(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case "i": CurrentMode = Mode.Insert; break;
                case "a": 
                          if (Col < Console[Row].Length) Col++; 
                          CurrentMode = Mode.Insert; 
                          break;
                case "v": CurrentMode = Mode.Visual; break;
                case "k": 
                          if (Row > 0) { Row--; ClampCursor(); } 
                          break;
                case "j": 
                          if (Row < Console.Count - 1) { Row++; ClampCursor(); } 
                          break;
                case "h": 
                          if (Col > 0) Col--; 
                          break;
                case "l": 
                          if (Col < Console[Row].Length) Col++; 
                          break;
            }
        }

        private void HandleVisualMode(KeyboardEventArgs e)
        {
            if (e.Key == "Escape") CurrentMode = Mode.Normal;
        }

        private void HandleInsertMode(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case "Escape":
                    if (Col > 0) Col--; 
                    CurrentMode = Mode.Normal;
                    break;

                case "Enter":
                    HandleEnter();
                    Row++;
                    Col = 0;
                    break;

                case "Backspace":
                    HandleBackspace();
                    break;

                default:
                    if (e.Key.Length == 1)
                    {
                        Console[Row].Insert(e.Key[0], Col);
                        Col++;
                    }
                    break;
            }
        }
    }
}
