namespace Newtype.Client.Core
{
    public class BufferLine
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
}
