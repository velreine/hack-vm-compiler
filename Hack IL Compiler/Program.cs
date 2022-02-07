using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hack_IL_Compiler
{
    class Program
    {
        public Dictionary<string, ushort> Memory = new Dictionary<string, ushort>()
        {
            {"SP", 0},
            {"LCL", 1},
            {"ARG", 2},
            {"THIS", 3},
            {"THAT", 4},
            {"Temp0", 5},
            {"Temp1", 6},
            {"Temp2", 7},
            {"Temp3", 8},
            {"Temp4", 9},
            {"Temp5", 10},
            {"Temp6", 11},
            {"Temp7", 12},
            {"R13", 13},
            {"R14", 14},
            {"R15", 15},
            /**
            In between comes our custom "variables" and "labels".
            **/
            {"KBD", 24576},
            {"SCREEN", 16384},
        };

        public enum Segment
        {
            Local,
            Argument,
            Constant,
            This,
            That,
            Static,
            Temp,
            Pointer,
        }

        private class FunctionDef
        {
            public string Name { get; set; }
            public int NumArgs { get; set; }
        }

        static List<FunctionDef> functions = new List<FunctionDef>();

        static int labels = 0;

        static string[] ParseVM(string file, string line)
        {
            // push     \t\t        constant           2
            // commands[0] = "push"
            // commands[1] = "constant"
            // commands[2] = "2"
            string[] commands = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (commands.Length == 0)
            {
                return new string[0];
            }

            // The first part of the VM code is always the command.
            var cmd = commands[0];

            if (cmd == "pop")
            {
                var segment = commands[1];
                var value = Convert.ToInt32(commands[2]);

                if (segment == "argument")
                {
                    // pop argument <offset>
                    // pop what-ever is on the stack to argument + offset.
                    // Dette kan ikke være fuckign rigtigt mand what the fuck, hvoraskfasdn oanfiksd sgiosdg

                    // new. 12 instr
                    return new string[]
                    {
                        // Get offset
                        $"@{value}",
                        "D=A", // D gets overwritten
                        // Get arg address and add offset
                        "@ARG",
                        "D=D+M", // 7000 + 3 => 7003
                        // Place Address in register 14
                        "@R14",
                        "M=D",
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Get address from reg 14 and place value in address location
                        "@R14",
                        "A=M",
                        "M=D",
                    };
                }

                if (segment == "local")
                {
                    return new string[]
                    {
                        // Get offset
                        $"@{value}",
                        "D=A", // D gets overwritten
                        // Get local address and add offset
                        "@LCL",
                        "D=D+M", // 7000 + 3 => 7003
                        // Place Address in register 14
                        "@R14",
                        "M=D",
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Get address from reg 14 and place value in address location
                        "@R14",
                        "A=M",
                        "M=D",
                    };
                }

                if (segment == "this")
                {
                    return new string[]
                    {
                        // Get offset
                        $"@{value}",
                        "D=A", // D gets overwritten
                        // Get this address and add offset
                        "@THIS",
                        "D=D+M", // 7000 + 3 => 7003
                        // Place Address in register 14
                        "@R14",
                        "M=D",
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Get address from reg 14 and place value in address location
                        "@R14",
                        "A=M",
                        "M=D",
                    };
                }

                if (segment == "that")
                {
                    return new string[]
                    {
                        // Get offset
                        $"@{value}",
                        "D=A", // D gets overwritten
                        // Get local address and add offset
                        "@THAT",
                        "D=D+M", // 7000 + 3 => 7003
                        // Place Address in register 14
                        "@R14",
                        "M=D",
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Get address from reg 14 and place value in address location
                        "@R14",
                        "A=M",
                        "M=D",
                    };
                }

                if (segment == "temp")
                {
                    return new string[]
                    {
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Place value in temp register
                        $"@R{5 + value}",
                        "M=D",
                    };
                }

                if (segment == "pointer")
                {
                    var target = value == 0 ? "THIS" : "THAT";

                    return new string[]
                    {
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Place value in this/that register
                        $"@{target}",
                        "M=D",
                    };
                }

                // pop static <value>
                if (segment == "static")
                {
                    // @16 + <offset>
                    // var addressToReach = (16 + value);

                    return new string[]
                    {
                        // Get value from stack and decrement stack pointer
                        "@SP",
                        "AM=M-1",
                        "D=M",
                        // Place value on the address
                        $"@{file}.{value}",
                        "M=D",
                    };
                }
            }

            // Handle the push command.
            if (cmd == "push")
            {
                var segment = commands[1];

                var value = 0;

                try
                {
                    value = Convert.ToInt32(commands[2]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Input was: " + commands[2].ToString());
                    Console.WriteLine(e);
                    throw;
                }
                //var value = Convert.ToInt32(commands[2]);

                // Target segment: constant

                // place 2 into the address SP points to
                // equivalent to ram[SP] = 2
                //          D       M       A       SP      256
                // @2       ??      ??      2       256     ??
                // D=A      2       ??      2       256     ??
                // @SP      2       *SP     SP      256     ??
                // A=M      2       **SP    *SP     256     ??
                // M=D      2       **SP=2  *SP     256     2
                // we then increment the stack pointer
                // @SP      2       *SP     SP      256     2
                // M=M+1    2       *SP++   SP      257     2
                if (segment == "constant")
                {
                    return new string[]
                    {
                        $"@{value}",
                        "D=A",
                        "@SP",
                        "A=M",
                        "M=D",
                        "@SP",
                        "M=M+1",
                    };
                }

                // push targets: constant, argument, this, that, (?)temp, 
                if (segment == "argument")
                {
                    // push argument <offset>
                    // push what-ever is at argument + offset onto the stack.
                    return new string[]
                    {
                        $"@{value}", // value is offset.
                        "D=A", // D = offset.
                        "@ARG",
                        "A=D+M", // A = pointer + offset
                        "D=M", // Store value from *arg+offset into buffer.
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }

                if (segment == "this")
                {
                    return new string[]
                    {
                        $"@{value}", // value is offset.
                        "D=A", // D = offset.
                        "@THIS",
                        "A=D+M", // A = pointer + offset
                        "D=M", // Store value from *this+offset into buffer.
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }

                if (segment == "that")
                {
                    return new string[]
                    {
                        $"@{value}", // value is offset.
                        "D=A", // D = offset.
                        "@THAT",
                        "A=D+M", // A = pointer + offset
                        "D=M", // Store value from *that+offset into buffer.
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }

                if (segment == "local")
                {
                    return new string[]
                    {
                        $"@{value}", // value is offset.
                        "D=A", // D = offset.
                        "@LCL",
                        "A=D+M", // A = pointer + offset
                        "D=M", // Store value from *local+offset into buffer.
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }

                if (segment == "temp")
                {
                    return new string[]
                    {
                        $"@R{value + 5}",
                        "D=M", // Store value from temp into buffer.
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }

                if (segment == "pointer")
                {
                    var target = value == 0 ? "THIS" : "THAT";

                    return new string[]
                    {
                        $"@{target}",
                        "D=M",
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }

                if (segment == "static")
                {
                    // @16 + <offset>
                    // var addressToReach = (16 + value);

                    return new string[]   
                    {
                        $"@{file}.{value}",
                        "D=M",
                        // Standard put value into stack
                        "@SP",
                        "M=M+1", // increment stack pointer
                        "A=M-1", // get the top of the stack
                        "M=D", // place value on top of stack
                    };
                }
            }

            if (cmd == "add")
            {
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "M=D+M", // 257
                };
            }

            if (cmd == "and")
            {
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "M=D&M", // 257
                };
            }

            if (cmd == "or")
            {
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "M=D|M", // 257
                };
            }

            if (cmd == "sub")
            {
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "M=M-D", // 257
                };
            }

            if (cmd == "neg")
            {
                return new string[]
                {
                    "@SP", // 258
                    "A=M-1", // 257
                    "M=-M", // 257
                };
            }

            if (cmd == "not")
            {
                return new string[]
                {
                    "@SP", // 258
                    "A=M-1", // 257
                    "M=!M", // 257
                };
            }

            if (cmd == "eq")
            {
                string label_eq = $"EQ_{labels++}";
                string label_finished = $"FIN_{labels++}";
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "D=M-D", // 257 D er måske 0. === EQ
                    "@" + label_eq,
                    "D;JEQ",
                    "@SP",
                    "A=M-1",
                    "M=0",
                    "@" + label_finished,
                    "0;JMP",
                    $"({label_eq})",
                    "@SP",
                    "A=M-1", // 256
                    "M=-1", // set value to -1 which is actually 1111...1111 => true.
                    $"({label_finished})"
                };
            }

            if (cmd == "neq")
            {
                string label_neq = $"NOT_EQ_{labels++}";
                string label_finished = $"FIN_{labels++}";
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "D=M-D", // 257 D er måske 0. === NEQ
                    "@" + label_neq,
                    "D;JNE",
                    "@SP",
                    "A=M-1",
                    "M=0",
                    "@" + label_finished,
                    "0;JMP",
                    $"({label_neq})",
                    "@SP",
                    "A=M-1", // 256
                    "M=-1", // set value to -1 which is actually 1111...1111 => true.
                    $"({label_finished})"
                };
            }

            if (cmd == "lt")
            {
                string label_lt = $"LT_{labels++}";
                string label_finished = $"FIN_{labels++}";
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "D=M-D", // 257 D er måske 0. === LT
                    "@" + label_lt,
                    "D;JLT",
                    "@SP",
                    "A=M-1",
                    "M=0",
                    "@" + label_finished,
                    "0;JMP",
                    $"({label_lt})",
                    "@SP",
                    "A=M-1", // 256
                    "M=-1", // set value to -1 which is actually 1111...1111 => true.
                    $"({label_finished})"
                };
            }

            if (cmd == "gt")
            {
                string label_lt = $"GT_{labels++}";
                string label_finished = $"FIN_{labels++}";
                return new string[]
                {
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    "A=A-1", // 257
                    "D=M-D", // 257 D er måske 0. === GT
                    "@" + label_lt,
                    "D;JGT",
                    "@SP",
                    "A=M-1",
                    "M=0",
                    "@" + label_finished,
                    "0;JMP",
                    $"({label_lt})",
                    "@SP",
                    "A=M-1", // 256
                    "M=-1", // set value to -1 which is actually 1111...1111 => true.
                    $"({label_finished})"
                };
            }

            if (cmd == "label")
            {
                var labelName = commands[1];
                // If we got a label just turn it into something our assembler understands.
                // which is (LABEL) etc...
                return new string[]
                {
                    $"({labelName})"
                };
            }

            if (cmd == "goto")
            {
                var labelName = commands[1];

                return new string[]
                {
                    $"@{labelName}",
                    "0;JMP",
                };
            }

            if (cmd == "if-goto")
            {
                var labelName = commands[1];

                /* return'er new string[] (array) {
                /* denne string er "@SP",
                /*}
                */
                return new string[]
                {
                    // assume -1 is true
                    // assume -1 is on the stack
                    // ! post.script our assumptions were false.
                    "@SP", // 258
                    "AM=M-1", // 257
                    "D=M", // 257
                    $"@{labelName}",
                    "D;JNE", // if D != 0 ie. true. JNE checks if the value is 0.
                };
            }

            if (cmd == "call")
            {
                var functionName = commands[1];
                var numArgs = Convert.ToInt32(commands[2]);

                // CALL implementation:
                // save/push ret$main.add, LCL, ARG, THIS, THAT.
                // set arg pointer to stack pointer -5 - num_args
                // set LCL to SP
                //call Main.add 2
                //(ret$main.add_<labels++>) // <--- where the program flow should continue after the call.

                var retLabel = $"ret${functionName}_{labels++}";

                return new string[]
                {
                    // push return address
                    $"@{retLabel}",
                    "D=A",
                    "@SP",
                    "M=M+1", // increment SP
                    "A=M-1", // take top of stack addr
                    "M=D", // put return address onto stack.
                    // push LCL
                    "@LCL",
                    "D=M",
                    "@SP",
                    "M=M+1", // increment SP
                    "A=M-1", // take top of stack addr
                    "M=D", // put local address onto stack.
                    // push ARG
                    "@ARG",
                    "D=M",
                    "@SP",
                    "M=M+1", // increment SP
                    "A=M-1", // take top of stack addr
                    "M=D", // put arg address onto stack.
                    // push THIS
                    "@THIS",
                    "D=M",
                    "@SP",
                    "M=M+1", // increment SP
                    "A=M-1", // take top of stack addr
                    "M=D", // put this address onto stack.
                    // push THAT
                    "@THAT",
                    "D=M",
                    "@SP",
                    "M=M+1", // increment SP
                    "A=M-1", // take top of stack addr
                    "M=D", // put that address onto stack.
                    // set ARG = SP - (numArgs + 5)
                    // set arg pointer to stack pointer -5 - num_args      
                    $"@{numArgs + 5}",
                    "D=A",
                    "@SP",
                    "D=M-D", // grab the address of the stack pointer minus 7.
                    "@ARG",
                    "M=D", // store the address in the ARG register.

                    // Set LCL to point to the same as Stack Pointer.
                    "@SP",
                    "D=M",
                    "@LCL",
                    "M=D",

                    // goto/jmp to function          
                    $"@{functionName}",
                    "0;JMP",

                    $"({retLabel})",
                };
            }

            if (cmd == "return")
            {
                return new string[]
                {
                    // endFrame = LCL
                    // *ARG = pop()
                    // SP = ARG + 1 
                    // THAT = *(endFrame -1)
                    // THIS = *(endFrame -2)
                    // ARG = *(endFrame -3)
                    // LCL = *(endFrame -4)
                    // retAddr = *(endFrame -5)
                    // goto retAddr

                    // endFrame = LCL :
                    "@LCL",
                    "D=M",
                    "@R13", // save into R13 as endFrame
                    "M=D",
                    // pop stack value to arg 0
                    // Set SP to point to the same as LCL.

                    // retAddr = *(endFrame - 5)
                    "@5",
                    "D=A",
                    "@R13",
                    "A=M-D",
                    "D=M",
                    "@R14",
                    "M=D",

                    // *ARG = pop()
                    // Get value from SP[-1] top of stack
                    "@SP",
                    "A=M-1",
                    "D=M",
                    // Place value into ARG [0]
                    "@ARG",
                    "A=M",
                    "M=D",

                    // SP = ARG + 1 
                    "@ARG",
                    "D=M+1",
                    "@SP",
                    "M=D",

                    // THAT = *(endFrame - 1)
                    "@1",
                    "D=A",
                    "@R13",
                    "A=M-D",
                    "D=M",
                    "@THAT",
                    "M=D",

                    // THIS = *(endFrame - 2)
                    "@2",
                    "D=A",
                    "@R13",
                    "A=M-D",
                    "D=M",
                    "@THIS",
                    "M=D",

                    // ARG = *(endFrame - 3)
                    "@3",
                    "D=A",
                    "@R13",
                    "A=M-D",
                    "D=M",
                    "@ARG",
                    "M=D",

                    // LCL = *(endFrame - 4)
                    "@4",
                    "D=A",
                    "@R13",
                    "A=M-D",
                    "D=M",
                    "@LCL",
                    "M=D",

                    // goto retAddr
                    "@R14",
                    "A=M",
                    "0;JMP",
                };
            }

            // function Main.foo 5
            // cmd class.function numargs
            if (cmd == "function")
            {
                var functionName = commands[1];
                var numArgs = Convert.ToInt32(commands[2]);

                // When a function is invoked.
                // The caller must ensure the "state" is saved.
                // When the function call returns back the "state" should be restored.
                List<string> functionLines = new List<string>();
                functionLines.Add($"({functionName})");
                // if (numArgs == 0) {
                //     numArgs = 1;
                // }

                // If num args is above 0, push 0 that amount of times for local variables
                // So we can initialize local variables to 0
                for (int i = 0; i < numArgs; i++)
                {
                    functionLines.AddRange(new string[]
                    {
                        "@0", // put 0 into buffer.
                        "D=A",
                        "@SP", // access stack pointer.
                        "A=M", // follow stack pointer.
                        "M=D", // set value to 0.
                        "@SP",
                        "M=M+1", // increment stack pointer.
                    });
                }

                return functionLines.ToArray();
            }

            throw new Exception("no such command: " + cmd);
        }

        private static string[] RemoveComments(string[] lines)
        {
            List<string> output = new List<string>();

            foreach (string line in lines)
            {
                var modifiedLine = line;

                // Removes lines that starts with a comment or is an empty line.
                if (line.StartsWith("//") || String.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Removes in-line comments.
                var index = line.IndexOf("//");

                if (index != -1)
                {
                    modifiedLine = modifiedLine[..index];
                }

                output.Add(modifiedLine);
            }

            return output.ToArray();
        }

        static void Main(string[] args)
        {
            var folderPath = "StaticsTest";
            var path = "/Users/nicky/Desktop/H5/nand2tetris/projects/08/FunctionCalls/" + folderPath;
            // Test "push constant x" and "add"
            var outfilename = path + "/" + folderPath + ".asm";

            // Test "eq"
            //var inFile = "/Users/nicky/Desktop/H5/nand2tetris/projects/07/custom/eq.vm";
            // var inFiles = new string[] {inFile};
            var inFilesNames = System.IO.Directory.GetFiles(path, "*.vm");
            

            // var outFile = Path.ChangeExtension(inFile, "asm");
            List<string> outputLines = new List<string>();

            // Boostrap / Sys.init
            outputLines.AddRange(new string[] {  
                "// bootstrap",
                "@256",
                "D=A",
                "@SP",
                "M=D",
            });

            outputLines.AddRange(ParseVM("bootstrap.vm", "call Sys.init 0"));
            
            foreach (var fileName in inFilesNames)
            {
                // Fucking Linux.
                string underscoreFileName = fileName.Replace('/', '_');

                // Fucking Windows.
                underscoreFileName = underscoreFileName.Replace('\\', '_');

                string[] lines = File.ReadAllLines(fileName);
                lines = RemoveComments(lines);

                foreach (var line in lines)
                {
                    outputLines.Add("// " + line); 
                    outputLines.AddRange(ParseVM(underscoreFileName, line));
                }
            }

            File.WriteAllLines(outfilename, outputLines);
        }
    }
}