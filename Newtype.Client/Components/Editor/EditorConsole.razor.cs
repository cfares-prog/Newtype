using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Newtype.Client.Core;
using Newtype.Shared.Models; 
using System.Net.Http.Json;
using System.Text;

namespace Newtype.Client.Components.Editor
{
    public partial class EditorConsole : IAsyncDisposable
    {
        private HubConnection? hubConnection;

        [Inject] private NavigationManager Nav { get; set;} = default;
        [Inject] private HttpClient Http { get; set; } = default;

        private List<string> TerminalLines = new();
        private bool IsTerminalOpen = false;
        private bool IsCompiling = false;

        private Mode CurrentMode = Mode.Normal;
        private bool IsTreeOpen { get; set; } = true;

        private int Row = 0; 
        private int Col = 0; 

        private string VisibleText = "Welcome to Newtype";
        private bool shouldPreventDefault = true;

        private List<BufferLine> Console = new List<BufferLine> { new() };

        protected override async Task OnInitializedAsync()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/compilerHub"))
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<string>("ReceiveTerminalOutput", (message) =>
                    {
                        TerminalLines.Add(message);
                        StateHasChanged(); 
                    });

            await hubConnection.StartAsync();
        }

        private async Task RunCodeAsync()
        {
            if (hubConnection is null) return;

            IsCompiling = true;
            IsTerminalOpen = true;
            TerminalLines.Clear();
            TerminalLines.Add(">> Spawning compiler process...");
            StateHasChanged();

            var sourceCode = GetVisibleText(); 
            var request = new CompileRequest(sourceCode, "c", "main.c");

            await Http.PostAsJsonAsync($"api/compile/run?connectionId={hubConnection.ConnectionId}", request);

            IsCompiling = false;
        }

        public async ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.DisposeAsync();
            }
        }

        private void ToggleTree()
        {
            IsTreeOpen = !IsTreeOpen;
        }

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

            ReadOnlySpan<char> textMovingDown = currentLine.GetAfter();
            int lengthToMove = textMovingDown.Length;

            var newLine = new BufferLine(); 

            if (lengthToMove > 0)
            {
                newLine.GapEnd = newLine.Buffer.Length - lengthToMove;
                textMovingDown.CopyTo(newLine.Buffer.AsSpan(newLine.GapEnd));
                newLine.GapStart = 0; 
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
                          int maxCol = Console[Row].Length > 0 ? Console[Row].Length - 1 : 0;
                          if (Col < maxCol) Col++; 
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
