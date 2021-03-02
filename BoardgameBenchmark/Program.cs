using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Boardgame.Lib;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BoardgameBenchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var summary = BenchmarkRunner.Run<BenchmarkNextMove>();
            return;

            // make sure the same result
            WriteBoard(new[] { new BenchmarkNextMove().RunTenMoves() });
            WriteBoard(new[] { new BenchmarkNextMove().RunTenMoves() });
            WriteBoard(new[] { new BenchmarkNextMove().RunTenMoves() });

        }

        [MemoryDiagnoser]
        public class BenchmarkNextMove
        {
            private readonly Board board;

            List<List<(int, int)>> tenFutureSpots;
            List<Square[]> tenNewSquares;

            public BenchmarkNextMove()
            {
                board = Board.NewBoard();

                board[0, 0] = 4;
                board[1, 0] = 1;
                board[2, 0] = 0;
                board[3, 0] = 8;
                board[4, 0] = 0;
                board[5, 0] = 0;

                board[0, 1] = 3;
                board[1, 1] = 5;
                board[2, 1] = 0;
                board[3, 1] = 0;
                board[4, 1] = 0;
                board[5, 1] = 3;

                board[0, 2] = 7;
                board[1, 2] = 0;
                board[2, 2] = 7;
                board[3, 2] = 6;
                board[4, 2] = 1;
                board[5, 2] = 6;

                board[0, 3] = 0;
                board[1, 3] = 0;
                board[2, 3] = 0;
                board[3, 3] = 0;
                board[4, 3] = 0;
                board[5, 3] = 0;

                board[0, 4] = 0;
                board[1, 4] = 7;
                board[2, 4] = 0;
                board[3, 4] = 0;
                board[4, 4] = 0;
                board[5, 4] = 0;

                board[0, 5] = 5;
                board[1, 5] = 0;
                board[2, 5] = 0;
                board[3, 5] = 0;
                board[4, 5] = 0;
                board[5, 5] = 0;


                tenFutureSpots = JsonConvert.DeserializeObject<List<List<(int, int)>>>(File.ReadAllText("FutureSpots.json"));
                tenNewSquares = JsonConvert.DeserializeObject<List<int[]>>(File.ReadAllText("NewSquares.json")).Select(t => new Square[] { t[0], t[1], t[2] }).ToList();

            }

            [Benchmark]
            public Board RunTenMoves()
            {

                Turn turn = new Turn(null, board);

                for (int i = 0; i < 4; i++)
                {
                    var futureSpots = tenFutureSpots[i];// turn.Board.RandomSpots();
                    var nextMoves = new List<Move>(turn.Moves());

                    var blockSquares = new Square[] { Square.BlockSquare(), Square.BlockSquare(), Square.BlockSquare() };
                    Square[] newSquares = tenNewSquares[i]; // new Square[] { Square.Random(), Square.Random(), Square.Random() };

                    var newDup = newSquares.Select(x => x.Number).Distinct().Count();
                    if (newDup == 1)
                    {
                        blockSquares = newSquares;
                    }

                    var futureMoves = new ConcurrentBag<(Move, Move)>();

                    Parallel.ForEach(nextMoves, m =>
                    {
                        var b = m.Board.Copy(false).FillRandomSpots(blockSquares, futureSpots);
                        foreach (var nm in (new Turn(turn, b).Moves())) { futureMoves.Add((m, nm)); }
                    });

                    var bestMoves = futureMoves
                        .OrderBy(m => m.Item2.Board.WeightedCount)
                        .ThenBy(m => m.Item2.Board.WeightedSum)
                        .ThenBy(m => m.Item2.Board.CenterSum)
                        .ThenBy(m => m.Item1.Board.WeightedCount)
                        .ThenBy(m => m.Item1.Board.WeightedSum)
                        .ThenBy(m => m.Item1.Board.CenterSum)
                        .FirstOrDefault();

                    var nextBoard = bestMoves.Item1.Board.Copy(false);

                    nextBoard.FillRandomSpots(newSquares, futureSpots);

                    turn = new Turn(turn, nextBoard);

                }

                return turn.Board;
            }
        }



        private static void WriteBoard(IEnumerable<Board> boards)
        {
            foreach (var b in boards) { Console.WriteLine($"Count: {b.Count} Points: {b.Points} Sum: {b.CenterSum}"); }

            for (byte y = 0; y < 6; y++)
            {
                foreach (var b in boards)
                {
                    if (b == null) { break; }
                    Console.Write("|");
                    for (byte x = 0; x < 6; x++)
                    {
                        var t = b[x, y];
                        if (t.IsMatched)
                        {
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        else if (t.IsTrail)
                        {
                            Console.BackgroundColor = ConsoleColor.White;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        else if (t.IsNew)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        else if (!t.IsEmpty && t.Number == 0)
                        {
                            Console.BackgroundColor = ConsoleColor.Yellow;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        Console.Write($" {t} ");
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write("|       |");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
