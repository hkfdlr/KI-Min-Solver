using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace KI_Min_Solver
{
    class Program
    {
        static void Main(string[] args)
        {
            ZipExtractor extractor = new ZipExtractor();
            Parser parser = new Parser();
            Simplex simplex = new Simplex();
            Helper helper = new Helper();
            String fileName = "KI_Benchmarks.zip";
            string[][] txtFiles = new string[0][];
            decimal[][] matrix = new decimal[0][];

            try
            {
                Console.WriteLine("Enter filename (default: KI_Benchmarks.zip): ");
                String enteredFileName = Console.ReadLine();
                if (!(enteredFileName == ""))
                {
                    fileName = enteredFileName;
                }

                String zipPath = (@".\" + $"{fileName}");

                if (!zipPath.EndsWith(".zip"))
                {
                    zipPath = zipPath + ".zip";
                }

                txtFiles = extractor.Extract(zipPath);

                foreach (string[] file in txtFiles)
                {
                    matrix = parser.Parse(file);
                    simplex.solve(matrix, "Min", matrix[0].Length - 1);
                }

                helper.deleteTxtFolder();
            } catch
            {
                if (Directory.Exists(@".\txts\"))
                {
                    helper.deleteTxtFolder();
                }
            }
        }
    }

    public class ZipExtractor
    {
        public string[][] Extract(string zipPath)
        {
            List<string[]> txtFiles = new List<string[]>();
            string[] txtLines = new string[0];

            String extractPath = @".\txts\";

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        {
                            String filePath = extractPath + entry.FullName;
                            Directory.CreateDirectory(extractPath);

                            if (!File.Exists(filePath))
                            {
                                entry.ExtractToFile(extractPath + entry.FullName);
                            }
                            txtLines = System.IO.File.ReadAllLines(filePath);
                            txtFiles.Add(txtLines);
                        }
                    }
                }
                return txtFiles.ToArray();
            } catch
            {
                Console.WriteLine("File not found");

                return new string[0][];
            }
        }
    }

    public class Parser
    {
        public decimal[][] Parse(string[] txtLines)
        {
            decimal[][] matrix = new decimal[txtLines.Length][];
            decimal[] objFunc = new decimal[0];
            int commentLines = 0;
            String type;
            int lineIndex = -1;
            foreach (string line in txtLines)
            {
                String processedLine = line;
                if (processedLine.StartsWith("//") || processedLine.StartsWith("/*"))   
                {
                    commentLines++;
                    continue;
                } else if (processedLine.StartsWith("min:"))  
                {
                    processedLine = processedLine.Replace("min:", "");
                    processedLine = processedLine + "+ 0";
                    type = "Min";
                }
                char[] separators = new char[] { '>', '<', '+' };

                if (processedLine.Contains("-")) 
                {
                    processedLine = processedLine.Replace("-", "+ -");
                }

                string[] split = processedLine.Split(separators, StringSplitOptions.RemoveEmptyEntries); 
                decimal[] matrixRow = new decimal[split.Length];
                int entryIndex = 0;
                foreach (string entry in split)
                {
                    string processedEntry = entry;
                    if (processedEntry == "" || processedEntry == " " || processedEntry == null) {
                        Array.Resize(ref matrixRow, matrixRow.Length - 1);
                        continue;
                    }
                    if (processedEntry.Contains("=")) {
                        processedEntry = processedEntry.Replace("=", "");
                        processedEntry = processedEntry.Replace(";", "");
                    }
                    processedEntry.Trim();
                    processedEntry = processedEntry.Replace(" ", "");
                    string[] subs = new string[2];
                    subs = processedEntry.Split('*');
                    matrixRow[entryIndex] = decimal.Parse(subs[0]);
                    entryIndex++;
                }
                if (lineIndex == -1)
                {
                    Array.Resize(ref objFunc, split.Length);
                    objFunc = matrixRow;
                }
                else
                {
                    matrix[lineIndex] = matrixRow;
                }
                lineIndex++;
            }
            Array.Resize(ref matrix, matrix.Length - commentLines);
            matrix[matrix.Length - 1] = objFunc;
            return matrix;
        }
    }

    public class Simplex
    {
        bool[] rowInBaseForm = new bool[0];
        decimal optimum = 0;


        public void solve(decimal[][] matrix, string type, int numberOfVars)
        {
            var helper = new Helper();
            KeyValuePair<string, decimal>[] varValues = new KeyValuePair<string, decimal>[numberOfVars];
            Array.Resize(ref rowInBaseForm, matrix.Length - 1);
            decimal[] objFunc = matrix[matrix.Length - 1];

            matrix = init(matrix, numberOfVars);

            Console.Clear();
            Console.WriteLine(type);
            Console.WriteLine();
            helper.printMatrix(matrix);

            int iterationNumber = 1;

            // Phase 1
            while (rowInBaseForm.Contains(false))
            {
                matrix = iterate(matrix, iterationNumber, false);
                iterationNumber++;
            }
            Console.Clear();
            Console.WriteLine("Phase 1 done, starting phase 2");

            Console.ReadKey();
            Console.Clear();

            // Phase 2
            // remove columns of artificial variables
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i][numberOfVars + matrix.Length - 1] = matrix[i][matrix[i].Length - 1];
                Array.Resize(ref matrix[i], matrix[i].Length - (matrix.Length - 1));
            }

            matrix[matrix.Length - 1] = objFunc;
            Array.Resize(ref matrix[matrix.Length - 1], matrix[0].Length);


            calcZLine(matrix);

            while (matrix[matrix.Length-1].SkipLast(1).Min() < 0)
            {
                matrix = iterate(matrix, iterationNumber, true);
            }

            optimum = -1 * matrix[matrix.Length - 1][matrix[matrix.Length - 1].Length - 1];
            varValues = solveObjFunc(matrix, objFunc, numberOfVars, varValues);

            Console.Clear();
            Console.WriteLine($"Optimal value: {optimum}");
            Console.WriteLine();
            for (int i = 0; i < varValues.Length; i++)
            {
                if (varValues[i].Key != null)
                {
                    Console.WriteLine($"{varValues[i].Key}: {varValues[i].Value}");
                }
                else
                {
                    varValues[i] = KeyValuePair.Create<string, decimal>($"x{i}", 0);
                    Console.WriteLine($"{varValues[i].Key}: {varValues[i].Value}");
                }
            }
            Console.ReadKey();
            Console.Clear();
        }

        decimal[][] init(decimal[][] matrix, int numberOfVars)
        {
            decimal[] wFunc = new decimal[matrix[0].Length];

            for (int i = 0; i < matrix.Length; i++)
            {
                if (i == matrix.Length - 1)
                {
                    matrix[i] = wFunc;
                    continue;
                }
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    wFunc[j] -= matrix[i][j];
                }
            }
            for (int i = 0; i < rowInBaseForm.Length; i++)
            {
                rowInBaseForm[i] = false;
            }
            for (int i = 0; i < matrix.Length; i++)
            {
                Array.Resize(ref matrix[i], matrix[i].Length + ((matrix.Length - 1) * 2));
                matrix[i][matrix[i].Length - 1] = matrix[i][numberOfVars];
                matrix[i][numberOfVars] = 0;

                for (int j = numberOfVars; j < matrix[i].Length -1; j++)
                {
                    if ((i < matrix.Length - 1))
                    {
                        if (j == i + numberOfVars)
                        {
                            matrix[i][j] = -1;
                        }
                        else if (j == i + numberOfVars + matrix.Length - 1)
                        {
                            matrix[i][j] = 1;
                        }
                    }
                    else
                    {
                        if (j < i + numberOfVars)
                        {
                            matrix[i][j] = 1;
                        }
                    }
                }
            }

            return matrix;
        }

        decimal[][] iterate(decimal[][] matrix, int iterationNumber, bool isPhaseTwo)
        {
            var helper = new Helper();
            decimal[] elemsInPivCol = new decimal[matrix.Length -1];
            decimal[] objFunc;
            decimal max = 0;
            decimal[] maxInRow = new decimal[matrix.Length];
            var pivRow = 0;
            int pivCol = 0;
            decimal pivElem = 0;

            // build wFunc, set wFunc as last row

            pivCol = Array.IndexOf(matrix[matrix.Length - 1], matrix[matrix.Length - 1].SkipLast(1).Min());

            for (int i = 0; i < matrix.Length - 1; i++)
            {
                if (matrix[i][pivCol] > 0)
                {
                    elemsInPivCol[i] = matrix[i][matrix[i].Length - 1] / matrix[i][pivCol];
                } else
                {
                    elemsInPivCol[i] = decimal.MaxValue;
                }

            }

            pivRow = Array.IndexOf(elemsInPivCol, elemsInPivCol.Min());
            pivElem = matrix[pivRow][pivCol];

            if (pivRow == matrix.Length - 1)
            {
                return matrix;
            }

            for (int i = 0; i < matrix[pivRow].Length; i++)
            {
                matrix[pivRow][i] /= pivElem;
                rowInBaseForm[pivRow] = true;
            }

            for (int i = 0; i < matrix.Length; i++)
            {
                if (i == pivRow)
                {
                    continue;
                }
                var elemInPivCol = matrix[i][pivCol];
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    matrix[i][j] -= (matrix[pivRow][j] * elemInPivCol);
                }
            }
            return matrix;
        }

        void calcZLine(decimal[][] matrix)
        {
            decimal[] zRowValues = new decimal[matrix.Length - 1];
            var isBase = false;

            for (int i = 0; i < matrix[0].Length; i++)
            {
                isBase = false;
                for (int j = 0; j < matrix.Length - 1; j++)
                {
                    if (!(matrix[j][i].Equals(0) || matrix[j][i].Equals(1)))
                    {
                        break;
                    } else if (matrix[j][i] == 1 && isBase == false)
                    {
                        isBase = true;
                        zRowValues[j] = -1 * matrix[matrix.Length - 1][i];
                    }
                    else if (matrix[j][i] == 1 && isBase == true)
                    {
                        zRowValues[j] = 0;
                        break;
                    }
                }
            }
            for (int i = 0; i < matrix.Length - 1; i++)
            {
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    matrix[i][j] = matrix[i][j];
                    matrix[matrix.Length - 1][j] += matrix[i][j] * zRowValues[i];
                }
            }
        }

        KeyValuePair<string, decimal>[] solveObjFunc(decimal[][] matrix, decimal[] objFunc, int numberOfVars, KeyValuePair<string, decimal>[] varValues) 
        {


            for (int i = 0; i < numberOfVars; i++)
            {
                bool oneFound = false;

                for (int j = 0; j < matrix.Length; j++)
                {
                    if (matrix[j][i] == 1)
                    {
                        if (oneFound == false)
                        {
                            oneFound = true;
                            varValues[i] = KeyValuePair.Create<string, decimal>($"x{i}", matrix[j][matrix[j].Length-1]);
                        } else
                        {
                            break;
                        }
                    }
                }
            }

            return varValues;
        }
    }

    public class Helper
    {
        public void printMatrix(decimal[][] matrix)
        {
            foreach (var line in matrix)
            {
                foreach (var elem in line)
                {
                    Console.Write($"{elem}  |");
                }
                Console.WriteLine();
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.ReadKey();
        }

        public void deleteTxtFolder()
        {
            string[] files = Directory.GetFiles(@".\txts\");

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            Directory.Delete(@".\txts\");
        }
    }
}