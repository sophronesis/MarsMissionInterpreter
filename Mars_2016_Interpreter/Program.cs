
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Mars_2016_Interpreter
{
    delegate void CommandSender(Command command);
    class Rover
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Rover(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    class Command
    {
        public string Name;
        public int[] Args;

        public Command(string name, int[] args)
        {
            this.Name = name;
            this.Args = args;
        }
    }

    class Map
    {
        public int[][] map;
        public Rover rover;

        public Map(int xsize, int ysize, int roverXpos, int roverYpos)
        {
            map = new int[xsize][];
            for (int i = 0; i < xsize; ++i)
                map[i] = new int[ysize];
            rover = new Rover(roverXpos, roverYpos);
        }

        public void Display()
        {
            for (int j = 0; j < map.Length; ++j)
            {
                for (int i = 0; i < map[j].Length; ++i)
                {
                    if ((rover.X == i) && (rover.Y == j))
                        Console.Write("1");
                    else
                        Console.Write(map[j][i]);
                }
                Console.WriteLine();
            }
                
            
        }

        public void CommandHandle(Command command)
        {
            Console.WriteLine(command.Name + " + " + command.Args[0]);
            if (command.Name == "RoverMove") //move rover
            {
                switch (command.Args[0])
                {
                    case 0:
                        if (rover.X < map.Length - 1)
                            rover.X += 1;
                        break;
                    case 1:
                        if (rover.Y < map[0].Length - 1)
                            rover.Y += 1;
                        break;
                    case 2:
                        if (rover.X > 0)
                            rover.X -= 1;
                        break;
                    case 3: 
                        if (rover.Y > 0)
                            rover.Y -= 1;
                        break;
                }
            }
            else if (command.Name == "EndOfCode")
            {
                
            }
        }
    }

    static class EquationParser
    { 
        private static Dictionary<string, int> _operators;
        static EquationParser()
        {
            _operators = new Dictionary<string, int>();
            _operators.Add("/", 3);
            _operators.Add("*", 3);
            _operators.Add("-", 2);
            _operators.Add("+", 2);
            _operators.Add("(", -1);
        }
        public static List<string> Tokenize(string equation)
        {
            List<string> tokens = new List<string>();
            string currentEquation = equation;
            int parenthesesCount = 0;
            var regex = new Regex(@"\s*(\d*\.\d+?|\d+|[A-Za-z]\w*|[-+*/\(\)])\s*");
            while (regex.IsMatch(currentEquation))
            {
                Capture fullcapt = regex.Match(currentEquation).Groups[0].Captures[0];
                Capture capt = regex.Match(currentEquation).Groups[1].Captures[0];
                switch (capt.ToString())
                {
                    case "(":
                        parenthesesCount++;
                        break;
                    case ")":
                        parenthesesCount--;
                        break;
                }
                tokens.Add(capt.ToString());
                currentEquation = currentEquation.Remove(fullcapt.Index, fullcapt.Length);
            }
            if (parenthesesCount!=0)
                throw new Exception("Parentheses mismatch");
            if (currentEquation.Length != 0)
                throw new Exception("Unable to tokenize equation");
            return tokens;
            
        }
        public static List<string> ConvertToRpn(List<string> tokens, Dictionary<string, float> variablesStorage)
        {
            List<string> outputQueue = new List<string>();
            Stack<string> operatorsStack= new Stack<string>();
            float number;
            foreach (string token in tokens)
            {
                if (float.TryParse(token,out number))//is number
                    outputQueue.Add(token);
                else if (new Regex(@"[a-zA-z_][a-zA-z_0-9]").IsMatch(token))
                {
                    if (variablesStorage.ContainsKey(token)) outputQueue.Add(token);
                    else throw new Exception("Can't find variable with name \""+token+"\"");

                }
                else if (token == "(") operatorsStack.Push(token);
                else if (token == ")")
                {
                    while (operatorsStack.Count > 0)
                        if ((operatorsStack.Peek() != "(")) outputQueue.Add(operatorsStack.Pop());
                        else break;
                    if (operatorsStack.Count == 0) throw new Exception("Parentheses mismatch");
                    if (operatorsStack.Peek() == "(") operatorsStack.Pop();
                }
                else if (_operators.ContainsKey(token))
                {
                    while (operatorsStack.Count > 0)
                        if (_operators[token] <= _operators[operatorsStack.Peek()])
                            outputQueue.Add(operatorsStack.Pop());
                        else break;
                    operatorsStack.Push(token);
                }
                else throw new Exception("Can't convert this row lol");//TODO
            }
            foreach (string operators in operatorsStack)
                outputQueue.Add(operators);
            return outputQueue;
        }
        public static float CalculateRpn(List<string> tokens, Dictionary<string, float> variablesStorage)
        {
            Stack<float> valueStack = new Stack<float>();
            float number;
            foreach (string token in tokens)
            {
                if (float.TryParse(token, out number))//is number
                    valueStack.Push(number);
                else if (variablesStorage.ContainsKey(token))
                {
                    float value = variablesStorage[token];
                    valueStack.Push(value);
                }
                else if (_operators.ContainsKey(token))
                {
                    switch (token)
                    {
                        case "*":
                            valueStack.Push(valueStack.Pop()*valueStack.Pop());
                            break;
                        case "/":
                            number = valueStack.Pop();
                            valueStack.Push(valueStack.Pop()/number);
                            break;
                        case "+":
                            valueStack.Push(valueStack.Pop() + valueStack.Pop());
                            break;
                        case "-":
                            number = valueStack.Pop();
                            valueStack.Push(valueStack.Pop() - number);
                            break;
                    }
                }
                else throw new Exception("Can't calculate this row lol");//TODO
            }
            return valueStack.Peek();//todo: add check of stack length
        }
        public static float Calculate(string equation, Dictionary<string, float> variablesStorage)
        {
            Console.WriteLine("!");
            return CalculateRpn(ConvertToRpn(Tokenize(equation), variablesStorage), variablesStorage);
        }
    }

    class Interpreter
    {
        public event CommandSender CommandEvent;
        private Dictionary<string, int> _roverCommand = new Dictionary<string, int>(0);
        private int _currentPos;
        private Dictionary<string, float> _variablesStorage = new Dictionary<string, float>();
        public bool IsCompleted = false;
        public string[] SourceCode { get; set; }
        public Interpreter(string[] sourceCode)
        {
            SourceCode = sourceCode;
            _currentPos = 0;
            _roverCommand = new Dictionary<string, int>();
            _roverCommand.Add("right", 0);
            _roverCommand.Add("forward", 1);
            _roverCommand.Add("left", 2);
            _roverCommand.Add("backward", 3);
            //CommandEvent += map.CommandHandle;
        }
        public void NextAction()
        {
            if (!IsCompleted)
            {
                Console.WriteLine(_currentPos+1);
                Regex currRegex;
                //check for end of file
                if (_currentPos > SourceCode.Length - 1)//last row check
                {
                    if (CommandEvent != null) CommandEvent(new Command("EndOfCode", new int[] { 0 }));
                    IsCompleted = true;
                }
                //rover motion commands
                else if ((currRegex = new Regex(@"^\s*(right|forward|left|backward)\s*;\s*$")).IsMatch(SourceCode[_currentPos]))
                {
                    Match moveRoverMatch = currRegex.Match(SourceCode[_currentPos]);
                    if (CommandEvent != null) CommandEvent(new Command("RoverMove", new int[] { _roverCommand[moveRoverMatch.Groups[1].ToString()] }));
                    _currentPos += 1;
                }
                //goto statement
                else if ((currRegex = new Regex(@"^\s*goto\s*(.+?)\s*;\s*$")).IsMatch(SourceCode[_currentPos]))
                {
                    Match gotoMatch = currRegex.Match(SourceCode[_currentPos]);
                    if (gotoMatch.Success)
                    _currentPos = int.Parse(gotoMatch.Groups[1].ToString())-1;
                    if ((_currentPos < 0) && (_currentPos >= SourceCode.Length)) throw new Exception("Wrong goto position");
                    Console.WriteLine("goto " + gotoMatch.Groups[1]);
                }
                //assigment of new var
                else if ((currRegex = new Regex(@"\s*(float|int)\s*([a-zA-Z_\-][a-zA-Z0-9_\-]*)\s*\=\s*(.+?)\s*;")).IsMatch(SourceCode[_currentPos]))
                {
                    Match setVariableMatch = currRegex.Match(SourceCode[_currentPos]);
                    for (int i = 1; i < setVariableMatch.Groups.Count; ++i)
                        Console.WriteLine(setVariableMatch.Groups[i]);
                    switch (setVariableMatch.Groups[0].ToString())
                    {
                        case "float":
                            _variablesStorage.Add(setVariableMatch.Groups[1].ToString(), EquationParser.Calculate(setVariableMatch.Groups[2].ToString(), _variablesStorage));
                            Console.WriteLine(setVariableMatch.Groups[1] + " = " + _variablesStorage[setVariableMatch.Groups[1].ToString()]);
                            break;
                        case "int":
                            _variablesStorage.Add(setVariableMatch.Groups[1].ToString(), (int)EquationParser.Calculate(setVariableMatch.Groups[2].ToString(), _variablesStorage));
                            Console.WriteLine(setVariableMatch.Groups[1] + " = " + _variablesStorage[setVariableMatch.Groups[1].ToString()]);
                            break;
                    }
                    Console.WriteLine(setVariableMatch.Groups[1].ToString() + "-" + setVariableMatch.Groups[2].ToString() + "-" + setVariableMatch.Groups[3].ToString());
                    _currentPos += 1;
                }
                //new var w/o assignment
                else if ((currRegex = new Regex(@"\s*(float|int)\s*([a-zA-Z_\-][a-zA-Z0-9_\-]*)\s*;")).IsMatch(SourceCode[_currentPos]))
                {
                    Match createVariableMatch = currRegex.Match(SourceCode[_currentPos]);
                    for (int i = 1; i < createVariableMatch.Groups.Count; ++i)
                        Console.WriteLine(createVariableMatch.Groups[i]);
                    switch (createVariableMatch.Groups[1].ToString())
                    {
                        case "float":
                            _variablesStorage.Add(createVariableMatch.Groups[2].ToString(), 0f);
                            break;
                        case "int":
                            _variablesStorage.Add(createVariableMatch.Groups[2].ToString(), 0);
                            break;
                    }
                    Console.WriteLine(createVariableMatch.Groups[2] + " = " + _variablesStorage[createVariableMatch.Groups[2].ToString()]);
                    Console.WriteLine(createVariableMatch.Groups[1].ToString() + "-" + createVariableMatch.Groups[2].ToString());
                    _currentPos += 1;
                }
                //existing value change
                else if ((currRegex = new Regex(@"\s*([a-zA-Z_\-][a-zA-Z0-9_\-]*)\s*([\+\-\*\/]?\=)\s*(.+?)\s*;")).IsMatch(SourceCode[_currentPos]))
                {
                    Match changeVariableMatch = currRegex.Match(SourceCode[_currentPos]);
                    for (int i = 1; i < changeVariableMatch.Groups.Count; ++i)
                        Console.WriteLine(changeVariableMatch.Groups[i]);
                    float equationResult = EquationParser.Calculate(changeVariableMatch.Groups[3].ToString(),
                                                             _variablesStorage);
                    switch (changeVariableMatch.Groups[2].ToString())
                    {
                        case "=":
                            _variablesStorage[changeVariableMatch.Groups[1].ToString()] = equationResult;
                            break;
                        case "+=":
                            _variablesStorage[changeVariableMatch.Groups[1].ToString()] += equationResult;
                            break;
                        case "-=":
                            _variablesStorage[changeVariableMatch.Groups[1].ToString()] -= equationResult;
                            break;
                        case "*=":
                            _variablesStorage[changeVariableMatch.Groups[1].ToString()] *= equationResult;
                            break;
                        case "/=":
                            _variablesStorage[changeVariableMatch.Groups[1].ToString()] /= equationResult;
                            break;
                    }
                    Console.WriteLine(changeVariableMatch.Groups[1] + " = " + _variablesStorage[changeVariableMatch.Groups[1].ToString()]);
                    _currentPos += 1;
                } 
                else if (new Regex(@"^\s*$").IsMatch(SourceCode[_currentPos]))
                {
                    _currentPos += 1;
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {


            string filename = "../../test.txt";
            FileInfo file = new FileInfo(filename);
            if (file.Exists)
            {
                string[] lines = File.ReadAllLines(filename);
                Map myMap = new Map(10,10,4,4);
                Interpreter myInterpreter = new Interpreter(lines);
                myInterpreter.CommandEvent += myMap.CommandHandle;
                try
                {
                    while (!myInterpreter.IsCompleted)
                    {
                        myInterpreter.NextAction();
                        myMap.Display();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("e = "+e.Message);
                }

            }
            else
                Console.WriteLine("File not found");
            Console.WriteLine("!End!");
            Console.ReadKey();
            return;

            string a = " foo* foo - 10 + 5 * 7 +24/(1 + 1  ) ";//данное выражение
            Console.WriteLine(a);
            var b = EquationParser.Tokenize(a);     //список токенов
            var c = new Dictionary<string, float>();//словарь переменных
            //переменные в словаре переменных
            c.Add("foo", 10);
            c.Add("bar", 0);
            foreach (var i in EquationParser.ConvertToRpn(b, c))//токены в польской нотации
            {
                Console.Write(i + " | ");
            }
            Console.WriteLine();
            foreach (var j in c)//переменные в словаре переменных
            {
                Console.WriteLine(j.Key + " = " + j.Value);
            }
            Console.WriteLine();
            Console.WriteLine(EquationParser.CalculateRpn(EquationParser.ConvertToRpn(b, c), c));//результат

            Console.ReadKey();
            return;

        }   
    }
}