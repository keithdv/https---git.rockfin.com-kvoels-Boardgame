using Boardgame.Lib;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BoardgameDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Run this to create a random board
            // and do moves until the board is full
            // The top scores achieved give you an idea of
            // how good the strategy is

            var display = new List<string>();

            //while (true)
            //{
            //var board = Board.NewBoard();

            var board = Board.NewBoard();


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

            Turn turn = new Turn(null, board);

            var p = 0;

            List<List<(int, int)>> tenFutureSpots = new List<List<(int, int)>>();
            List<int[]> tenNewSquares = new List<int[]>();

            while (turn != null)
            {


                var futureSpots = turn.Board.RandomSpots();

                var nextMoves = new List<Move>(turn.Moves());
                var blockSquares = new Square[] { Square.BlockSquare(), Square.BlockSquare(), Square.BlockSquare() };
                Square[] newSquares = new Square[] { Square.Random(), Square.Random(), Square.Random() };

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

                // BEST!!!
                var bestMoves = futureMoves
                        .OrderBy(m => m.Item2.Board.WeightedCount)
                        .ThenBy(m => m.Item2.Board.WeightedSum)
                        .ThenBy(m => m.Item2.Board.CenterSum)
                        .ThenBy(m => m.Item1.Board.WeightedCount)
                        .ThenBy(m => m.Item1.Board.WeightedSum)
                        .ThenBy(m => m.Item1.Board.CenterSum)
                        .FirstOrDefault();

                var bestMove = bestMoves.Item1;

                if (bestMove == null)
                {
                    Console.WriteLine($"Depth {turn.Depth} Points {p}");
                    WriteBoard(new Board[] { turn.Board });

                    display.Add($"Depth {turn.Depth} Points {p}");
                    break;
                }

                p += bestMove.Board.Points;

                WriteBoard(new Board[] { turn.Board.FillRandomSpots(new Square[] { Square.BlockSquare(), Square.BlockSquare(), Square.BlockSquare() }, futureSpots), bestMove.Board, bestMove.Board.Copy(false).FillRandomSpots(newSquares, futureSpots) });
                Console.ReadLine();

                //if (turn.Depth % 5 == 0)
                //{
                //}

                var nextBoard = bestMove.Board.Copy(false);

                nextBoard.FillRandomSpots(newSquares, futureSpots);

                //if (newSquares.Select(x => x.Number).Distinct().Count() == 2)
                //{
                //    Console.WriteLine($"All Match! {string.Join(' ', newSquares)}");
                //    //Console.ReadLine();
                //}

                //if (newDup == 1)
                //{
                //    WriteBoard(new Board[] { turn.Board, bestMoves.Item1.Board, bestMoves.Item2.Board, bestMoves.Item1.Board.Copy(false).FillRandomSpots(newSquares, futureSpots) });
                //    //Console.ReadLine();
                //}

                turn = new Turn(turn, nextBoard);

                if (turn.Depth % 50 == 0)
                {
                    Console.WriteLine($"Depth {turn.Depth} Points {p} Count {turn.Board.Count}");
                }

                tenFutureSpots.Add(futureSpots);
                tenNewSquares.Add(newSquares.Select(n => (int) n.Number).ToArray());

                if (tenFutureSpots.Count > 9)
                {
                    break;
                }
            }
            Console.Clear();
            display.ForEach(d => Console.WriteLine(d));

            WriteBoard(turn.Board);

            File.WriteAllText("FutureSpots.json", JsonConvert.SerializeObject(tenFutureSpots));
            File.WriteAllText("NewSquares.json", JsonConvert.SerializeObject(tenNewSquares));

            //}
        }



        private static void WriteBoard(params Board[] boards)
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
