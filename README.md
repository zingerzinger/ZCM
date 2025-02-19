# ZCM

LOLANG is a simple programming language.

<b>The repository contains:</b><br>

- examples of programs written in LOLANG (LOLANG_src)
- examples of programs written in zasm (zasm_src)
- ZCM core instruction set
- LOLANG compiler ( LOLANG --> lexer --> AST builder --> emitter --> zasm --> binary --> core emulator )
 
<b>Build & Run:</b> <br>

1. Follow the <u>[instructions](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2410)</u>  to install .Net on Ubuntu
2. ```$ dotnet run LOLANG_src/prog_fpga_test.txt```

See the [expected arbitrary LOLANG program execution results...](https://github.com/zingerzinger/ZCM/blob/master/LOLANG_TEST.png)

Watch the [Emulator and FPGA implementation comparison video...](https://youtu.be/kDskQJAMOYo)

The Cyclone IV ZCM core Verilog implementation is to be found.

![The synhesized core](https://raw.githubusercontent.com/zingerzinger/ZCM/refs/heads/master/core_sythesized.png)
