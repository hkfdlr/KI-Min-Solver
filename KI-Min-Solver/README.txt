## Description


KI-Min-Solver is a C#-program for Windows used for minimization-optimization problems using the simplex-algorithm. It takes a .zip-file with each optimization as a .txt-file.


## Usage


0. .txt-files have to be in the following format: '1*x + 2*y + 3*x >= 4' with the objective function as the first row

1. Place the .zip-file in the root of the project (i.e. same as the KI-Min-Solver.exe)

2. Run the KI-Min-Solver.exe

3. Enter the .zip-files name

4. The variable values will be shown on the console as a matrix (make sure the window is big enough if you use lots of variables), confirm with Enter-Key.

5. Console tells you when phase 2 of the simplex-algorithm is ready, confirm with Enter-Key.

6. Optimum- and variable-values are shown on the console, confirm with Enter-Key to start next optimization.

7. If program exits successfully the "txts"-folder will be deleted.