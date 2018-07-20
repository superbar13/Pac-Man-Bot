using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using PacManBot.Constants;
using PacManBot.Extensions;

namespace PacManBot.Games
{
    public class C4Game : MultiplayerGame, IMessagesGame
    {
        public override string Name => "Connect Four";
        public override TimeSpan Expiry => TimeSpan.FromMinutes(60);

        private const int Columns = 7, Rows = 6;


        private Board<Player> board;
        private List<Pos> highlighted;


        private C4Game() { }

        protected override void Initialize(ulong channelId, SocketUser[] players, IServiceProvider services)
        {
            base.Initialize(channelId, players, services);

            highlighted = new List<Pos>();
            board = new Player[Columns, Rows];
            board.Fill(Player.None);
        }



        public bool IsInput(string value, ulong userId)
        {
            return userId == User(Turn)?.Id && int.TryParse(StripPrefix(value), out int num) && num > 0 && num <= Columns;
        }


        public void Input(string input, ulong userId = 1)
        {
            if (State != State.Active) return;
            LastPlayed = DateTime.Now;

            int column = int.Parse(StripPrefix(input)) - 1;
            if (!AvailableColumns(board).Contains(column)) return; // Column is full

            DropPiece(board, column, Turn);

            Time++;

            if (FindWinner(board, Turn, Time, highlighted)) Winner = Turn;
            else if (IsTie(board, Turn, Time)) Winner = Player.Tie;

            if (Winner == Player.None)
            {
                Turn = Turn.Opponent;
            }
            else
            {
                State = State.Completed;
                Turn = Winner;
            }
        }


        public override EmbedBuilder GetEmbed(bool showHelp = true)
        {
            if (State == State.Cancelled) return CancelledEmbed();

            var description = new StringBuilder();

            for (int i = 0; i < 2; i++)
            {
                if (i == Turn) description.Append("►");
                description.Append($"{((Player)i).Circle()} - {User(i).NameandDisc().SanitizeMarkdown()}\n");
            }

            description.Append("ᅠ\n");

            if (State == State.Active)
            {
                var columns = AvailableColumns(board);
                for (int x = 0; x < Columns; x++) description.Append(columns.Contains(x) ? CustomEmoji.NumberCircle[x + 1] : CustomEmoji.Empty);
                description.Append('\n');
            }

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    description.Append(board[x, y].Circle(highlighted.Contains(new Pos(x, y))));
                }
                description.Append('\n');
            }

            if (State == State.Active) description.Append("ᅠ\n*Say the number of a column (1 to 7) to place a piece*");


            return new EmbedBuilder()
            {
                Title = ColorEmbedTitle(),
                Description = description.ToString(),
                Color = Turn.Color,
                ThumbnailUrl = Winner == Player.None ? Turn.Circle().ToEmote()?.Url : User(Winner)?.GetAvatarUrl(),
            };
        }


        private static bool FindWinner(Board<Player> board, Player player, int time, List<Pos> highlighted = null)
        {
            if (time < 7) return false;
            return board.FindLines(player, 4, highlighted);
        }


        private static bool IsTie(Board<Player> board, Player turn, int time)
        {
            if (time < Rows*Columns - 6) return false;
            else if (time == Rows*Columns) return true;

            turn = turn.Opponent;

            foreach (int column in AvailableColumns(board)) // Checks that all possible configurations result in a tie
            {
                var tempBoard = board.Copy();
                DropPiece(tempBoard, column, turn);
                if (FindWinner(tempBoard, turn, time + 1) || !IsTie(tempBoard, turn, time + 1)) return false;
            }

            return true;
        }



        public override void BotInput()
        {
            var moves = new Dictionary<int, int>(); // Column and amount of possible loses by playing in that column
            var avoidMoves = new List<int>(); // Moves where it can lose right away

            var columns = AvailableColumns(board);
            if (columns.Count == 1)
            {
                moves.Add(columns[0], 0);
            }
            else
            {
                foreach (int column in columns) // All moves it can make
                {
                    var tempBoard = board.Copy();
                    DropPiece(tempBoard, column, Turn);

                    if (FindWinner(tempBoard, Turn, Time + 1)) // Can win in 1 move
                    {
                        moves = new Dictionary<int, int> { { column, 0 } };
                        avoidMoves = new List<int>();
                        break;
                    }

                    moves.Add(column, MayLoseCount(tempBoard, Turn, Turn.Opponent, Time + 1, depth: 3));

                    if (MayLoseCount(tempBoard, Turn, Turn.Opponent, Time + 1, depth: 1) > 0) avoidMoves.Add(column); // Can lose right away
                }
            }

            if (avoidMoves.Count < moves.Count)
            {
                foreach (int move in avoidMoves) moves.Remove(move);
            }

            int leastLoses = moves.Min(x => x.Value);
            var finalOptions = moves.Where(x => x.Value == leastLoses).Select(x => x.Key).ToList();

            Input($"{1 + Bot.Random.Choose(finalOptions)}");
        }




        private static int MayLoseCount(Board<Player> board, Player player, Player turn, int time, int depth)
        {
            int count = 0;

            if (depth <= 0 || time == board.Length) return count;

            foreach (int column in AvailableColumns(board))
            {
                var tempBoard = board.Copy();
                DropPiece(tempBoard, column, turn);

                if (turn != player && FindWinner(tempBoard, turn, time + 1)) // Loses to opponent
                {
                    count++;
                }
                else if (turn != player || !FindWinner(tempBoard, turn, time + 1)) // Isn't a win
                {
                    count += MayLoseCount(tempBoard, player, turn.Opponent, time + 1, depth - 1);
                }
            }

            return count;
        }



        private static void DropPiece(Board<Player> board, int column, Player player)
        {
            for (int row = Rows - 1; row >= 0; row--)
            {
                if (board[column, row] == Player.None)
                {
                    board[column, row] = player;
                    break;
                }
            }
        }
        

        private static List<int> AvailableColumns(Board<Player> board)
        {
            var available = new List<int>();
            for (int x = 0; x < Columns; x++)
            {
                bool full = true;
                for (int y = 0; y < Rows; y++)
                {
                    if (board[x, y] == Player.None) full = false;
                }
                if (!full) available.Add(x);
            }
            return available;
        }
    }
}
