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
        public string rawContent = "Welcome to Newtype\nPress i to enter insert mode";
        public Mode CurrentMode = Mode.Normal;
        public int Cursor_X = 0;
        public int Cursor_Y = 0;
        public int Line = 1;
        public int Col = 1;

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
        }

        private void HandleVisualMode(KeyboardEventArgs e)
        {
        }

        private void HandleInsertMode(KeyboardEventArgs e)
        {
        }

        public void shouldPreventDefault(KeyboardEventArgs e)
        {
        }
    }
}
